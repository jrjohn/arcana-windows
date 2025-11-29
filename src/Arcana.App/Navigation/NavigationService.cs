using Arcana.Plugins.Contracts;

namespace Arcana.App.Navigation;

/// <summary>
/// Navigation service implementation for WinUI.
/// WinUI 導航服務實作
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IViewRegistry _viewRegistry;

    public bool CanGoBack => false; // Implement with Frame navigation stack
    public string? CurrentViewId { get; private set; }

    public event EventHandler<NavigationEventArgs>? Navigated;

    public NavigationService(IViewRegistry viewRegistry)
    {
        _viewRegistry = viewRegistry;
    }

    public Task<bool> NavigateToAsync(string viewId, object? parameter = null)
    {
        // Implementation will be connected to MainWindow
        CurrentViewId = viewId;
        Navigated?.Invoke(this, new NavigationEventArgs
        {
            ToViewId = viewId,
            Parameter = parameter,
            NavigationType = NavigationType.Forward
        });
        return Task.FromResult(true);
    }

    public Task<bool> NavigateToNewTabAsync(string viewId, object? parameter = null)
    {
        CurrentViewId = viewId;
        Navigated?.Invoke(this, new NavigationEventArgs
        {
            ToViewId = viewId,
            Parameter = parameter,
            NavigationType = NavigationType.NewTab
        });
        return Task.FromResult(true);
    }

    public Task<bool> GoBackAsync()
    {
        // Implement with Frame navigation
        return Task.FromResult(false);
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
