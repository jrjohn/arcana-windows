namespace Arcana.Plugins.Contracts;

/// <summary>
/// View registry for registering view contributions.
/// 視圖註冊表，用於註冊視圖貢獻
/// </summary>
public interface IViewRegistry
{
    /// <summary>
    /// Registers a view.
    /// </summary>
    IDisposable RegisterView(ViewDefinition view);

    /// <summary>
    /// Registers a view factory.
    /// </summary>
    IDisposable RegisterViewFactory(string viewId, Func<object> factory);

    /// <summary>
    /// Gets a view definition by ID.
    /// </summary>
    ViewDefinition? GetView(string viewId);

    /// <summary>
    /// Gets all registered views.
    /// </summary>
    IReadOnlyList<ViewDefinition> GetAllViews();

    /// <summary>
    /// Creates a view instance.
    /// </summary>
    object? CreateViewInstance(string viewId);

    /// <summary>
    /// Event raised when views change.
    /// </summary>
    event EventHandler? ViewsChanged;
}

/// <summary>
/// View definition.
/// 視圖定義
/// </summary>
public record ViewDefinition
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Icon { get; init; }
    public ViewType Type { get; init; } = ViewType.Page;
    public Type? ViewType { get; init; }
    public Type? ViewModelType { get; init; }
    public bool CanHaveMultipleInstances { get; init; }
    public string? Category { get; init; }
    public int Order { get; init; }
}

/// <summary>
/// View type enumeration.
/// </summary>
public enum ViewType
{
    Page,       // 頁面視圖
    Dialog,     // 對話框
    Panel,      // 面板
    Widget,     // 小工具
    Flyout      // 飛出視窗
}

/// <summary>
/// Navigation service interface.
/// 導航服務介面
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to a view.
    /// </summary>
    Task<bool> NavigateToAsync(string viewId, object? parameter = null);

    /// <summary>
    /// Navigates to a view in a new tab.
    /// </summary>
    Task<bool> NavigateToNewTabAsync(string viewId, object? parameter = null);

    /// <summary>
    /// Goes back.
    /// </summary>
    Task<bool> GoBackAsync();

    /// <summary>
    /// Goes forward.
    /// </summary>
    Task<bool> GoForwardAsync();

    /// <summary>
    /// Gets whether we can go back.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Gets whether we can go forward.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// Gets the current view ID.
    /// </summary>
    string? CurrentViewId { get; }

    /// <summary>
    /// Gets all available views for navigation.
    /// </summary>
    IReadOnlyList<ViewDefinition> GetAvailableViews();

    /// <summary>
    /// Gets views by category.
    /// </summary>
    IReadOnlyList<ViewDefinition> GetViewsByCategory(string category);

    /// <summary>
    /// Opens a dialog.
    /// </summary>
    Task<TResult?> ShowDialogAsync<TResult>(string viewId, object? parameter = null);

    /// <summary>
    /// Closes the current tab or dialog.
    /// </summary>
    Task CloseAsync();

    /// <summary>
    /// Event raised when navigation occurs.
    /// </summary>
    event EventHandler<NavigationEventArgs>? Navigated;

    /// <summary>
    /// Event raised before navigation occurs (can be cancelled).
    /// </summary>
    event EventHandler<NavigatingCancelEventArgs>? Navigating;
}

/// <summary>
/// Navigating event args with cancel support.
/// </summary>
public class NavigatingCancelEventArgs : EventArgs
{
    public string? FromViewId { get; init; }
    public string? ToViewId { get; init; }
    public object? Parameter { get; init; }
    public bool Cancel { get; set; }
}

/// <summary>
/// Navigation event args.
/// </summary>
public class NavigationEventArgs : EventArgs
{
    public string? FromViewId { get; init; }
    public string? ToViewId { get; init; }
    public object? Parameter { get; init; }
    public NavigationType NavigationType { get; init; }
}

/// <summary>
/// Navigation type.
/// </summary>
public enum NavigationType
{
    Forward,
    Back,
    NewTab,
    Replace
}
