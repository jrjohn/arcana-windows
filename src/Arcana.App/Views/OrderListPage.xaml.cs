using Arcana.App.ViewModels;
using Arcana.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Arcana.App.Views;

/// <summary>
/// Order list page.
/// 訂單列表頁面
/// </summary>
public sealed partial class OrderListPage : Page
{
    private OrderListViewModel ViewModel { get; }

    public OrderListPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<OrderListViewModel>();
        DataContext = ViewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
        UpdateUI();
    }

    private void UpdateUI()
    {
        OrdersListView.ItemsSource = ViewModel.Orders;
        OrderCountText.Text = $"共 {ViewModel.TotalCount} 筆訂單";
        PageInfo.Text = $"第 {ViewModel.CurrentPage} 頁 / 共 {ViewModel.TotalPages} 頁";
        PrevButton.IsEnabled = ViewModel.CurrentPage > 1;
        NextButton.IsEnabled = ViewModel.CurrentPage < ViewModel.TotalPages;

        var start = (ViewModel.CurrentPage - 1) * ViewModel.PageSize + 1;
        var end = Math.Min(ViewModel.CurrentPage * ViewModel.PageSize, ViewModel.TotalCount);
        PaginationInfo.Text = $"第 {start}-{end} 筆，共 {ViewModel.TotalCount} 筆";
    }

    private async void NewOrder_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.CreateOrderCommand.ExecuteAsync(null);
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshCommand.ExecuteAsync(null);
        UpdateUI();
    }

    private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ViewModel.SearchText = args.QueryText;
        await ViewModel.SearchCommand.ExecuteAsync(null);
        UpdateUI();
    }

    private async void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        StatusFilter.SelectedIndex = 0;
        await ViewModel.ClearFilterCommand.ExecuteAsync(null);
        UpdateUI();
    }

    private async void OrdersListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Order order)
        {
            await ViewModel.ViewOrderCommand.ExecuteAsync(order);
        }
    }

    private async void EditOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is Order order)
        {
            await ViewModel.EditOrderCommand.ExecuteAsync(order);
        }
    }

    private async void DeleteOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is Order order)
        {
            await ViewModel.DeleteOrderCommand.ExecuteAsync(order);
            UpdateUI();
        }
    }

    private async void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.PreviousPageCommand.ExecuteAsync(null);
        UpdateUI();
    }

    private async void NextPage_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.NextPageCommand.ExecuteAsync(null);
        UpdateUI();
    }
}
