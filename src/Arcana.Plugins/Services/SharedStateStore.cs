using System.Collections.Concurrent;
using Arcana.Plugins.Contracts;

namespace Arcana.Plugins.Services;

/// <summary>
/// Shared state store implementation for cross-plugin state.
/// 跨插件共享狀態存儲實作
/// </summary>
public class SharedStateStore : ISharedStateStore
{
    private readonly ConcurrentDictionary<string, object?> _state = new();
    private readonly ConcurrentDictionary<string, List<Delegate>> _watchers = new();
    private readonly object _lock = new();

    public T? Get<T>(string key)
    {
        if (_state.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    public void Set<T>(string key, T value)
    {
        _state[key] = value;
        NotifyWatchers(key, value);
    }

    public bool Remove(string key)
    {
        var removed = _state.TryRemove(key, out var oldValue);
        if (removed)
        {
            NotifyWatchers<object?>(key, default);
        }
        return removed;
    }

    public bool ContainsKey(string key)
    {
        return _state.ContainsKey(key);
    }

    public IDisposable OnChange<T>(string key, Action<T?> handler)
    {
        lock (_lock)
        {
            if (!_watchers.TryGetValue(key, out var watchers))
            {
                watchers = new List<Delegate>();
                _watchers[key] = watchers;
            }
            watchers.Add(handler);
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                if (_watchers.TryGetValue(key, out var watchers))
                {
                    watchers.Remove(handler);
                }
            }
        });
    }

    private void NotifyWatchers<T>(string key, T? value)
    {
        if (!_watchers.TryGetValue(key, out var watchers))
        {
            return;
        }

        List<Delegate> watchersCopy;
        lock (_lock)
        {
            watchersCopy = watchers.ToList();
        }

        foreach (var watcher in watchersCopy)
        {
            try
            {
                if (watcher is Action<T?> handler)
                {
                    handler(value);
                }
            }
            catch
            {
                // Ignore errors in handlers
            }
        }
    }

    private class Subscription : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public Subscription(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _dispose();
            }
        }
    }
}
