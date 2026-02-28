using System.Collections.ObjectModel;
using Arcana.App.Navigation;
using Arcana.Core.Common;
using Arcana.Domain.Entities;
using Arcana.Domain.Services;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Contracts.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.App.ViewModels;

/// <summary>
/// ViewModel for order detail page using UDF pattern.
/// </summary>
public partial class OrderDetailViewModel : ReactiveViewModelBase
{
    // ============ Dependencies ============
    private readonly OrderService _orderService;
    private readonly CustomerService _customerService;
    private readonly ProductService _productService;
    private readonly NavGraph _nav;
    private readonly WindowService _windowService;

    // ============ Private State ============

    [ObservableProperty]
    private Order _order = new();

    [ObservableProperty]
    private ObservableCollection<OrderItem> _items = new();

    [ObservableProperty]
    private OrderItem? _selectedItem;

    [ObservableProperty]
    private ObservableCollection<Customer> _customers = new();

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private string? _productSearchText;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    private int? _orderId;
    private bool _isNew;

    // ============ Input/Output/Effect ============
    private Input? _input;
    private Output? _output;
    private Effect? _effect;

    public Input In => _input ??= new Input(this);
    public Output Out => _output ??= new Output(this);
    public Effect Fx => _effect ??= new Effect();

    // ============ Constructor ============
    public OrderDetailViewModel(
        OrderService orderService,
        CustomerService customerService,
        ProductService productService,
        NavGraph nav,
        WindowService windowService)
    {
        _orderService = orderService;
        _customerService = customerService;
        _productService = productService;
        _nav = nav;
        _windowService = windowService;
    }

    // ============ Internal Actions ============

    private async Task LoadAsync(int? orderId)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsLoading = true;
            ErrorMessage = null;

            _orderId = orderId;
            _isNew = !orderId.HasValue;

            // Load customers for lookup
            var customerResult = await _customerService.SearchCustomersAsync("", 100);
            if (customerResult.IsSuccess)
            {
                Customers = new ObservableCollection<Customer>(customerResult.Value!);
            }

            if (_isNew)
            {
                Order = new Order
                {
                    OrderNumber = await _orderService.GenerateOrderNumberAsync(),
                    OrderDate = DateTime.Today,
                    Status = OrderStatus.Draft,
                    TaxRate = 5m
                };
                Items = new ObservableCollection<OrderItem>();
                IsEditing = true;
            }
            else
            {
                var result = await _orderService.GetOrderByIdAsync(orderId!.Value);
                if (result.IsSuccess)
                {
                    Order = result.Value!;
                    Items = new ObservableCollection<OrderItem>(Order.Items);
                    SelectedCustomer = Customers.FirstOrDefault(c => c.Id == Order.CustomerId);
                    IsEditing = false;
                }
                else
                {
                    ErrorMessage = result.Error!.Message;
                    Fx.ShowError.Emit(result.Error);
                }
            }

            Fx.DataLoaded.Emit();
        }
        finally
        {
            IsBusy = false;
            IsLoading = false;
        }
    }

    private void Edit()
    {
        IsEditing = true;
    }

    private async Task SaveAsync()
    {
        if (IsBusy) return;

        // Validate
        if (SelectedCustomer == null)
        {
            Fx.ShowWarning.Emit("Please select a customer");
            return;
        }

        if (Items.Count == 0)
        {
            Fx.ShowWarning.Emit("Please add at least one order item");
            return;
        }

        try
        {
            IsBusy = true;
            IsLoading = true;

            // Update order from form
            Order.CustomerId = SelectedCustomer.Id;
            Order.CustomerName = SelectedCustomer.Name;
            Order.Items = Items.ToList();
            Order.CalculateTotals();

            Result<Order> result;
            if (_isNew)
            {
                result = await _orderService.CreateOrderAsync(Order);
            }
            else
            {
                result = await _orderService.UpdateOrderAsync(Order);
            }

            if (result.IsSuccess)
            {
                Order = result.Value!;
                _orderId = Order.Id;
                _isNew = false;
                IsEditing = false;
                IsDirty = false;

                Fx.ShowSuccess.Emit("Order saved successfully");
                Fx.OrderSaved.Emit(Order);
            }
            else
            {
                Fx.ShowError.Emit(result.Error!);
            }
        }
        finally
        {
            IsBusy = false;
            IsLoading = false;
        }
    }

    private async Task CancelAsync()
    {
        if (IsDirty)
        {
            Fx.ConfirmDiscard.Emit(new ConfirmDiscardRequest(async () =>
            {
                await PerformCancelAsync();
            }));
        }
        else
        {
            await PerformCancelAsync();
        }
    }

    private async Task PerformCancelAsync()
    {
        if (_isNew)
        {
            await _nav.Close();
        }
        else
        {
            await LoadAsync(_orderId);
            IsEditing = false;
        }
    }

    private async Task SearchProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(ProductSearchText)) return;

        var result = await _productService.SearchProductsAsync(ProductSearchText);
        if (result.IsSuccess)
        {
            Products = new ObservableCollection<Product>(result.Value!);
        }
    }

    private void AddItem(Product? product)
    {
        if (product == null) return;

        var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existingItem != null)
        {
            existingItem.Quantity++;
            RefreshItems();
        }
        else
        {
            var lineNumber = Items.Count + 1;
            var item = new OrderItem
            {
                LineNumber = lineNumber,
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                Unit = product.Unit,
                UnitPrice = product.Price,
                Quantity = 1,
                DiscountPercent = 0
            };
            Items.Add(item);
        }

        IsDirty = true;
        CalculateTotals();
    }

    private void RemoveItem(OrderItem? item)
    {
        if (item != null)
        {
            Items.Remove(item);
            RenumberItems();
            IsDirty = true;
            CalculateTotals();
        }
    }

    private void IncreaseQuantity(OrderItem? item)
    {
        if (item != null)
        {
            item.Quantity++;
            IsDirty = true;
            RefreshItems();
            CalculateTotals();
        }
    }

    private void DecreaseQuantity(OrderItem? item)
    {
        if (item != null && item.Quantity > 1)
        {
            item.Quantity--;
            IsDirty = true;
            RefreshItems();
            CalculateTotals();
        }
    }

    private void ClearItems()
    {
        Items.Clear();
        IsDirty = true;
        CalculateTotals();
    }

    private void RenumberItems()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].LineNumber = i + 1;
        }
    }

    private void RefreshItems()
    {
        var temp = Items;
        Items = new ObservableCollection<OrderItem>(temp);
    }

    private void CalculateTotals()
    {
        Order.Items = Items.ToList();
        Order.CalculateTotals();
        OnPropertyChanged(nameof(Order));
    }

    // ============ State Change Handlers ============

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value != null && IsEditing)
        {
            Order.CustomerId = value.Id;
            Order.CustomerName = value.Name;
            Order.ShippingAddress = value.Address;
            Order.ShippingCity = value.City;
            Order.ShippingPostalCode = value.PostalCode;
            IsDirty = true;
        }
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

    public sealed class Input : IViewModelInput
    {
        private readonly OrderDetailViewModel _vm;

        internal Input(OrderDetailViewModel vm) => _vm = vm;

        public Task Load(int? orderId) => _vm.LoadAsync(orderId);
        public void Edit() => _vm.Edit();
        public Task Save() => _vm.SaveAsync();
        public Task Cancel() => _vm.CancelAsync();

        // Product search
        public void UpdateProductSearchText(string? text) => _vm.ProductSearchText = text;
        public Task SearchProducts() => _vm.SearchProductsAsync();

        // Item operations
        public void AddItem(Product? product) => _vm.AddItem(product);
        public void RemoveItem(OrderItem? item) => _vm.RemoveItem(item);
        public void IncreaseQuantity(OrderItem? item) => _vm.IncreaseQuantity(item);
        public void DecreaseQuantity(OrderItem? item) => _vm.DecreaseQuantity(item);
        public void ClearItems() => _vm.ClearItems();

        // Selection
        public void SelectItem(OrderItem? item) => _vm.SelectedItem = item;
        public void SelectCustomer(Customer? customer) => _vm.SelectedCustomer = customer;
    }

    #endregion

    #region Output

    public sealed class Output : IViewModelOutput
    {
        private readonly OrderDetailViewModel _vm;

        internal Output(OrderDetailViewModel vm) => _vm = vm;

        // Order state
        public Order Order => _vm.Order;
        public ObservableCollection<OrderItem> Items => _vm.Items;
        public OrderItem? SelectedItem => _vm.SelectedItem;

        // Lookup data
        public ObservableCollection<Customer> Customers => _vm.Customers;
        public Customer? SelectedCustomer => _vm.SelectedCustomer;
        public ObservableCollection<Product> Products => _vm.Products;
        public string? ProductSearchText => _vm.ProductSearchText;

        // UI state
        public bool IsDirty => _vm.IsDirty;
        public bool IsEditing => _vm.IsEditing;
        public bool IsLoading => _vm.IsLoading;
        public bool IsBusy => _vm.IsBusy;
        public string? ErrorMessage => _vm.ErrorMessage;

        // Computed state
        public bool IsNew => _vm._isNew;
        public string Title => _vm._isNew ? "New Order" : $"Order {_vm.Order.OrderNumber}";
        public bool CanSave => _vm.IsEditing && !_vm.IsBusy;
        public bool CanEdit => !_vm.IsEditing && !_vm._isNew;
        public bool HasItems => _vm.Items.Count > 0;
    }

    #endregion

    #region Effect

    public sealed class Effect : IViewModelEffect, IDisposable
    {
        private bool _disposed;

        // Notifications
        public EffectSubject<AppError> ShowError { get; } = new();
        public EffectSubject<string> ShowWarning { get; } = new();
        public EffectSubject<string> ShowSuccess { get; } = new();
        public EffectSubject<string> ShowInfo { get; } = new();

        // Confirmations
        public EffectSubject<ConfirmDiscardRequest> ConfirmDiscard { get; } = new();

        // Events
        public EffectSubject DataLoaded { get; } = new();
        public EffectSubject<Order> OrderSaved { get; } = new();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            ShowError.Dispose();
            ShowWarning.Dispose();
            ShowSuccess.Dispose();
            ShowInfo.Dispose();
            ConfirmDiscard.Dispose();
            DataLoaded.Dispose();
            OrderSaved.Dispose();
        }
    }

    #endregion

    #region Records

    public record ConfirmDiscardRequest(Action OnConfirm);

    #endregion
}
