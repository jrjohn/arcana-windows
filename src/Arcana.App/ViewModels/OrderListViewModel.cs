using System.Collections.ObjectModel;
using Arcana.Core.Common;
using Arcana.Domain.Entities;
using Arcana.Domain.Services;
using Arcana.Plugins.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcana.App.ViewModels;

/// <summary>
/// ViewModel for order list page.
/// </summary>
public partial class OrderListViewModel : ViewModelBase
{
    private readonly IOrderService _orderService;
    private readonly INavigationService _navigationService;
    private readonly IWindowService _windowService;

    [ObservableProperty]
    private ObservableCollection<Order> _orders = new();

    [ObservableProperty]
    private Order? _selectedOrder;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _pageSize = 20;

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private OrderStatus? _statusFilter;

    [ObservableProperty]
    private DateTime? _fromDate;

    [ObservableProperty]
    private DateTime? _toDate;

    public OrderListViewModel(
        IOrderService orderService,
        INavigationService navigationService,
        IWindowService windowService)
    {
        _orderService = orderService;
        _navigationService = navigationService;
        _windowService = windowService;

        // Default date range to last 30 days
        ToDate = DateTime.Today;
        FromDate = DateTime.Today.AddDays(-30);
    }

    public override async Task InitializeAsync()
    {
        await LoadOrdersAsync();
    }

    [RelayCommand]
    private async Task LoadOrdersAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            var request = new PageRequest(CurrentPage, PageSize, "OrderDate", true);
            var result = await _orderService.GetOrdersAsync(request);

            if (result.IsSuccess)
            {
                Orders = new ObservableCollection<Order>(result.Value!.Items);
                TotalCount = result.Value.TotalCount;
                TotalPages = result.Value.TotalPages;
            }
            else
            {
                SetError(result.Error!.Message);
            }
        });
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadOrdersAsync();
    }

    [RelayCommand]
    private async Task ClearFilterAsync()
    {
        SearchText = null;
        StatusFilter = null;
        FromDate = DateTime.Today.AddDays(-30);
        ToDate = DateTime.Today;
        CurrentPage = 1;
        await LoadOrdersAsync();
    }

    [RelayCommand]
    private async Task GoToPageAsync(int page)
    {
        if (page >= 1 && page <= TotalPages)
        {
            CurrentPage = page;
            await LoadOrdersAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadOrdersAsync();
        }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadOrdersAsync();
        }
    }

    [RelayCommand]
    private async Task CreateOrderAsync()
    {
        await _navigationService.NavigateToNewTabAsync("OrderDetailPage");
    }

    [RelayCommand]
    private async Task EditOrderAsync(Order? order)
    {
        if (order != null)
        {
            await _navigationService.NavigateToNewTabAsync("OrderDetailPage", order.Id);
        }
    }

    [RelayCommand]
    private async Task ViewOrderAsync(Order? order)
    {
        if (order != null)
        {
            await _navigationService.NavigateToNewTabAsync("OrderDetailPage", order.Id);
        }
    }

    [RelayCommand]
    private async Task DeleteOrderAsync(Order? order)
    {
        if (order == null) return;

        var confirmed = await _windowService.ShowConfirmAsync(
            "Confirm Delete",
            $"Are you sure you want to delete order {order.OrderNumber}? This action cannot be undone.");

        if (confirmed)
        {
            var result = await _orderService.DeleteOrderAsync(order.Id);
            if (result.IsSuccess)
            {
                Orders.Remove(order);
                TotalCount--;
            }
            else
            {
                await _windowService.ShowErrorAsync(result.Error!.Message);
            }
        }
    }

    [RelayCommand]
    private async Task ChangeStatusAsync(Order? order)
    {
        if (order == null) return;

        // Show status picker dialog
        // For now, just cycle through statuses
        var nextStatus = order.Status switch
        {
            OrderStatus.Draft => OrderStatus.Pending,
            OrderStatus.Pending => OrderStatus.Confirmed,
            OrderStatus.Confirmed => OrderStatus.Processing,
            OrderStatus.Processing => OrderStatus.Shipped,
            OrderStatus.Shipped => OrderStatus.Delivered,
            OrderStatus.Delivered => OrderStatus.Completed,
            _ => order.Status
        };

        var result = await _orderService.ChangeStatusAsync(order.Id, nextStatus);
        if (result.IsSuccess)
        {
            order.Status = nextStatus;
            // Refresh the list
            await LoadOrdersAsync();
        }
        else
        {
            await _windowService.ShowErrorAsync(result.Error!.Message);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadOrdersAsync();
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        // Implement export functionality
        await _windowService.ShowInfoAsync("Export functionality is under development...");
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (SelectedOrder == null)
        {
            await _windowService.ShowWarningAsync("Please select an order to print");
            return;
        }

        // Implement print functionality
        await _windowService.ShowInfoAsync("Print functionality is under development...");
    }
}
