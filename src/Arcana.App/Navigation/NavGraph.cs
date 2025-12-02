using Arcana.Plugins.Contracts;

namespace Arcana.App.Navigation;

/// <summary>
/// Centralized navigation graph for type-safe navigation.
/// All navigation routes and actions are defined here.
/// Implements INavGraph for plugin consumption.
/// </summary>
public sealed class NavGraph : INavGraph
{
    private readonly INavigationService _navigationService;

    public NavGraph(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    // ============================================================
    // INavGraph Implementation
    // ============================================================

    /// <inheritdoc />
    public Task<bool> To(string routeId, object? parameter = null)
        => _navigationService.NavigateToAsync(routeId, parameter);

    /// <inheritdoc />
    public Task<bool> ToNewTab(string routeId, object? parameter = null)
        => _navigationService.NavigateToNewTabAsync(routeId, parameter);

    /// <inheritdoc />
    public Task<bool> ToWithinTab(string parentRouteId, string routeId, object? parameter = null)
        => _navigationService.NavigateWithinTabAsync(parentRouteId, routeId, parameter);

    /// <inheritdoc />
    public Task<bool> Back() => _navigationService.GoBackAsync();

    /// <inheritdoc />
    public Task<bool> Forward() => _navigationService.GoForwardAsync();

    /// <inheritdoc />
    public Task Close() => _navigationService.CloseAsync();

    /// <inheritdoc />
    public Task<TResult?> ShowDialog<TResult>(string routeId, object? parameter = null)
        => _navigationService.ShowDialogAsync<TResult>(routeId, parameter);

    /// <inheritdoc />
    public bool CanGoBack => _navigationService.CanGoBack;

    /// <inheritdoc />
    public bool CanGoForward => _navigationService.CanGoForward;

    /// <inheritdoc />
    public string? CurrentRouteId => _navigationService.CurrentViewId;

    // ============================================================
    // ROUTES - All navigation destinations
    // ============================================================

    public static class Routes
    {
        // Orders Module
        public const string OrderList = "OrderListPage";
        public const string OrderDetail = "OrderDetailPage";

        // Products Module
        public const string ProductList = "ProductListPage";
        public const string ProductDetail = "ProductDetailPage";

        // Customers Module
        public const string CustomerList = "CustomerListPage";
        public const string CustomerDetail = "CustomerDetailPage";

        // System
        public const string PluginManager = "PluginManagerPage";
        public const string Settings = "SettingsPage";
        public const string Dashboard = "DashboardPage";
    }

    // ============================================================
    // NAVIGATION ACTIONS
    // ============================================================

    #region Orders

    /// <summary>
    /// Navigate to order list.
    /// </summary>
    public Task<bool> ToOrderList()
        => _navigationService.NavigateToNewTabAsync(Routes.OrderList);

    /// <summary>
    /// Navigate to order detail for viewing.
    /// </summary>
    public Task<bool> ToOrderDetail(int orderId, bool readOnly = false)
        => _navigationService.NavigateToNewTabAsync(Routes.OrderDetail, new OrderDetailArgs(orderId, readOnly));

    /// <summary>
    /// Navigate to create new order.
    /// </summary>
    public Task<bool> ToNewOrder()
        => _navigationService.NavigateToNewTabAsync(Routes.OrderDetail, new OrderDetailArgs(null, false));

    #endregion

    #region Products

    /// <summary>
    /// Navigate to product list.
    /// </summary>
    public Task<bool> ToProductList()
        => _navigationService.NavigateToNewTabAsync(Routes.ProductList);

    /// <summary>
    /// Navigate to product detail.
    /// </summary>
    public Task<bool> ToProductDetail(int productId)
        => _navigationService.NavigateToNewTabAsync(Routes.ProductDetail, productId);

    /// <summary>
    /// Navigate to create new product.
    /// </summary>
    public Task<bool> ToNewProduct()
        => _navigationService.NavigateToNewTabAsync(Routes.ProductDetail);

    #endregion

    #region Customers

    /// <summary>
    /// Navigate to customer list.
    /// </summary>
    public Task<bool> ToCustomerList()
        => _navigationService.NavigateToNewTabAsync(Routes.CustomerList);

    /// <summary>
    /// Navigate to customer detail.
    /// </summary>
    public Task<bool> ToCustomerDetail(int customerId)
        => _navigationService.NavigateToNewTabAsync(Routes.CustomerDetail, customerId);

    /// <summary>
    /// Navigate to create new customer.
    /// </summary>
    public Task<bool> ToNewCustomer()
        => _navigationService.NavigateToNewTabAsync(Routes.CustomerDetail);

    #endregion

    #region System

    /// <summary>
    /// Navigate to plugin manager.
    /// </summary>
    public Task<bool> ToPluginManager()
        => _navigationService.NavigateToNewTabAsync(Routes.PluginManager);

    /// <summary>
    /// Navigate to settings.
    /// </summary>
    public Task<bool> ToSettings()
        => _navigationService.NavigateToNewTabAsync(Routes.Settings);

    /// <summary>
    /// Navigate to dashboard.
    /// </summary>
    public Task<bool> ToDashboard()
        => _navigationService.NavigateToNewTabAsync(Routes.Dashboard);

    #endregion

    // Common Actions (Back, Forward, Close, To, ShowDialog) are implemented via INavGraph interface above

    // ============================================================
    // NAVIGATION ARGS
    // ============================================================

    public record OrderDetailArgs(int? OrderId, bool ReadOnly);
}
