using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Arcana.App.Views;

/// <summary>
/// Home page.
/// </summary>
public sealed partial class HomePage : Page
{
    public HomePage()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
    {
        // TODO: Load actual dashboard data
        TodayOrdersCount.Text = "12";
        TodayRevenue.Text = "$45,600";
        PendingOrdersCount.Text = "5";
        TotalCustomers.Text = "128";
        LastSyncTime.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm");

        await Task.CompletedTask;
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
