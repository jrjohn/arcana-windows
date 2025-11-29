using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Arcana.Plugins.Contracts;
using Microsoft.Extensions.Logging;

namespace Arcana.Plugins.Services;

/// <summary>
/// Plugin permission manager implementation.
/// </summary>
public class PluginPermissionManager : IPluginPermissionManager
{
    private readonly ILogger<PluginPermissionManager> _logger;
    private readonly ConcurrentDictionary<string, PluginPermission> _permissions = new();
    private readonly ConcurrentDictionary<string, PluginPermissionManifest> _manifests = new();
    private readonly string _permissionsFilePath;

    public event EventHandler<PermissionDeniedEventArgs>? PermissionDenied;

    public PluginPermissionManager(ILogger<PluginPermissionManager> logger, string dataPath)
    {
        _logger = logger;
        _permissionsFilePath = Path.Combine(dataPath, "plugin_permissions.json");
        LoadPermissions();
    }

    public bool HasPermission(string pluginId, PluginPermission permission)
    {
        var granted = GetPermissions(pluginId);
        return (granted & permission) == permission;
    }

    public PermissionCheckResult CheckPermission(PermissionRequest request)
    {
        var granted = GetPermissions(request.PluginId);
        var hasPermission = (granted & request.Permission) == request.Permission;

        if (!hasPermission)
        {
            var denied = request.Permission & ~granted;

            _logger.LogWarning(
                "Permission denied for plugin {PluginId}: {Permission} (Resource: {Resource})",
                request.PluginId, denied, request.Resource);

            PermissionDenied?.Invoke(this, new PermissionDeniedEventArgs
            {
                PluginId = request.PluginId,
                RequestedPermission = request.Permission,
                Resource = request.Resource
            });

            return PermissionCheckResult.Deny(denied, $"Permission denied: {denied}");
        }

        return PermissionCheckResult.Allow();
    }

    public async Task GrantPermissionAsync(string pluginId, PluginPermission permission)
    {
        var current = GetPermissions(pluginId);
        _permissions[pluginId] = current | permission;

        _logger.LogInformation("Granted permission {Permission} to plugin {PluginId}", permission, pluginId);

        await SavePermissionsAsync();
    }

    public async Task RevokePermissionAsync(string pluginId, PluginPermission permission)
    {
        var current = GetPermissions(pluginId);
        _permissions[pluginId] = current & ~permission;

        _logger.LogInformation("Revoked permission {Permission} from plugin {PluginId}", permission, pluginId);

        await SavePermissionsAsync();
    }

    public PluginPermission GetPermissions(string pluginId)
    {
        return _permissions.GetValueOrDefault(pluginId, PluginPermission.None);
    }

    public async Task SetPermissionsAsync(string pluginId, PluginPermission permissions)
    {
        _permissions[pluginId] = permissions;

        _logger.LogInformation("Set permissions for plugin {PluginId}: {Permissions}", pluginId, permissions);

        await SavePermissionsAsync();
    }

    public PluginPermissionManifest? GetManifest(string pluginId)
    {
        return _manifests.GetValueOrDefault(pluginId);
    }

    public void RegisterManifest(string pluginId, PluginPermissionManifest manifest)
    {
        _manifests[pluginId] = manifest;
        _logger.LogDebug("Registered permission manifest for plugin {PluginId}", pluginId);
    }

    /// <summary>
    /// Registers manifest from plugin type attributes.
    /// </summary>
    public void RegisterManifestFromType(string pluginId, Type pluginType)
    {
        var requiredAttrs = pluginType.GetCustomAttributes<RequiresPermissionAttribute>();
        var optionalAttrs = pluginType.GetCustomAttributes<OptionalPermissionAttribute>();

        var required = PluginPermission.None;
        var optional = PluginPermission.None;
        var reasons = new Dictionary<PluginPermission, string>();

        foreach (var attr in requiredAttrs)
        {
            required |= attr.Permission;
            if (!string.IsNullOrEmpty(attr.Reason))
            {
                reasons[attr.Permission] = attr.Reason;
            }
        }

        foreach (var attr in optionalAttrs)
        {
            optional |= attr.Permission;
            if (!string.IsNullOrEmpty(attr.Reason))
            {
                reasons[attr.Permission] = attr.Reason;
            }
        }

        var manifest = new PluginPermissionManifest
        {
            RequiredPermissions = required,
            OptionalPermissions = optional,
            PermissionReasons = reasons
        };

        RegisterManifest(pluginId, manifest);
    }

    /// <summary>
    /// Validates that a plugin has all required permissions.
    /// </summary>
    public PermissionCheckResult ValidateRequiredPermissions(string pluginId)
    {
        var manifest = GetManifest(pluginId);
        if (manifest == null)
        {
            return PermissionCheckResult.Allow();
        }

        var granted = GetPermissions(pluginId);
        var missing = manifest.RequiredPermissions & ~granted;

        if (missing != PluginPermission.None)
        {
            return PermissionCheckResult.Deny(missing, $"Missing required permissions: {missing}");
        }

        return PermissionCheckResult.Allow();
    }

    /// <summary>
    /// Grants default permissions for built-in plugins.
    /// </summary>
    public async Task GrantBuiltInPermissionsAsync(string pluginId)
    {
        // Built-in plugins get full access by default
        await SetPermissionsAsync(pluginId, PluginPermission.FullAccess);
    }

    /// <summary>
    /// Grants default permissions for external plugins.
    /// </summary>
    public async Task GrantDefaultPermissionsAsync(string pluginId)
    {
        var manifest = GetManifest(pluginId);
        if (manifest != null)
        {
            // Grant required permissions automatically
            await SetPermissionsAsync(pluginId, manifest.RequiredPermissions);
        }
        else
        {
            // Default to basic plugin permissions
            await SetPermissionsAsync(pluginId, PluginPermission.BasicPlugin);
        }
    }

    private void LoadPermissions()
    {
        try
        {
            if (File.Exists(_permissionsFilePath))
            {
                var json = File.ReadAllText(_permissionsFilePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, int>>(json);

                if (data != null)
                {
                    foreach (var (pluginId, permissions) in data)
                    {
                        _permissions[pluginId] = (PluginPermission)permissions;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load plugin permissions");
        }
    }

    private async Task SavePermissionsAsync()
    {
        try
        {
            var data = _permissions.ToDictionary(kvp => kvp.Key, kvp => (int)kvp.Value);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

            var directory = Path.GetDirectoryName(_permissionsFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(_permissionsFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save plugin permissions");
        }
    }
}

/// <summary>
/// Sandbox wrapper that enforces permissions on service access.
/// </summary>
public class PluginSandbox
{
    private readonly string _pluginId;
    private readonly IPluginPermissionManager _permissionManager;

    public PluginSandbox(string pluginId, IPluginPermissionManager permissionManager)
    {
        _pluginId = pluginId;
        _permissionManager = permissionManager;
    }

    /// <summary>
    /// Requires permission before executing an action.
    /// </summary>
    public void RequirePermission(PluginPermission permission, string? resource = null)
    {
        var result = _permissionManager.CheckPermission(new PermissionRequest
        {
            PluginId = _pluginId,
            Permission = permission,
            Resource = resource
        });

        if (!result.Granted)
        {
            throw new PluginPermissionException(_pluginId, result.DeniedPermissions, result.Message);
        }
    }

    /// <summary>
    /// Executes an action if permission is granted.
    /// </summary>
    public T? ExecuteWithPermission<T>(PluginPermission permission, Func<T> action, string? resource = null)
    {
        RequirePermission(permission, resource);
        return action();
    }

    /// <summary>
    /// Executes an async action if permission is granted.
    /// </summary>
    public async Task<T?> ExecuteWithPermissionAsync<T>(
        PluginPermission permission,
        Func<Task<T>> action,
        string? resource = null)
    {
        RequirePermission(permission, resource);
        return await action();
    }
}

/// <summary>
/// Exception thrown when plugin permission is denied.
/// </summary>
public class PluginPermissionException : Exception
{
    public string PluginId { get; }
    public PluginPermission DeniedPermissions { get; }

    public PluginPermissionException(string pluginId, PluginPermission denied, string? message = null)
        : base(message ?? $"Plugin {pluginId} does not have permission: {denied}")
    {
        PluginId = pluginId;
        DeniedPermissions = denied;
    }
}
