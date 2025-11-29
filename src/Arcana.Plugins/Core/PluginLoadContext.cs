using System.Reflection;
using System.Runtime.Loader;

namespace Arcana.Plugins.Core;

/// <summary>
/// Assembly load context for plugin isolation with proper unloading support.
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _pluginPath;
    private bool _isUnloading;

    /// <summary>
    /// Gets the plugin path associated with this context.
    /// </summary>
    public string PluginPath => _pluginPath;

    /// <summary>
    /// Event raised when the context is being unloaded.
    /// </summary>
    public new event EventHandler? Unloading;

    public PluginLoadContext(string pluginPath) : base(name: Path.GetFileName(pluginPath), isCollectible: true)
    {
        _pluginPath = pluginPath;
        _resolver = new AssemblyDependencyResolver(pluginPath);

        // Subscribe to unloading event for cleanup
        base.Unloading += OnUnloading;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // First try to resolve from plugin directory
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // Don't load shared framework assemblies - let them come from default context
        // This prevents version conflicts for common libraries
        return null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return nint.Zero;
    }

    private void OnUnloading(AssemblyLoadContext context)
    {
        _isUnloading = true;
        Unloading?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Initiates unloading of this context.
    /// Note: Actual unloading happens when all references are released and GC runs.
    /// </summary>
    public void InitiateUnload()
    {
        if (!_isUnloading)
        {
            Unload();
        }
    }
}

/// <summary>
/// Wrapper that holds a weak reference to the plugin load context.
/// Used to verify that unloading actually occurred.
/// </summary>
public class PluginLoadContextReference
{
    private readonly WeakReference<PluginLoadContext> _weakReference;
    private readonly string _pluginId;

    public string PluginId => _pluginId;
    public bool IsAlive => _weakReference.TryGetTarget(out _);

    public PluginLoadContextReference(string pluginId, PluginLoadContext context)
    {
        _pluginId = pluginId;
        _weakReference = new WeakReference<PluginLoadContext>(context);
    }

    public PluginLoadContext? GetContext()
    {
        _weakReference.TryGetTarget(out var context);
        return context;
    }
}

/// <summary>
/// Manages plugin load contexts and ensures proper unloading.
/// </summary>
public class PluginLoadContextManager : IDisposable
{
    private readonly Dictionary<string, PluginLoadContextReference> _contexts = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new load context for a plugin.
    /// </summary>
    public PluginLoadContext CreateContext(string pluginId, string pluginPath)
    {
        lock (_lock)
        {
            // Unload existing context if present
            if (_contexts.TryGetValue(pluginId, out var existing))
            {
                UnloadContext(pluginId);
            }

            var context = new PluginLoadContext(pluginPath);
            _contexts[pluginId] = new PluginLoadContextReference(pluginId, context);
            return context;
        }
    }

    /// <summary>
    /// Gets an existing load context for a plugin.
    /// </summary>
    public PluginLoadContext? GetContext(string pluginId)
    {
        lock (_lock)
        {
            if (_contexts.TryGetValue(pluginId, out var reference))
            {
                return reference.GetContext();
            }
            return null;
        }
    }

    /// <summary>
    /// Unloads a plugin's load context.
    /// </summary>
    public async Task<bool> UnloadContextAsync(string pluginId, int maxAttempts = 10)
    {
        PluginLoadContext? context;

        lock (_lock)
        {
            if (!_contexts.TryGetValue(pluginId, out var reference))
            {
                return true; // Already unloaded
            }

            context = reference.GetContext();
            if (context == null)
            {
                _contexts.Remove(pluginId);
                return true; // Already collected
            }
        }

        // Initiate unload
        context.InitiateUnload();

        // Clear our reference
        lock (_lock)
        {
            if (_contexts.TryGetValue(pluginId, out var reference))
            {
                // Keep the weak reference to verify unloading
            }
        }

        // Wait for GC to collect the context
        for (int i = 0; i < maxAttempts; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            lock (_lock)
            {
                if (_contexts.TryGetValue(pluginId, out var reference) && !reference.IsAlive)
                {
                    _contexts.Remove(pluginId);
                    return true;
                }
            }

            await Task.Delay(100);
        }

        // Context still alive - something is holding a reference
        return false;
    }

    /// <summary>
    /// Synchronously unloads a plugin's load context.
    /// </summary>
    public void UnloadContext(string pluginId)
    {
        lock (_lock)
        {
            if (_contexts.TryGetValue(pluginId, out var reference))
            {
                var context = reference.GetContext();
                context?.InitiateUnload();
            }
        }

        // Force GC to help with unloading
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        lock (_lock)
        {
            if (_contexts.TryGetValue(pluginId, out var reference) && !reference.IsAlive)
            {
                _contexts.Remove(pluginId);
            }
        }
    }

    /// <summary>
    /// Gets the unload status of all plugins.
    /// </summary>
    public IReadOnlyDictionary<string, bool> GetUnloadStatus()
    {
        lock (_lock)
        {
            return _contexts.ToDictionary(kvp => kvp.Key, kvp => !kvp.Value.IsAlive);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            foreach (var kvp in _contexts)
            {
                var context = kvp.Value.GetContext();
                context?.InitiateUnload();
            }
            _contexts.Clear();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        GC.SuppressFinalize(this);
    }
}
