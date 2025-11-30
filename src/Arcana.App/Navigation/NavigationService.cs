using Arcana.Plugins.Contracts;
using NavEventArgs = Arcana.Plugins.Contracts.NavigationEventArgs;

namespace Arcana.App.Navigation;

/// <summary>
/// Navigation service implementation for WinUI.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IViewRegistry _viewRegistry;
    private readonly Stack<string> _backStack = new();
    private readonly Stack<string> _forwardStack = new();

    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => _forwardStack.Count > 0;
    public string? CurrentViewId { get; private set; }

    public event EventHandler<NavEventArgs>? Navigated;
    public event EventHandler<NavigatingCancelEventArgs>? Navigating;

    public NavigationService(IViewRegistry viewRegistry)
    {
        _viewRegistry = viewRegistry;
    }

    public Task<bool> NavigateToAsync(string viewId, object? parameter = null)
    {
        var previousViewId = CurrentViewId;

        // Raise navigating event
        var navigatingArgs = new NavigatingCancelEventArgs
        {
            FromViewId = previousViewId,
            ToViewId = viewId,
            Parameter = parameter
        };
        Navigating?.Invoke(this, navigatingArgs);
        if (navigatingArgs.Cancel)
        {
            return Task.FromResult(false);
        }

        // Save to back stack
        if (!string.IsNullOrEmpty(CurrentViewId))
        {
            _backStack.Push(CurrentViewId);
            _forwardStack.Clear();
        }

        CurrentViewId = viewId;
        Navigated?.Invoke(this, new NavEventArgs
        {
            FromViewId = previousViewId,
            ToViewId = viewId,
            Parameter = parameter,
            NavigationType = NavigationType.Forward
        });
        return Task.FromResult(true);
    }

    public Task<bool> NavigateToNewTabAsync(string viewId, object? parameter = null)
    {
        var previousViewId = CurrentViewId;
        CurrentViewId = viewId;
        Navigated?.Invoke(this, new NavEventArgs
        {
            FromViewId = previousViewId,
            ToViewId = viewId,
            Parameter = parameter,
            NavigationType = NavigationType.NewTab
        });
        return Task.FromResult(true);
    }

    public Task<bool> NavigateWithinTabAsync(string parentViewId, string viewId, object? parameter = null)
    {
        var previousViewId = CurrentViewId;
        CurrentViewId = viewId;

        // Use MainWindow for navigation within tab
        if (App.MainWindow is MainWindow mainWindow)
        {
            mainWindow.NavigateWithinTab(parentViewId, viewId, parameter);
        }

        Navigated?.Invoke(this, new NavEventArgs
        {
            FromViewId = previousViewId,
            ToViewId = viewId,
            Parameter = parameter,
            NavigationType = NavigationType.Forward
        });
        return Task.FromResult(true);
    }

    public Task<bool> GoBackAsync()
    {
        if (!CanGoBack) return Task.FromResult(false);

        var previousViewId = CurrentViewId;
        if (!string.IsNullOrEmpty(previousViewId))
        {
            _forwardStack.Push(previousViewId);
        }

        CurrentViewId = _backStack.Pop();
        Navigated?.Invoke(this, new NavEventArgs
        {
            FromViewId = previousViewId,
            ToViewId = CurrentViewId,
            NavigationType = NavigationType.Back
        });
        return Task.FromResult(true);
    }

    public Task<bool> GoForwardAsync()
    {
        if (!CanGoForward) return Task.FromResult(false);

        var previousViewId = CurrentViewId;
        if (!string.IsNullOrEmpty(previousViewId))
        {
            _backStack.Push(previousViewId);
        }

        CurrentViewId = _forwardStack.Pop();
        Navigated?.Invoke(this, new NavEventArgs
        {
            FromViewId = previousViewId,
            ToViewId = CurrentViewId,
            NavigationType = NavigationType.Forward
        });
        return Task.FromResult(true);
    }

    public IReadOnlyList<ViewDefinition> GetAvailableViews()
    {
        return _viewRegistry.GetAllViews();
    }

    public IReadOnlyList<ViewDefinition> GetViewsByCategory(string category)
    {
        return _viewRegistry.GetAllViews()
            .Where(v => v.Category == category)
            .OrderBy(v => v.Order)
            .ToList();
    }

    public Task<TResult?> ShowDialogAsync<TResult>(string viewId, object? parameter = null)
    {
        // Dialog implementation
        return Task.FromResult<TResult?>(default);
    }

    public Task CloseAsync()
    {
        // Close current tab/dialog
        return Task.CompletedTask;
    }
}
