using System.Collections.Concurrent;
using Arcana.Plugins.Contracts;

namespace Arcana.Plugins.Services;

/// <summary>
/// View registry implementation.
/// 視圖註冊表實作
/// </summary>
public class ViewRegistry : IViewRegistry
{
    private readonly ConcurrentDictionary<string, ViewDefinition> _views = new();
    private readonly ConcurrentDictionary<string, Func<object>> _factories = new();

    public event EventHandler? ViewsChanged;

    public IDisposable RegisterView(ViewDefinition view)
    {
        if (!_views.TryAdd(view.Id, view))
        {
            throw new InvalidOperationException($"View already registered: {view.Id}");
        }

        OnViewsChanged();

        return new Subscription(() =>
        {
            _views.TryRemove(view.Id, out _);
            _factories.TryRemove(view.Id, out _);
            OnViewsChanged();
        });
    }

    public IDisposable RegisterViewFactory(string viewId, Func<object> factory)
    {
        _factories[viewId] = factory;

        return new Subscription(() =>
        {
            _factories.TryRemove(viewId, out _);
        });
    }

    public ViewDefinition? GetView(string viewId)
    {
        return _views.TryGetValue(viewId, out var view) ? view : null;
    }

    public IReadOnlyList<ViewDefinition> GetAllViews()
    {
        return _views.Values.OrderBy(v => v.Order).ToList();
    }

    public object? CreateViewInstance(string viewId)
    {
        if (_factories.TryGetValue(viewId, out var factory))
        {
            return factory();
        }

        if (_views.TryGetValue(viewId, out var view) && view.ViewType != null)
        {
            return Activator.CreateInstance(view.ViewType);
        }

        return null;
    }

    private void OnViewsChanged()
    {
        ViewsChanged?.Invoke(this, EventArgs.Empty);
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
