using System.Reflection;
using System.Text.Json;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Contracts.Manifest;
using Arcana.Plugins.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcana.Plugins.Core;

/// <summary>
/// Plugin manager for loading, activating, and managing plugins.
/// Supports lazy loading via declarative manifests and activation events.
/// </summary>
public class PluginManager : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PluginManager> _logger;
    private readonly string _pluginsPath;
    private readonly Dictionary<string, LoadedPlugin> _plugins = new();
    private readonly Dictionary<string, PendingPlugin> _pendingPlugins = new();
    private readonly List<IPlugin> _builtInPlugins = new();
    private readonly PluginLoadContextManager _contextManager = new();

    // Lazy loading services
    private ManifestService? _manifestService;
    private ActivationEventService? _activationEventService;
    private LazyContributionService? _lazyContributionService;

    public IReadOnlyDictionary<string, LoadedPlugin> Plugins => _plugins;
    public IReadOnlyDictionary<string, PendingPlugin> PendingPlugins => _pendingPlugins;

    /// <summary>
    /// Whether to use lazy loading (default: true).
    /// When true, plugins are only loaded when their activation events fire.
    /// </summary>
    public bool UseLazyLoading { get; set; } = true;

    public PluginManager(
        IServiceProvider serviceProvider,
        ILogger<PluginManager> logger,
        string pluginsPath)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _pluginsPath = pluginsPath;
    }

    /// <summary>
    /// Initializes the lazy loading services.
    /// Call this before discovering plugins.
    /// </summary>
    public void InitializeLazyLoading(
        ManifestService manifestService,
        ActivationEventService activationEventService,
        LazyContributionService lazyContributionService)
    {
        _manifestService = manifestService;
        _activationEventService = activationEventService;
        _lazyContributionService = lazyContributionService;

        // Set up the activation callback
        _activationEventService.SetActivationCallback(async pluginId =>
        {
            await ActivatePluginAsync(pluginId);
        });

        _logger.LogInformation("Lazy loading initialized");
    }

    /// <summary>
    /// Registers a built-in plugin.
    /// </summary>
    public void RegisterBuiltInPlugin(IPlugin plugin)
    {
        _builtInPlugins.Add(plugin);
    }

    /// <summary>
    /// Registers a built-in plugin with its manifest.
    /// </summary>
    public void RegisterBuiltInPlugin(IPlugin plugin, PluginManifest manifest)
    {
        _builtInPlugins.Add(plugin);
        _manifestService?.RegisterManifest(manifest, AppContext.BaseDirectory);
    }

    /// <summary>
    /// Discovers all plugins (manifests first, then optionally loads assemblies).
    /// </summary>
    public async Task DiscoverPluginsAsync(CancellationToken cancellationToken = default)
    {
        // Phase 1: Discover manifests (without loading assemblies)
        if (UseLazyLoading && _manifestService != null)
        {
            await DiscoverManifestsAsync(cancellationToken);
        }

        // Phase 2: Load built-in plugins
        foreach (var plugin in _builtInPlugins)
        {
            await LoadPluginAsync(plugin, null, cancellationToken);
        }

        // Phase 3: Load external plugins (if not using lazy loading, or for startup plugins)
        if (Directory.Exists(_pluginsPath))
        {
            foreach (var pluginDir in Directory.GetDirectories(_pluginsPath))
            {
                try
                {
                    if (UseLazyLoading)
                    {
                        // For lazy loading, only prepare the plugin info
                        await PrepareExternalPluginAsync(pluginDir, cancellationToken);
                    }
                    else
                    {
                        // Legacy: Load immediately
                        await LoadPluginFromDirectoryAsync(pluginDir, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process plugin from {PluginDir}", pluginDir);
                }
            }
        }

        _logger.LogInformation("Discovered {Loaded} loaded plugins, {Pending} pending plugins",
            _plugins.Count, _pendingPlugins.Count);
    }

    /// <summary>
    /// Discovers manifests and registers lazy contributions.
    /// </summary>
    private async Task DiscoverManifestsAsync(CancellationToken cancellationToken)
    {
        if (_manifestService == null || _lazyContributionService == null)
            return;

        var manifests = await _manifestService.DiscoverManifestsAsync(_pluginsPath, cancellationToken);

        foreach (var manifest in manifests)
        {
            var pluginDir = _manifestService.GetManifestDirectory(manifest.Id);
            if (pluginDir == null) continue;

            // Register as pending plugin
            _pendingPlugins[manifest.Id] = new PendingPlugin
            {
                Manifest = manifest,
                PluginPath = pluginDir,
                DataPath = GetDataPath(manifest.Id)
            };

            // Register lazy contributions
            await _lazyContributionService.RegisterManifestContributionsAsync(manifest, pluginDir);

            // Register activation events
            if (manifest.ActivationEvents != null && manifest.ActivationEvents.Length > 0)
            {
                _activationEventService?.RegisterPendingPlugin(manifest.Id, manifest.ActivationEvents);
            }

            _logger.LogDebug("Discovered manifest: {PluginId} with {EventCount} activation events",
                manifest.Id, manifest.ActivationEvents?.Length ?? 0);
        }
    }

    /// <summary>
    /// Prepares an external plugin without loading it.
    /// </summary>
    private async Task PrepareExternalPluginAsync(string pluginDir, CancellationToken cancellationToken)
    {
        // Check for new manifest format first
        var manifestPath = Path.Combine(pluginDir, "plugin.manifest.json");
        if (!File.Exists(manifestPath))
        {
            manifestPath = Path.Combine(pluginDir, "plugin.json");
        }

        if (!File.Exists(manifestPath))
        {
            _logger.LogWarning("No manifest found in {PluginDir}", pluginDir);
            return;
        }

        // If manifest was already discovered, skip
        var dirName = Path.GetFileName(pluginDir);
        if (_pendingPlugins.Values.Any(p => p.PluginPath == pluginDir))
        {
            return;
        }

        // Load manifest if not already loaded
        var manifest = await _manifestService?.LoadManifestAsync(manifestPath, cancellationToken)!;
        if (manifest == null)
        {
            // Fall back to legacy loading
            await LoadPluginFromDirectoryAsync(pluginDir, cancellationToken);
            return;
        }

        // Already handled in DiscoverManifestsAsync
    }

    /// <summary>
    /// Activates all plugins that should activate on startup.
    /// </summary>
    public async Task ActivateAllAsync(CancellationToken cancellationToken = default)
    {
        // Fire startup activation event first
        if (_activationEventService != null)
        {
            await _activationEventService.FireAsync(ActivationEventType.OnStartup);
        }

        // Activate all loaded plugins (built-in and startup)
        var sortedPlugins = TopologicalSort(_plugins.Values.ToList());

        foreach (var loadedPlugin in sortedPlugins)
        {
            if (loadedPlugin.Plugin.State == PluginState.NotLoaded ||
                loadedPlugin.Plugin.State == PluginState.Loaded)
            {
                try
                {
                    await ActivateLoadedPluginAsync(loadedPlugin);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to activate plugin: {PluginId}",
                        loadedPlugin.Plugin.Metadata.Id);
                }
            }
        }

        // For plugins without lazy loading, activate pending startup plugins
        if (!UseLazyLoading)
        {
            return;
        }

        // Activate pending plugins that want startup activation
        var startupPlugins = _pendingPlugins.Values
            .Where(p => _manifestService?.ShouldActivateOnStartup(p.Manifest.Id) == true)
            .ToList();

        foreach (var pending in startupPlugins)
        {
            try
            {
                await LoadAndActivatePendingPluginAsync(pending, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to activate startup plugin: {PluginId}",
                    pending.Manifest.Id);
            }
        }
    }

    /// <summary>
    /// Activates a specific plugin by ID.
    /// Will load the plugin if it's pending.
    /// </summary>
    public async Task ActivatePluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        // Check if already loaded
        if (_plugins.TryGetValue(pluginId, out var loadedPlugin))
        {
            if (loadedPlugin.Plugin.State == PluginState.Active)
                return;

            await ActivateLoadedPluginAsync(loadedPlugin);
            return;
        }

        // Check if pending
        if (_pendingPlugins.TryGetValue(pluginId, out var pending))
        {
            await LoadAndActivatePendingPluginAsync(pending, cancellationToken);
            return;
        }

        throw new InvalidOperationException($"Plugin not found: {pluginId}");
    }

    /// <summary>
    /// Loads and activates a pending plugin.
    /// </summary>
    private async Task LoadAndActivatePendingPluginAsync(PendingPlugin pending, CancellationToken cancellationToken = default)
    {
        var pluginId = pending.Manifest.Id;
        _logger.LogInformation("Loading pending plugin: {PluginId}", pluginId);

        // Activate dependencies first
        if (pending.Manifest.Dependencies != null)
        {
            foreach (var depId in pending.Manifest.Dependencies)
            {
                await ActivatePluginAsync(depId, cancellationToken);
            }
        }

        // Load the assembly
        var assemblyName = pending.Manifest.Main;
        if (string.IsNullOrEmpty(assemblyName))
        {
            _logger.LogError("Plugin manifest missing 'main' field: {PluginId}", pluginId);
            return;
        }

        var assemblyPath = Path.Combine(pending.PluginPath, assemblyName);
        if (!File.Exists(assemblyPath))
        {
            _logger.LogError("Plugin assembly not found: {Path}", assemblyPath);
            return;
        }

        // Create load context
        var loadContext = _contextManager.CreateContext(pluginId, assemblyPath);
        var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

        // Find IPlugin implementation
        Type? pluginType = null;
        if (!string.IsNullOrEmpty(pending.Manifest.PluginClass))
        {
            pluginType = assembly.GetType(pending.Manifest.PluginClass);
        }

        if (pluginType == null)
        {
            pluginType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
        }

        if (pluginType == null)
        {
            _logger.LogError("No IPlugin implementation found in: {PluginId}", pluginId);
            return;
        }

        var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;

        // Create loaded plugin
        var loadedPlugin = new LoadedPlugin
        {
            Plugin = plugin,
            PluginPath = pending.PluginPath,
            DataPath = pending.DataPath,
            LoadContext = loadContext
        };

        // Remove from pending, add to loaded
        _pendingPlugins.Remove(pluginId);
        _plugins[pluginId] = loadedPlugin;

        // Notify lazy contribution service
        _lazyContributionService?.OnPluginActivated(pluginId);

        // Mark as activated in the event service
        _activationEventService?.MarkActivated(pluginId);

        // Activate the plugin
        await ActivateLoadedPluginAsync(loadedPlugin);

        _logger.LogInformation("Loaded and activated pending plugin: {PluginId}", pluginId);
    }

    /// <summary>
    /// Activates an already loaded plugin.
    /// </summary>
    private async Task ActivateLoadedPluginAsync(LoadedPlugin loadedPlugin)
    {
        var pluginId = loadedPlugin.Plugin.Metadata.Id;

        if (loadedPlugin.Plugin.State == PluginState.Active)
            return;

        // Activate dependencies first
        if (loadedPlugin.Plugin.Metadata.Dependencies != null)
        {
            foreach (var depId in loadedPlugin.Plugin.Metadata.Dependencies)
            {
                await ActivatePluginAsync(depId);
            }
        }

        var context = CreatePluginContext(loadedPlugin);
        await loadedPlugin.Plugin.ActivateAsync(context);

        // Mark as activated
        _activationEventService?.MarkActivated(pluginId);
        _lazyContributionService?.OnPluginActivated(pluginId);

        _logger.LogInformation("Activated plugin: {PluginId}", pluginId);
    }

    /// <summary>
    /// Deactivates a specific plugin.
    /// </summary>
    public async Task DeactivatePluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (!_plugins.TryGetValue(pluginId, out var loadedPlugin))
        {
            return;
        }

        if (loadedPlugin.Plugin.State == PluginState.Active)
        {
            await loadedPlugin.Plugin.DeactivateAsync();
            _logger.LogInformation("Deactivated plugin: {PluginId}", pluginId);
        }
    }

    /// <summary>
    /// Gets a plugin by ID.
    /// </summary>
    public IPlugin? GetPlugin(string pluginId)
    {
        return _plugins.TryGetValue(pluginId, out var loaded) ? loaded.Plugin : null;
    }

    /// <summary>
    /// Checks if a plugin is pending (not yet loaded).
    /// </summary>
    public bool IsPending(string pluginId)
    {
        return _pendingPlugins.ContainsKey(pluginId);
    }

    /// <summary>
    /// Gets the data path for a plugin.
    /// </summary>
    private static string GetDataPath(string pluginId)
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Arcana", "plugins", pluginId);
    }

    // Legacy loading method for backwards compatibility
    private async Task LoadPluginFromDirectoryAsync(string pluginDir, CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        if (!File.Exists(manifestPath))
        {
            manifestPath = Path.Combine(pluginDir, "plugin.manifest.json");
        }

        if (!File.Exists(manifestPath))
        {
            _logger.LogWarning("No manifest found in {PluginDir}", pluginDir);
            return;
        }

        var manifestJson = await File.ReadAllTextAsync(manifestPath, cancellationToken);
        var manifest = JsonSerializer.Deserialize<LegacyPluginManifest>(manifestJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (manifest == null || string.IsNullOrEmpty(manifest.Main))
        {
            _logger.LogWarning("Invalid plugin manifest in {PluginDir}", pluginDir);
            return;
        }

        var assemblyPath = Path.Combine(pluginDir, manifest.Main);
        if (!File.Exists(assemblyPath))
        {
            _logger.LogWarning("Plugin assembly not found: {AssemblyPath}", assemblyPath);
            return;
        }

        var pluginId = manifest.Id ?? Path.GetFileName(pluginDir);
        var loadContext = _contextManager.CreateContext(pluginId, assemblyPath);
        var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        foreach (var pluginType in pluginTypes)
        {
            var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
            await LoadPluginAsync(plugin, pluginDir, cancellationToken, loadContext);
        }
    }

    private Task LoadPluginAsync(IPlugin plugin, string? pluginDir, CancellationToken cancellationToken)
    {
        return LoadPluginAsync(plugin, pluginDir, cancellationToken, null);
    }

    private async Task LoadPluginAsync(IPlugin plugin, string? pluginDir, CancellationToken cancellationToken, PluginLoadContext? loadContext)
    {
        var pluginId = plugin.Metadata.Id;

        if (_plugins.ContainsKey(pluginId))
        {
            _logger.LogWarning("Plugin already loaded: {PluginId}", pluginId);
            return;
        }

        var loadedPlugin = new LoadedPlugin
        {
            Plugin = plugin,
            PluginPath = pluginDir ?? AppContext.BaseDirectory,
            DataPath = GetDataPath(pluginId),
            LoadContext = loadContext
        };

        Directory.CreateDirectory(loadedPlugin.DataPath);

        _plugins[pluginId] = loadedPlugin;
        _logger.LogInformation("Loaded plugin: {PluginId} v{Version}", pluginId, plugin.Metadata.Version);

        await Task.CompletedTask;
    }

    private IPluginContext CreatePluginContext(LoadedPlugin loadedPlugin)
    {
        return new PluginContext(
            loadedPlugin.Plugin.Metadata.Id,
            loadedPlugin.PluginPath,
            loadedPlugin.DataPath,
            _serviceProvider
        );
    }

    private static List<LoadedPlugin> TopologicalSort(List<LoadedPlugin> plugins)
    {
        var sorted = new List<LoadedPlugin>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        void Visit(LoadedPlugin plugin)
        {
            var id = plugin.Plugin.Metadata.Id;
            if (visited.Contains(id)) return;
            if (visiting.Contains(id)) throw new InvalidOperationException($"Circular dependency detected: {id}");

            visiting.Add(id);

            if (plugin.Plugin.Metadata.Dependencies != null)
            {
                foreach (var depId in plugin.Plugin.Metadata.Dependencies)
                {
                    var dep = plugins.FirstOrDefault(p => p.Plugin.Metadata.Id == depId);
                    if (dep != null)
                    {
                        Visit(dep);
                    }
                }
            }

            visiting.Remove(id);
            visited.Add(id);
            sorted.Add(plugin);
        }

        foreach (var plugin in plugins)
        {
            Visit(plugin);
        }

        return sorted;
    }

    /// <summary>
    /// Unloads a specific plugin with proper context cleanup.
    /// </summary>
    public async Task<bool> UnloadPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (!_plugins.TryGetValue(pluginId, out var loadedPlugin))
        {
            return false;
        }

        try
        {
            if (loadedPlugin.Plugin.State == PluginState.Active)
            {
                await loadedPlugin.Plugin.DeactivateAsync();
            }

            await loadedPlugin.Plugin.DisposeAsync();
            _plugins.Remove(pluginId);

            if (loadedPlugin.LoadContext != null)
            {
                var unloaded = await _contextManager.UnloadContextAsync(pluginId);
                if (!unloaded)
                {
                    _logger.LogWarning("Plugin {PluginId} context could not be fully unloaded", pluginId);
                }
                return unloaded;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unloading plugin: {PluginId}", pluginId);
            return false;
        }
    }

    public PluginLoadContextManager ContextManager => _contextManager;

    public async ValueTask DisposeAsync()
    {
        foreach (var loadedPlugin in _plugins.Values.Reverse())
        {
            try
            {
                await loadedPlugin.Plugin.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing plugin: {PluginId}", loadedPlugin.Plugin.Metadata.Id);
            }
        }
        _plugins.Clear();
        _pendingPlugins.Clear();
        _contextManager.Dispose();

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Loaded plugin information.
/// </summary>
public class LoadedPlugin
{
    public required IPlugin Plugin { get; init; }
    public required string PluginPath { get; init; }
    public required string DataPath { get; init; }
    public PluginLoadContext? LoadContext { get; init; }
    public bool IsBuiltIn => LoadContext == null;
}

/// <summary>
/// Pending plugin (manifest loaded but assembly not yet loaded).
/// </summary>
public class PendingPlugin
{
    public required PluginManifest Manifest { get; init; }
    public required string PluginPath { get; init; }
    public required string DataPath { get; init; }
}

/// <summary>
/// Legacy plugin manifest from plugin.json.
/// </summary>
internal class LegacyPluginManifest
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Main { get; set; }
    public string[]? Dependencies { get; set; }
}
