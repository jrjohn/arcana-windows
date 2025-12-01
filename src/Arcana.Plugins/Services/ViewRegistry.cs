using System.Collections.Concurrent;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Contracts.Validation;
using Microsoft.Extensions.Logging;

namespace Arcana.Plugins.Services;

/// <summary>
/// View registry implementation.
/// </summary>
public class ViewRegistry : IViewRegistry
{
    private readonly ConcurrentDictionary<string, ViewDefinition> _views = new();
    private readonly ConcurrentDictionary<string, Func<object>> _factories = new();
    private readonly ILogger<ViewRegistry>? _logger;

    public event EventHandler? ViewsChanged;

    public ViewRegistry() { }

    public ViewRegistry(ILogger<ViewRegistry> logger)
    {
        _logger = logger;
    }

    public IDisposable RegisterView(ViewDefinition view)
    {
        ValidateView(view);

        if (!_views.TryAdd(view.Id, view))
        {
            _logger?.LogInformation("View already registered, skipping: {ViewId}", view.Id);
            return new Subscription(() => { });
        }

        _logger?.LogInformation("Registered view: {ViewId} (ViewClass: {ViewClass})",
            view.Id, view.ViewClass?.Name ?? view.ViewClassName ?? "null");
        OnViewsChanged();

        return new Subscription(() =>
        {
            _logger?.LogInformation("Disposing view: {ViewId}", view.Id);
            _views.TryRemove(view.Id, out _);
            _factories.TryRemove(view.Id, out _);
            OnViewsChanged();
        });
    }

    private void ValidateView(ViewDefinition view)
    {
        var result = ViewValidator.Validate(view);

        // Log warnings
        foreach (var warning in result.Warnings)
        {
            _logger?.LogWarning("View validation warning: {Warning}", warning);
        }

        // Throw on errors
        if (!result.IsValid)
        {
            var errorMessage = string.Join("; ", result.Errors);
            _logger?.LogError("View validation failed: {Errors}", errorMessage);
            throw new ContributionValidationException($"View validation failed: {errorMessage}")
            {
                ValidationErrors = result.Errors
            };
        }
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

    public IReadOnlyList<ViewDefinition> GetModuleDefaultTabs(string moduleId)
    {
        return _views.Values
            .Where(v => v.ModuleId == moduleId && v.IsModuleDefaultTab)
            .OrderBy(v => v.ModuleTabOrder)
            .ToList();
    }

    public object? CreateViewInstance(string viewId)
    {
        if (_factories.TryGetValue(viewId, out var factory))
        {
            return factory();
        }

        if (_views.TryGetValue(viewId, out var view) && view.ViewClass != null)
        {
            return Activator.CreateInstance(view.ViewClass);
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
