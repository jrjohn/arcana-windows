using System.Reflection;
using System.Text.Json;
using Arcana.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcana.Plugins.Core;

/// <summary>
/// Plugin manager for loading, activating, and managing plugins.
/// 插件管理器，用於載入、啟用和管理插件
/// </summary>
public class PluginManager : IAsyncDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PluginManager> _logger;
    private readonly string _pluginsPath;
    private readonly Dictionary<string, LoadedPlugin> _plugins = new();
    private readonly List<IPlugin> _builtInPlugins = new();
    private readonly PluginLoadContextManager _contextManager = new();

    public IReadOnlyDictionary<string, LoadedPlugin> Plugins => _plugins;

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
    /// Registers a built-in plugin.
    /// </summary>
    public void RegisterBuiltInPlugin(IPlugin plugin)
    {
        _builtInPlugins.Add(plugin);
    }

    /// <summary>
    /// Discovers and loads all plugins.
    /// </summary>
    public async Task DiscoverPluginsAsync(CancellationToken cancellationToken = default)
    {
        // Load built-in plugins first
        foreach (var plugin in _builtInPlugins)
        {
            await LoadPluginAsync(plugin, null, cancellationToken);
        }

        // Load external plugins from plugins directory
        if (Directory.Exists(_pluginsPath))
        {
            foreach (var pluginDir in Directory.GetDirectories(_pluginsPath))
            {
                try
                {
                    await LoadPluginFromDirectoryAsync(pluginDir, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load plugin from {PluginDir}", pluginDir);
                }
            }
        }

        _logger.LogInformation("Discovered {Count} plugins", _plugins.Count);
    }

    /// <summary>
    /// Activates all plugins.
    /// </summary>
    public async Task ActivateAllAsync(CancellationToken cancellationToken = default)
    {
        // Sort by dependencies
        var sortedPlugins = TopologicalSort(_plugins.Values.ToList());

        foreach (var loadedPlugin in sortedPlugins)
        {
            if (loadedPlugin.Plugin.State == PluginState.Loaded)
            {
                try
                {
                    var context = CreatePluginContext(loadedPlugin);
                    await loadedPlugin.Plugin.ActivateAsync(context);
                    _logger.LogInformation("Activated plugin: {PluginId}", loadedPlugin.Plugin.Metadata.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to activate plugin: {PluginId}", loadedPlugin.Plugin.Metadata.Id);
                }
            }
        }
    }

    /// <summary>
    /// Activates a specific plugin.
    /// </summary>
    public async Task ActivatePluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (!_plugins.TryGetValue(pluginId, out var loadedPlugin))
        {
            throw new InvalidOperationException($"Plugin not found: {pluginId}");
        }

        if (loadedPlugin.Plugin.State != PluginState.Loaded)
        {
            return;
        }

        // Activate dependencies first
        if (loadedPlugin.Plugin.Metadata.Dependencies != null)
        {
            foreach (var depId in loadedPlugin.Plugin.Metadata.Dependencies)
            {
                await ActivatePluginAsync(depId, cancellationToken);
            }
        }

        var context = CreatePluginContext(loadedPlugin);
        await loadedPlugin.Plugin.ActivateAsync(context);
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

    private async Task LoadPluginFromDirectoryAsync(string pluginDir, CancellationToken cancellationToken)
    {
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        if (!File.Exists(manifestPath))
        {
            _logger.LogWarning("No plugin.json found in {PluginDir}", pluginDir);
            return;
        }

        var manifestJson = await File.ReadAllTextAsync(manifestPath, cancellationToken);
        var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson, new JsonSerializerOptions
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

        // Use context manager for proper isolation and unloading
        var pluginId = manifest.Id ?? Path.GetFileName(pluginDir);
        var loadContext = _contextManager.CreateContext(pluginId, assemblyPath);
        var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

        // Find IPlugin implementations
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
            DataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Arcana", "plugins", pluginId),
            LoadContext = loadContext
        };

        // Ensure data directory exists
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
            // Deactivate first
            if (loadedPlugin.Plugin.State == PluginState.Active)
            {
                await loadedPlugin.Plugin.DeactivateAsync();
            }

            // Dispose the plugin
            await loadedPlugin.Plugin.DisposeAsync();

            // Remove from dictionary
            _plugins.Remove(pluginId);

            // Unload the assembly context if it's an external plugin
            if (loadedPlugin.LoadContext != null)
            {
                var unloaded = await _contextManager.UnloadContextAsync(pluginId);
                if (!unloaded)
                {
                    _logger.LogWarning("Plugin {PluginId} context could not be fully unloaded - references may still exist", pluginId);
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

    /// <summary>
    /// Gets the load context manager for advanced operations.
    /// </summary>
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

        // Dispose context manager to unload all contexts
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
/// Plugin manifest from plugin.json.
/// </summary>
internal class PluginManifest
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? Main { get; set; }
    public string[]? Dependencies { get; set; }
}
