using System.Collections.ObjectModel;
using Arcana.App.Navigation;
using Arcana.App.ViewModels.Core;
using Arcana.Core.Common;
using Arcana.Domain.Entities;
using Arcana.Domain.Services;
using Arcana.Plugins.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels.Orders;

/// <summary>
/// ViewModel for order list page following UDF (Unidirectional Data Flow) pattern.
///
/// Architecture:
/// - INPUT:  Actions that trigger state changes (the only entry point)
/// - OUTPUT: Read-only reactive state exposed to View
/// - EFFECT: One-time events for side effects (dialogs, notifications)
///
/// Data Flow: View → Input → State Mutation → Output → View Re-render
/// </summary>
public partial class OrderListViewModel : ReactiveViewModelBase
{
    // ============ Dependencies ============
    private readonly IOrderService _orderService;
    private readonly NavGraph _nav;
    private readonly IWindowService _windowService;

    // ============ Private State ============

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

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    // ============ Input/Output/Effect ============
    private Input? _input;
    private Output? _output;
    private Effect? _effect;

    private ReadOnlyObservableCollection<Order>? _ordersReadOnly;

    public Input In => _input ??= new Input(this);
    public Output Out => _output ??= new Output(this);
    public Effect Fx => _effect ??= new Effect();

    // ============ Constructor ============
    public OrderListViewModel(
        IOrderService orderService,
        NavGraph nav,
        IWindowService windowService)
    {
        _orderService = orderService;
        _nav = nav;
        _windowService = windowService;

        // Default date range to last 30 days
        ToDate = DateTime.Today;
        FromDate = DateTime.Today.AddDays(-30);
    }

    // ============ Lifecycle ============
    public override async Task InitializeAsync()
    {
        await LoadOrdersAsync();
    }

    // ============ Internal Actions ============
    private async Task LoadOrdersAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsLoading = true;
            ClearError();

            var request = new PageRequest(CurrentPage, PageSize, "OrderDate", true);
            var result = await _orderService.GetOrdersAsync(request);

            if (result.IsSuccess)
            {
                Orders.Clear();
                foreach (var order in result.Value!.Items)
                {
                    Orders.Add(order);
                }
                TotalCount = result.Value.TotalCount;
                TotalPages = result.Value.TotalPages;

                Fx.DataRefreshed.Emit();
            }
            else
            {
                SetError(result.Error!);
            }
        }
        finally
        {
            IsBusy = false;
            IsLoading = false;
        }
    }

    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadOrdersAsync();
    }

    private async Task ClearFilterAsync()
    {
        SearchText = null;
        StatusFilter = null;
        FromDate = DateTime.Today.AddDays(-30);
        ToDate = DateTime.Today;
        CurrentPage = 1;
        await LoadOrdersAsync();
    }

    private async Task GoToPageAsync(int page)
    {
        if (page >= 1 && page <= TotalPages)
        {
            CurrentPage = page;
            await LoadOrdersAsync();
        }
    }

    private async Task PreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadOrdersAsync();
        }
    }

    private async Task NextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadOrdersAsync();
        }
    }

    private Task CreateOrderAsync()
    {
        return _nav.ToNewOrder();
    }

    private Task EditOrderAsync(Order order)
    {
        return _nav.ToOrderDetail(order.Id, readOnly: false);
    }

    private Task ViewOrderAsync(Order order)
    {
        return _nav.ToOrderDetail(order.Id, readOnly: true);
    }

    private async Task DeleteOrderAsync(Order order)
    {
        Fx.ConfirmDelete.Emit(new ConfirmDeleteRequest(order, async () =>
        {
            var result = await _orderService.DeleteOrderAsync(order.Id);
            if (result.IsSuccess)
            {
                Orders.Remove(order);
                TotalCount--;
                Fx.OrderDeleted.Emit(order);
                Fx.ShowSuccess.Emit($"Order {order.OrderNumber} deleted successfully");
            }
            else
            {
                Fx.ShowError.Emit(result.Error!);
            }
        }));
    }

    private async Task ChangeStatusAsync(Order order)
    {
        var oldStatus = order.Status;
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
            Fx.OrderStatusChanged.Emit(new StatusChangedEvent(order, oldStatus, nextStatus));
            await LoadOrdersAsync();
        }
        else
        {
            Fx.ShowError.Emit(result.Error!);
        }
    }

    private Task ExportAsync()
    {
        Fx.ShowInfo.Emit("Export functionality is under development...");
        return Task.CompletedTask;
    }

    private Task PrintAsync()
    {
        if (SelectedOrder == null)
        {
            Fx.ShowWarning.Emit("Please select an order to print");
            return Task.CompletedTask;
        }

        Fx.ShowInfo.Emit("Print functionality is under development...");
        return Task.CompletedTask;
    }

    // ============ Helper Methods ============
    private void SetError(AppError error)
    {
        ErrorMessage = error.Message;
        HasError = true;
        Fx.ShowError.Emit(error);
    }

    private void ClearError()
    {
        ErrorMessage = null;
        HasError = false;
    }

    // ============ Disposal ============
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _effect?.Dispose();
        }
        base.Dispose(disposing);
    }

    // ============================================================
    // NESTED CLASSES: Input, Output, Effect
    // ============================================================

    #region Input

    /// <summary>
    /// Input actions - the ONLY entry point for state changes.
    /// </summary>
    public sealed class Input : IViewModelInput
    {
        private readonly OrderListViewModel _vm;

        internal Input(OrderListViewModel vm) => _vm = vm;

        public Task Initialize() => _vm.InitializeAsync();
        public Task LoadOrders() => _vm.LoadOrdersAsync();
        public Task Search() => _vm.SearchAsync();
        public Task ClearFilter() => _vm.ClearFilterAsync();
        public Task GoToPage(int page) => _vm.GoToPageAsync(page);
        public Task PreviousPage() => _vm.PreviousPageAsync();
        public Task NextPage() => _vm.NextPageAsync();
        public Task CreateOrder() => _vm.CreateOrderAsync();
        public Task EditOrder(Order order) => _vm.EditOrderAsync(order);
        public Task ViewOrder(Order order) => _vm.ViewOrderAsync(order);
        public Task DeleteOrder(Order order) => _vm.DeleteOrderAsync(order);
        public Task ChangeStatus(Order order) => _vm.ChangeStatusAsync(order);
        public Task Refresh() => _vm.LoadOrdersAsync();
        public Task Export() => _vm.ExportAsync();
        public Task Print() => _vm.PrintAsync();

        public void UpdateSearchText(string? text) => _vm.SearchText = text;
        public void UpdateStatusFilter(OrderStatus? status) => _vm.StatusFilter = status;
        public void UpdateDateRange(DateTime? from, DateTime? to)
        {
            _vm.FromDate = from;
            _vm.ToDate = to;
        }
        public void SelectOrder(Order? order) => _vm.SelectedOrder = order;
    }

    #endregion

    #region Output

    /// <summary>
    /// Output state - read-only reactive state exposed to View.
    /// </summary>
    public sealed class Output : IViewModelOutput
    {
        private readonly OrderListViewModel _vm;

        internal Output(OrderListViewModel vm) => _vm = vm;

        // Collection State
        public ReadOnlyObservableCollection<Order> Orders =>
            _vm._ordersReadOnly ??= new ReadOnlyObservableCollection<Order>(_vm._orders);
        public Order? SelectedOrder => _vm.SelectedOrder;

        // Pagination State
        public int CurrentPage => _vm.CurrentPage;
        public int TotalPages => _vm.TotalPages;
        public int TotalCount => _vm.TotalCount;
        public int PageSize => _vm.PageSize;

        // Filter State
        public string? SearchText => _vm.SearchText;
        public OrderStatus? StatusFilter => _vm.StatusFilter;
        public DateTime? FromDate => _vm.FromDate;
        public DateTime? ToDate => _vm.ToDate;

        // UI State
        public bool IsLoading => _vm.IsLoading;
        public bool IsBusy => _vm.IsBusy;
        public string? ErrorMessage => _vm.ErrorMessage;
        public bool HasError => _vm.HasError;

        // Computed State
        public bool CanGoPrevious => CurrentPage > 1;
        public bool CanGoNext => CurrentPage < TotalPages;
        public bool HasSelection => SelectedOrder != null;
        public bool IsEmpty => Orders.Count == 0 && !IsLoading;
        public string StatusMessage => IsLoading
            ? "Loading..."
            : $"Showing {Orders.Count} of {TotalCount} orders (Page {CurrentPage} of {TotalPages})";
    }

    #endregion

    #region Effect

    /// <summary>
    /// Effect - one-time events for side effects (dialogs, notifications).
    /// Navigation is handled by NavGraph, not Effect.
    /// </summary>
    public sealed class Effect : IViewModelEffect, IDisposable
    {
        private bool _disposed;

        // Dialogs
        public EffectSubject<ConfirmDeleteRequest> ConfirmDelete { get; } = new();
        public EffectSubject<AppError> ShowError { get; } = new();
        public EffectSubject<string> ShowInfo { get; } = new();
        public EffectSubject<string> ShowWarning { get; } = new();
        public EffectSubject<string> ShowSuccess { get; } = new();

        // Events
        public EffectSubject<Order> OrderDeleted { get; } = new();
        public EffectSubject<StatusChangedEvent> OrderStatusChanged { get; } = new();
        public EffectSubject DataRefreshed { get; } = new();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            ConfirmDelete.Dispose();
            ShowError.Dispose();
            ShowInfo.Dispose();
            ShowWarning.Dispose();
            ShowSuccess.Dispose();
            OrderDeleted.Dispose();
            OrderStatusChanged.Dispose();
            DataRefreshed.Dispose();
        }
    }

    #endregion

    #region Records

    public record ConfirmDeleteRequest(Order Order, Action OnConfirm);
    public record StatusChangedEvent(Order Order, OrderStatus OldStatus, OrderStatus NewStatus);

    #endregion
}
