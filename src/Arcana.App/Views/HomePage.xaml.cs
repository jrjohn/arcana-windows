using Arcana.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Arcana.App.Views;

/// <summary>
/// Home page.
/// </summary>
public sealed partial class HomePage : Page
{
    private readonly ILocalizationService _localization;

    public HomePage()
    {
        this.InitializeComponent();
        _localization = App.Services.GetRequiredService<ILocalizationService>();
        _localization.CultureChanged += OnCultureChanged;

        Loaded += OnLoaded;
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplyLocalization);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyLocalization();
        await LoadDashboardDataAsync();
    }

    private void ApplyLocalization()
    {
        // Header
        WelcomeText.Text = _localization.Get("home.welcome");
        SubtitleText.Text = _localization.Get("home.subtitle");

        // Quick Actions
        QuickActionsTitle.Text = _localization.Get("home.quickActions");
        NewOrderText.Text = _localization.Get("home.newOrder");
        NewCustomerText.Text = _localization.Get("home.newCustomer");
        NewProductText.Text = _localization.Get("home.newProduct");
        ViewReportsText.Text = _localization.Get("home.viewReports");

        // Dashboard Cards
        TodayOrdersLabel.Text = _localization.Get("home.todayOrders");
        TodayOrdersUnit.Text = _localization.Get("unit.count");
        TodayRevenueLabel.Text = _localization.Get("home.todayRevenue");
        TodayRevenueUnit.Text = _localization.Get("unit.currency");
        PendingOrdersLabel.Text = _localization.Get("home.pendingOrders");
        PendingOrdersUnit.Text = _localization.Get("unit.count");
        TotalCustomersLabel.Text = _localization.Get("home.totalCustomers");
        TotalCustomersUnit.Text = _localization.Get("unit.people");

        // Recent Orders
        RecentOrdersTitle.Text = _localization.Get("home.recentOrders");
        ViewAllLink.Content = _localization.Get("home.viewAll");
        NoRecentOrdersText.Text = _localization.Get("home.noRecentOrders");

        // System Info
        SystemInfoTitle.Text = _localization.Get("home.systemInfo");
        VersionLabel.Text = _localization.Get("home.version");
        DatabaseLabel.Text = _localization.Get("home.database");
        DatabaseStatus.Text = _localization.Get("home.connected");
        LastSyncLabel.Text = _localization.Get("home.lastSync");
    }

    private async Task LoadDashboardDataAsync()
    {
        // TODO: Load actual dashboard data
        TodayOrdersCount.Text = "12";
        TodayRevenue.Text = "$45,600";
        PendingOrdersCount.Text = "5";
        TotalCustomers.Text = "128";
        LastSyncTime.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm");

        // Update empty state visibility based on recent orders
        UpdateRecentOrdersEmptyState();

        await Task.CompletedTask;
    }

    private void UpdateRecentOrdersEmptyState()
    {
        var hasOrders = RecentOrdersList.Items.Count > 0;
        RecentOrdersList.Visibility = hasOrders ? Visibility.Visible : Visibility.Collapsed;
        RecentOrdersEmptyState.Visibility = hasOrders ? Visibility.Collapsed : Visibility.Visible;
    }

    private void NewOrder_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to new order page
    }

    private void NewCustomer_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to new customer page
    }

    private void NewProduct_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to new product page
    }

    private void ViewReports_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to reports page
    }

    private void ViewAllOrders_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to orders page
    }
}
