using Arcana.App.ViewModels;
using Arcana.Domain.Entities;
using Arcana.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Arcana.App.Views;

/// <summary>
/// Order list page.
/// </summary>
public sealed partial class OrderListPage : Page
{
    private OrderListViewModel ViewModel { get; }
    private readonly ILocalizationService _localization;
    private readonly INavigationService _navigationService;

    public OrderListPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<OrderListViewModel>();
        _localization = App.Services.GetRequiredService<ILocalizationService>();
        _navigationService = App.Services.GetRequiredService<INavigationService>();
        _localization.CultureChanged += OnCultureChanged;
        DataContext = ViewModel;
        Loaded += OnLoaded;
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplyLocalization);
    }

    private void ApplyLocalization()
    {
        // Page title
        PageTitle.Text = _localization.Get("order.list");

        // Command bar buttons
        AddOrderButton.Label = _localization.Get("order.new");
        RefreshButton.Label = _localization.Get("common.refresh");
        ExportButton.Label = _localization.Get("common.export");
        PrintButton.Label = _localization.Get("common.print");

        // Search box
        SearchBox.PlaceholderText = _localization.Get("order.searchPlaceholder");

        // Status filter
        StatusFilter.PlaceholderText = _localization.Get("order.statusFilter");
        StatusAll.Content = _localization.Get("common.all");
        StatusDraft.Content = _localization.Get("order.status.draft");
        StatusPending.Content = _localization.Get("order.status.pending");
        StatusConfirmed.Content = _localization.Get("order.status.confirmed");
        StatusProcessing.Content = _localization.Get("order.status.processing");
        StatusShipped.Content = _localization.Get("order.status.shipped");
        StatusCompleted.Content = _localization.Get("order.status.completed");
        StatusCancelled.Content = _localization.Get("order.status.cancelled");

        // Date pickers - set language for calendar UI
        var currentCulture = _localization.CurrentCulture.Name;
        FromDatePicker.Language = currentCulture;
        ToDatePicker.Language = currentCulture;
        FromDatePicker.PlaceholderText = _localization.Get("order.fromDate");
        ToDatePicker.PlaceholderText = _localization.Get("order.toDate");

        // Clear filter button
        ClearFilterButton.Content = _localization.Get("common.clearFilter");

        // Column headers
        ColOrderNumber.Text = _localization.Get("order.number");
        ColDate.Text = _localization.Get("order.date");
        ColCustomer.Text = _localization.Get("order.customer");
        ColAmount.Text = _localization.Get("order.amount");
        ColPaymentStatus.Text = _localization.Get("order.paymentStatus");
        ColOrderStatus.Text = _localization.Get("order.orderStatus");
        ColActions.Text = _localization.Get("common.actions");

        // Pagination buttons
        PrevButton.Content = _localization.Get("common.prevPage");
        NextButton.Content = _localization.Get("common.nextPage");

        // Update dynamic texts
        UpdateUI();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyLocalization();
        await ViewModel.InitializeAsync();
        UpdateUI();
    }

    private void UpdateUI()
    {
        OrdersListView.ItemsSource = ViewModel.Orders;
        OrderCountText.Text = string.Format(_localization.Get("order.countFormat"), ViewModel.TotalCount);
        PageInfo.Text = string.Format(_localization.Get("common.pageFormat"), ViewModel.CurrentPage, ViewModel.TotalPages);
        PrevButton.IsEnabled = ViewModel.CurrentPage > 1;
        NextButton.IsEnabled = ViewModel.CurrentPage < ViewModel.TotalPages;

        var start = (ViewModel.CurrentPage - 1) * ViewModel.PageSize + 1;
        var end = Math.Min(ViewModel.CurrentPage * ViewModel.PageSize, ViewModel.TotalCount);
        PaginationInfo.Text = string.Format(_localization.Get("common.showingFormat"), start, end, ViewModel.TotalCount);
    }

    private void NewOrder_Click(object sender, RoutedEventArgs e)
    {
        // Try to find parent OrderModulePage
        var parent = FindParentOrderModulePage();
        if (parent != null)
        {
            parent.OpenNewOrder();
        }
        else
        {
            // Fallback: navigate within the same tab's Frame
            Frame.Navigate(typeof(OrderDetailPage), null);
        }
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

    private void OrdersListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Order order)
        {
            OpenOrderInParentModule(order.Id);
        }
    }

    private void EditOrder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is Order order)
        {
            OpenOrderInParentModule(order.Id);
        }
    }

    private void OpenOrderInParentModule(int orderId)
    {
        // Try to find parent OrderModulePage
        var parent = FindParentOrderModulePage();
        if (parent != null)
        {
            parent.OpenOrder(orderId);
        }
        else
        {
            // Fallback: navigate through navigation service
            _ = _navigationService.NavigateWithinTabAsync("OrderListPage", "OrderDetailPage", orderId);
        }
    }

    private OrderModulePage? FindParentOrderModulePage()
    {
        // Navigate up the visual tree to find the OrderModulePage
        DependencyObject? current = this;
        while (current != null)
        {
            if (current is Frame frame && frame.Parent is TabViewItem tabItem &&
                tabItem.Parent is TabView tabView && tabView.Parent is Grid grid &&
                grid.Parent is OrderModulePage modulePage)
            {
                return modulePage;
            }

            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }

        // Alternative: check if we're inside a Frame whose content's parent is OrderModulePage
        if (Frame?.Parent is TabViewItem item &&
            Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(item) is TabView tv &&
            Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(tv) is Grid g &&
            Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(g) is OrderModulePage page)
        {
            return page;
        }

        return null;
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
