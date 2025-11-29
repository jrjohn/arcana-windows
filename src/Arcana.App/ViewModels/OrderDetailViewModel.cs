using System.Collections.ObjectModel;
using Arcana.Core.Common;
using Arcana.Domain.Entities;
using Arcana.Domain.Services;
using Arcana.Plugins.Contracts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcana.App.ViewModels;

/// <summary>
/// ViewModel for order detail page.
/// </summary>
public partial class OrderDetailViewModel : ViewModelBase
{
    private readonly IOrderService _orderService;
    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;
    private readonly INavigationService _navigationService;
    private readonly IWindowService _windowService;

    private int? _orderId;
    private bool _isNew;

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

    public bool IsNew => _isNew;
    public string Title => _isNew ? "New Order" : $"Order {Order.OrderNumber}";

    public OrderDetailViewModel(
        IOrderService orderService,
        ICustomerService customerService,
        IProductService productService,
        INavigationService navigationService,
        IWindowService windowService)
    {
        _orderService = orderService;
        _customerService = customerService;
        _productService = productService;
        _navigationService = navigationService;
        _windowService = windowService;
    }

    public async Task LoadAsync(int? orderId)
    {
        _orderId = orderId;
        _isNew = !orderId.HasValue;

        await ExecuteWithLoadingAsync(async () =>
        {
            // Load customers for lookup
            var customerResult = await _customerService.SearchCustomersAsync("", 100);
            if (customerResult.IsSuccess)
            {
                Customers = new ObservableCollection<Customer>(customerResult.Value!);
            }

            if (_isNew)
            {
                // Create new order
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
                // Load existing order
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
                    SetError(result.Error!.Message);
                }
            }
        });
    }

    [RelayCommand]
    private void Edit()
    {
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            // Validate
            if (SelectedCustomer == null)
            {
                await _windowService.ShowWarningAsync("Please select a customer");
                return;
            }

            if (Items.Count == 0)
            {
                await _windowService.ShowWarningAsync("Please add at least one order item");
                return;
            }

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
                OnPropertyChanged(nameof(Title));

                await _windowService.ShowInfoAsync("Order saved successfully");
            }
            else
            {
                await _windowService.ShowErrorAsync(result.Error!.ToUserMessage());
            }
        });
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (IsDirty)
        {
            var confirmed = await _windowService.ShowConfirmAsync(
                "Discard Changes",
                "You have unsaved changes. Are you sure you want to discard them?");

            if (!confirmed) return;
        }

        if (_isNew)
        {
            await _navigationService.CloseAsync();
        }
        else
        {
            // Reload the order
            await LoadAsync(_orderId);
            IsEditing = false;
        }
    }

    [RelayCommand]
    private async Task SearchProductsAsync()
    {
        if (string.IsNullOrWhiteSpace(ProductSearchText)) return;

        var result = await _productService.SearchProductsAsync(ProductSearchText);
        if (result.IsSuccess)
        {
            Products = new ObservableCollection<Product>(result.Value!);
        }
    }

    [RelayCommand]
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

    [RelayCommand]
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

    [RelayCommand]
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

    [RelayCommand]
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

    [RelayCommand]
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
}
