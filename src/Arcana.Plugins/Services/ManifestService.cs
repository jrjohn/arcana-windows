using System.Text.Json;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Contracts.Manifest;
using Microsoft.Extensions.Logging;

namespace Arcana.Plugins.Services;

/// <summary>
/// Service for parsing and managing plugin manifests.
/// </summary>
public class ManifestService : ManifestService
{
    private readonly ILogger<ManifestService> _logger;
    private readonly Dictionary<string, PluginManifest> _manifests = new();
    private readonly Dictionary<string, string> _manifestPaths = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public ManifestService(ILogger<ManifestService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PluginManifest?> LoadManifestAsync(string manifestPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(manifestPath))
            {
                _logger.LogWarning("Manifest file not found: {Path}", manifestPath);
                return null;
            }

            var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            var manifest = JsonSerializer.Deserialize<PluginManifest>(json, JsonOptions);

            if (manifest == null)
            {
                _logger.LogWarning("Failed to deserialize manifest: {Path}", manifestPath);
                return null;
            }

            // Validate required fields
            if (string.IsNullOrEmpty(manifest.Id))
            {
                _logger.LogWarning("Manifest missing required 'id' field: {Path}", manifestPath);
                return null;
            }

            if (string.IsNullOrEmpty(manifest.Name))
            {
                _logger.LogWarning("Manifest missing required 'name' field: {Path}", manifestPath);
                return null;
            }

            if (string.IsNullOrEmpty(manifest.Version))
            {
                _logger.LogWarning("Manifest missing required 'version' field: {Path}", manifestPath);
                return null;
            }

            // Store manifest
            _manifests[manifest.Id] = manifest;
            _manifestPaths[manifest.Id] = manifestPath;

            _logger.LogInformation("Loaded manifest for plugin: {Id} v{Version}", manifest.Id, manifest.Version);
            return manifest;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error in manifest: {Path}", manifestPath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading manifest: {Path}", manifestPath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PluginManifest>> DiscoverManifestsAsync(
        string pluginsDirectory,
        CancellationToken cancellationToken = default)
    {
        var manifests = new List<PluginManifest>();

        if (!Directory.Exists(pluginsDirectory))
        {
            _logger.LogWarning("Plugins directory not found: {Path}", pluginsDirectory);
            return manifests;
        }

        // Look for plugin.manifest.json in each subdirectory
        foreach (var pluginDir in Directory.GetDirectories(pluginsDirectory))
        {
            var manifestPath = Path.Combine(pluginDir, "plugin.manifest.json");

            // Also check for legacy plugin.json
            if (!File.Exists(manifestPath))
            {
                manifestPath = Path.Combine(pluginDir, "plugin.json");
            }

            if (File.Exists(manifestPath))
            {
                var manifest = await LoadManifestAsync(manifestPath, cancellationToken);
                if (manifest != null)
                {
                    manifests.Add(manifest);
                }
            }
        }

        _logger.LogInformation("Discovered {Count} plugin manifests", manifests.Count);
        return manifests;
    }

    /// <inheritdoc />
    public PluginManifest? GetManifest(string pluginId)
    {
        return _manifests.TryGetValue(pluginId, out var manifest) ? manifest : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<PluginManifest> GetAllManifests()
    {
        return _manifests.Values.ToList();
    }

    /// <inheritdoc />
    public string? GetManifestDirectory(string pluginId)
    {
        if (_manifestPaths.TryGetValue(pluginId, out var path))
        {
            return Path.GetDirectoryName(path);
        }
        return null;
    }

    /// <inheritdoc />
    public void RegisterManifest(PluginManifest manifest, string? basePath = null)
    {
        _manifests[manifest.Id] = manifest;
        if (basePath != null)
        {
            _manifestPaths[manifest.Id] = Path.Combine(basePath, "plugin.manifest.json");
        }
    }

    /// <inheritdoc />
    public bool ShouldActivateOnStartup(string pluginId)
    {
        var manifest = GetManifest(pluginId);
        if (manifest == null)
            return true; // Default to startup activation for unknown plugins

        var events = manifest.ActivationEvents;

        // No activation events = activate on startup
        if (events == null || events.Length == 0)
            return true;

        // Check for onStartup or wildcard
        return events.Any(e =>
            e == ActivationEvents.OnStartup ||
            e == ActivationEvents.Star);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetActivationEvents(string pluginId)
    {
        var manifest = GetManifest(pluginId);
        if (manifest?.ActivationEvents == null)
            return Array.Empty<string>();

        return manifest.ActivationEvents;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetPluginsForActivationEvent(ActivationEventType eventType, string? argument)
    {
        var matchingPlugins = new List<string>();

        foreach (var (pluginId, manifest) in _manifests)
        {
            if (manifest.ActivationEvents == null)
                continue;

            foreach (var activationEvent in manifest.ActivationEvents)
            {
                var (type, arg) = ActivationEvents.Parse(activationEvent);

                if (type == ActivationEventType.Star)
                {
                    matchingPlugins.Add(pluginId);
                    break;
                }

                if (type == eventType)
                {
                    // For events without arguments, just match the type
                    if (argument == null || arg == null || arg == argument)
                    {
                        matchingPlugins.Add(pluginId);
                        break;
                    }
                }
            }
        }

        return matchingPlugins;
    }

    /// <inheritdoc />
    public ManifestValidationResult ValidateManifest(PluginManifest manifest)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Required fields
        if (string.IsNullOrEmpty(manifest.Id))
            errors.Add("Missing required field: id");

        if (string.IsNullOrEmpty(manifest.Name))
            errors.Add("Missing required field: name");

        if (string.IsNullOrEmpty(manifest.Version))
            errors.Add("Missing required field: version");
        else if (!Version.TryParse(manifest.Version, out _))
            warnings.Add($"Invalid version format: {manifest.Version}");

        // Validate contributions
        if (manifest.Contributes != null)
        {
            // Validate views
            if (manifest.Contributes.Views != null)
            {
                foreach (var view in manifest.Contributes.Views)
                {
                    if (string.IsNullOrEmpty(view.Id))
                        errors.Add("View missing required field: id");
                    if (string.IsNullOrEmpty(view.TitleKey) && string.IsNullOrEmpty(view.Title))
                        warnings.Add($"View '{view.Id}' has no title or titleKey");
                }
            }

            // Validate menus
            if (manifest.Contributes.Menus != null)
            {
                foreach (var menu in manifest.Contributes.Menus)
                {
                    if (string.IsNullOrEmpty(menu.Id))
                        errors.Add("Menu missing required field: id");
                    if (string.IsNullOrEmpty(menu.Location))
                        errors.Add($"Menu '{menu.Id}' missing required field: location");
                }
            }

            // Validate commands
            if (manifest.Contributes.Commands != null)
            {
                foreach (var cmd in manifest.Contributes.Commands)
                {
                    if (string.IsNullOrEmpty(cmd.Id))
                        errors.Add("Command missing required field: id");
                }
            }
        }

        // Validate activation events
        if (manifest.ActivationEvents != null)
        {
            foreach (var evt in manifest.ActivationEvents)
            {
                var (type, _) = ActivationEvents.Parse(evt);
                if (type == ActivationEventType.Unknown)
                    warnings.Add($"Unknown activation event: {evt}");
            }
        }

        return new ManifestValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }
}

/// <summary>
/// Interface for manifest service.
/// </summary>
public interface ManifestService
{
    /// <summary>
    /// Loads a manifest from a file path.
    /// </summary>
    Task<PluginManifest?> LoadManifestAsync(string manifestPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers all manifests in a plugins directory.
    /// </summary>
    Task<IReadOnlyList<PluginManifest>> DiscoverManifestsAsync(string pluginsDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a manifest by plugin ID.
    /// </summary>
    PluginManifest? GetManifest(string pluginId);

    /// <summary>
    /// Gets all registered manifests.
    /// </summary>
    IReadOnlyList<PluginManifest> GetAllManifests();

    /// <summary>
    /// Gets the directory containing a plugin's manifest.
    /// </summary>
    string? GetManifestDirectory(string pluginId);

    /// <summary>
    /// Registers a manifest (for built-in plugins).
    /// </summary>
    void RegisterManifest(PluginManifest manifest, string? basePath = null);

    /// <summary>
    /// Checks if a plugin should activate on startup.
    /// </summary>
    bool ShouldActivateOnStartup(string pluginId);

    /// <summary>
    /// Gets activation events for a plugin.
    /// </summary>
    IReadOnlyList<string> GetActivationEvents(string pluginId);

    /// <summary>
    /// Gets plugins that match an activation event.
    /// </summary>
    IReadOnlyList<string> GetPluginsForActivationEvent(ActivationEventType eventType, string? argument);

    /// <summary>
    /// Validates a manifest.
    /// </summary>
    ManifestValidationResult ValidateManifest(PluginManifest manifest);
}

/// <summary>
/// Result of manifest validation.
/// </summary>
public class ManifestValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
