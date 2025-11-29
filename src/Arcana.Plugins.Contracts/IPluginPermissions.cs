namespace Arcana.Plugins.Contracts;

/// <summary>
/// Plugin permission definitions.
/// 插件權限定義
/// </summary>
[Flags]
public enum PluginPermission
{
    None = 0,

    // File System
    ReadLocalData = 1 << 0,        // Read from plugin data folder
    WriteLocalData = 1 << 1,       // Write to plugin data folder
    ReadFileSystem = 1 << 2,       // Read arbitrary files
    WriteFileSystem = 1 << 3,      // Write arbitrary files

    // Network
    NetworkAccess = 1 << 4,        // Make HTTP requests
    WebSocketAccess = 1 << 5,      // Open WebSocket connections

    // Database
    ReadDatabase = 1 << 6,         // Read from main database
    WriteDatabase = 1 << 7,        // Write to main database

    // UI
    CreateWindows = 1 << 8,        // Create new windows
    CreateDialogs = 1 << 9,        // Show dialogs
    AccessClipboard = 1 << 10,     // Read/write clipboard
    ShowNotifications = 1 << 11,   // Show system notifications

    // Plugin Interaction
    InterPluginComm = 1 << 12,     // Communicate with other plugins
    AccessSharedState = 1 << 13,   // Access shared state store

    // System
    ExecuteCommands = 1 << 14,     // Execute registered commands
    RegisterMenus = 1 << 15,       // Register menu items
    RegisterViews = 1 << 16,       // Register views

    // Sensitive
    AccessCredentials = 1 << 17,   // Access stored credentials
    ExecuteProcess = 1 << 18,      // Execute external processes
    AccessHardware = 1 << 19,      // Access hardware devices

    // Common permission sets
    BasicPlugin = ReadLocalData | WriteLocalData | CreateDialogs | InterPluginComm | RegisterMenus | RegisterViews,
    NetworkPlugin = BasicPlugin | NetworkAccess,
    FullAccess = (1 << 20) - 1     // All permissions
}

/// <summary>
/// Permission request for runtime permission checks.
/// </summary>
public record PermissionRequest
{
    public required string PluginId { get; init; }
    public required PluginPermission Permission { get; init; }
    public string? Resource { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Permission check result.
/// </summary>
public record PermissionCheckResult
{
    public bool Granted { get; init; }
    public PluginPermission DeniedPermissions { get; init; }
    public string? Message { get; init; }

    public static PermissionCheckResult Allow() => new() { Granted = true };
    public static PermissionCheckResult Deny(PluginPermission denied, string? message = null) =>
        new() { Granted = false, DeniedPermissions = denied, Message = message };
}

/// <summary>
/// Plugin permission manifest for declaring required permissions.
/// </summary>
public record PluginPermissionManifest
{
    /// <summary>
    /// Required permissions that must be granted for plugin to function.
    /// </summary>
    public PluginPermission RequiredPermissions { get; init; }

    /// <summary>
    /// Optional permissions that enhance functionality.
    /// </summary>
    public PluginPermission OptionalPermissions { get; init; }

    /// <summary>
    /// Explanation for why each permission is needed.
    /// </summary>
    public IReadOnlyDictionary<PluginPermission, string>? PermissionReasons { get; init; }
}

/// <summary>
/// Interface for plugin permission manager.
/// </summary>
public interface IPluginPermissionManager
{
    /// <summary>
    /// Checks if a plugin has a specific permission.
    /// </summary>
    bool HasPermission(string pluginId, PluginPermission permission);

    /// <summary>
    /// Checks permissions and returns detailed result.
    /// </summary>
    PermissionCheckResult CheckPermission(PermissionRequest request);

    /// <summary>
    /// Grants a permission to a plugin.
    /// </summary>
    Task GrantPermissionAsync(string pluginId, PluginPermission permission);

    /// <summary>
    /// Revokes a permission from a plugin.
    /// </summary>
    Task RevokePermissionAsync(string pluginId, PluginPermission permission);

    /// <summary>
    /// Gets all permissions for a plugin.
    /// </summary>
    PluginPermission GetPermissions(string pluginId);

    /// <summary>
    /// Sets permissions for a plugin (replaces all existing).
    /// </summary>
    Task SetPermissionsAsync(string pluginId, PluginPermission permissions);

    /// <summary>
    /// Gets the permission manifest for a plugin.
    /// </summary>
    PluginPermissionManifest? GetManifest(string pluginId);

    /// <summary>
    /// Registers a permission manifest for a plugin.
    /// </summary>
    void RegisterManifest(string pluginId, PluginPermissionManifest manifest);

    /// <summary>
    /// Event raised when permission check fails.
    /// </summary>
    event EventHandler<PermissionDeniedEventArgs>? PermissionDenied;
}

/// <summary>
/// Event args for permission denied events.
/// </summary>
public class PermissionDeniedEventArgs : EventArgs
{
    public required string PluginId { get; init; }
    public required PluginPermission RequestedPermission { get; init; }
    public string? Resource { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Attribute for declaring required permissions on plugin classes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequiresPermissionAttribute : Attribute
{
    public PluginPermission Permission { get; }
    public string? Reason { get; }

    public RequiresPermissionAttribute(PluginPermission permission, string? reason = null)
    {
        Permission = permission;
        Reason = reason;
    }
}

/// <summary>
/// Attribute for declaring optional permissions.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class OptionalPermissionAttribute : Attribute
{
    public PluginPermission Permission { get; }
    public string? Reason { get; }

    public OptionalPermissionAttribute(PluginPermission permission, string? reason = null)
    {
        Permission = permission;
        Reason = reason;
    }
}
