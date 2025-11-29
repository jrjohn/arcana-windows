using Arcana.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace Arcana.App.Navigation;

/// <summary>
/// Dynamic navigation service that uses IViewRegistry for page resolution.
/// 使用 IViewRegistry 進行頁面解析的動態導航服務
/// </summary>
public class DynamicNavigationService : INavigationService
{
    private readonly IViewRegistry _viewRegistry;
    private readonly IServiceProvider _serviceProvider;
    private Frame? _frame;
    private TabView? _tabView;
    private readonly Stack<NavigationEntry> _backStack = new();
    private readonly Stack<NavigationEntry> _forwardStack = new();

    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => _forwardStack.Count > 0;
    public string? CurrentViewId { get; private set; }

    public event EventHandler<NavigationEventArgs>? Navigated;
    public event EventHandler<NavigatingEventArgs>? Navigating;

    public DynamicNavigationService(IViewRegistry viewRegistry, IServiceProvider serviceProvider)
    {
        _viewRegistry = viewRegistry;
        _serviceProvider = serviceProvider;

        // Subscribe to view registry changes
        _viewRegistry.ViewsChanged += OnViewsChanged;
    }

    /// <summary>
    /// Initializes the navigation service with the main frame and tab view.
    /// </summary>
    public void Initialize(Frame frame, TabView? tabView = null)
    {
        _frame = frame;
        _tabView = tabView;
    }

    public Task NavigateToAsync(string viewId, object? parameter = null)
    {
        return NavigateInternalAsync(viewId, parameter, false);
    }

    public Task NavigateToNewTabAsync(string viewId, object? parameter = null)
    {
        return NavigateInternalAsync(viewId, parameter, true);
    }

    private async Task NavigateInternalAsync(string viewId, object? parameter, bool newTab)
    {
        var view = _viewRegistry.GetView(viewId);
        if (view == null)
        {
            throw new InvalidOperationException($"View not found: {viewId}");
        }

        // Raise navigating event
        var navigatingArgs = new NavigatingEventArgs(viewId, parameter);
        Navigating?.Invoke(this, navigatingArgs);
        if (navigatingArgs.Cancel)
        {
            return;
        }

        // Save current to back stack
        if (!string.IsNullOrEmpty(CurrentViewId))
        {
            _backStack.Push(new NavigationEntry(CurrentViewId, null));
            _forwardStack.Clear();
        }

        if (newTab && _tabView != null)
        {
            await NavigateToTabAsync(view, parameter);
        }
        else if (_frame != null)
        {
            NavigateToFrame(view, parameter);
        }

        CurrentViewId = viewId;

        // Raise navigated event
        Navigated?.Invoke(this, new NavigationEventArgs(viewId, parameter));
    }

    private void NavigateToFrame(ViewDefinition view, object? parameter)
    {
        if (_frame == null) return;

        var pageType = view.ViewType;
        if (pageType != null)
        {
            _frame.Navigate(pageType, parameter);

            // Set view model if available
            if (_frame.Content != null && view.ViewModelType != null)
            {
                var viewModel = _serviceProvider.GetService(view.ViewModelType);
                if (viewModel != null && _frame.Content is FrameworkElement fe)
                {
                    fe.DataContext = viewModel;
                }
            }
        }
    }

    private Task NavigateToTabAsync(ViewDefinition view, object? parameter)
    {
        if (_tabView == null) return Task.CompletedTask;

        // Check if tab already exists for single-instance views
        if (!view.CanHaveMultipleInstances)
        {
            foreach (var item in _tabView.TabItems)
            {
                if (item is TabViewItem existingTab && existingTab.Tag?.ToString() == view.Id)
                {
                    _tabView.SelectedItem = existingTab;
                    return Task.CompletedTask;
                }
            }
        }

        // Create new tab
        var frame = new Frame();
        var pageType = view.ViewType;

        if (pageType != null)
        {
            frame.Navigate(pageType, parameter);

            // Set view model if available
            if (frame.Content != null && view.ViewModelType != null)
            {
                var viewModel = _serviceProvider.GetService(view.ViewModelType);
                if (viewModel != null && frame.Content is FrameworkElement fe)
                {
                    fe.DataContext = viewModel;
                }
            }
        }

        var tab = new TabViewItem
        {
            Header = GetTabHeader(view, parameter),
            Tag = view.Id,
            Content = frame,
            IconSource = GetIconSource(view.Icon)
        };

        _tabView.TabItems.Add(tab);
        _tabView.SelectedItem = tab;

        return Task.CompletedTask;
    }

    private static string GetTabHeader(ViewDefinition view, object? parameter)
    {
        if (parameter is int id)
        {
            return $"{view.Title} #{id}";
        }
        return view.Title;
    }

    private static IconSource? GetIconSource(string? icon)
    {
        if (string.IsNullOrEmpty(icon)) return null;
        return new FontIconSource { Glyph = icon };
    }

    public Task GoBackAsync()
    {
        if (!CanGoBack) return Task.CompletedTask;

        var entry = _backStack.Pop();
        if (!string.IsNullOrEmpty(CurrentViewId))
        {
            _forwardStack.Push(new NavigationEntry(CurrentViewId, null));
        }

        return NavigateToAsync(entry.ViewId, entry.Parameter);
    }

    public Task GoForwardAsync()
    {
        if (!CanGoForward) return Task.CompletedTask;

        var entry = _forwardStack.Pop();
        if (!string.IsNullOrEmpty(CurrentViewId))
        {
            _backStack.Push(new NavigationEntry(CurrentViewId, null));
        }

        return NavigateToAsync(entry.ViewId, entry.Parameter);
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

    private void OnViewsChanged(object? sender, EventArgs e)
    {
        // Could refresh navigation UI here
    }

    private record NavigationEntry(string ViewId, object? Parameter);
}

/// <summary>
/// Navigation event args.
/// </summary>
public class NavigationEventArgs : EventArgs
{
    public string ViewId { get; }
    public object? Parameter { get; }

    public NavigationEventArgs(string viewId, object? parameter)
    {
        ViewId = viewId;
        Parameter = parameter;
    }
}

/// <summary>
/// Navigating event args with cancel support.
/// </summary>
public class NavigatingEventArgs : EventArgs
{
    public string ViewId { get; }
    public object? Parameter { get; }
    public bool Cancel { get; set; }

    public NavigatingEventArgs(string viewId, object? parameter)
    {
        ViewId = viewId;
        Parameter = parameter;
    }
}
