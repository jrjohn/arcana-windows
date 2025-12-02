using Arcana.App.ViewModels;
using Arcana.Domain.Entities;
using Arcana.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Arcana.App.Views;

/// <summary>
/// Order detail page.
/// </summary>
public sealed partial class OrderDetailPage : Page
{
    private OrderDetailViewModel ViewModel { get; }
    private readonly ILocalizationService _localization;

    public OrderDetailPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<OrderDetailViewModel>();
        _localization = App.Services.GetRequiredService<ILocalizationService>();
        _localization.CultureChanged += OnCultureChanged;
        DataContext = ViewModel;
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplyLocalization);
    }

    private void ApplyLocalization()
    {
        // Set language for calendar date pickers
        var currentCulture = _localization.CurrentCulture.Name;
        OrderDatePicker.Language = currentCulture;
        ExpectedDeliveryDatePicker.Language = currentCulture;

        // Command bar buttons
        CopyButton.Label = _localization.Get("order.copy");
        EditButton.Label = _localization.Get("common.edit");
        SaveButton.Label = _localization.Get("common.save");
        CancelButton.Label = _localization.Get("common.cancel");
        PrintButton.Label = _localization.Get("common.print");

        // Order info section
        OrderInfoTitle.Text = _localization.Get("order.info");
        OrderNumberLabel.Text = _localization.Get("order.number");
        OrderDateLabel.Text = _localization.Get("order.date");
        CustomerLabel.Text = _localization.Get("order.customer");
        PaymentMethodLabel.Text = _localization.Get("order.paymentMethod");

        // Payment method options
        PaymentCash.Content = _localization.Get("payment.cash");
        PaymentCreditCard.Content = _localization.Get("payment.creditCard");
        PaymentBankTransfer.Content = _localization.Get("payment.bankTransfer");
        PaymentCheck.Content = _localization.Get("payment.check");
        PaymentCredit.Content = _localization.Get("payment.credit");

        // Order items section
        OrderItemsTitle.Text = _localization.Get("order.items");
        ProductSearchBox.PlaceholderText = _localization.Get("order.searchProduct");
        ClearItemsButton.Content = _localization.Get("order.clearItems");

        // Order items table headers
        ColProductCode.Text = _localization.Get("order.productCode");
        ColProductName.Text = _localization.Get("order.productName");
        ColQuantity.Text = _localization.Get("order.quantity");
        ColUnitPrice.Text = _localization.Get("order.unitPrice");
        ColDiscount.Text = _localization.Get("order.discount");
        ColSubtotal.Text = _localization.Get("order.subtotal");

        // Shipping section
        ShippingInfoTitle.Text = _localization.Get("order.shippingInfo");
        ShippingAddressText.Header = _localization.Get("order.shippingAddress");
        ShippingCityText.Header = _localization.Get("order.shippingCity");
        ShippingPostalCodeText.Header = _localization.Get("order.shippingPostalCode");
        ExpectedDeliveryDatePicker.Header = _localization.Get("order.expectedDelivery");

        // Notes section
        NotesTitle.Text = _localization.Get("order.notes");

        // Order summary section
        OrderSummaryTitle.Text = _localization.Get("order.summary");
        SubtotalLabel.Text = _localization.Get("order.subtotal");
        TaxLabel.Text = _localization.Get("order.tax");
        ShippingLabel.Text = _localization.Get("order.shipping");
        DiscountLabel.Text = _localization.Get("order.discount");
        TotalLabel.Text = _localization.Get("order.total");
        CalculateButton.Content = _localization.Get("order.calculate");

        // Update status text
        if (ViewModel?.Out.Order != null)
        {
            OrderStatusText.Text = _localization.Get($"order.status.{ViewModel.Out.Order.Status.ToString().ToLowerInvariant()}");
        }
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Handle different parameter types
        if (e.Parameter is OrderCopyParameter copyParam)
        {
            // Copy from existing order - create new order with pre-filled data
            await ViewModel.In.Load(null); // Load as new order

            // Pre-fill from copied order
            ViewModel.In.SelectCustomer(copyParam.Customer);
            ViewModel.Out.Order.ShippingAddress = copyParam.ShippingAddress;
            ViewModel.Out.Order.ShippingCity = copyParam.ShippingCity;
            ViewModel.Out.Order.ShippingPostalCode = copyParam.ShippingPostalCode;
            ViewModel.Out.Order.Notes = $"{_localization.Get("order.copiedFrom")} #{copyParam.SourceOrderId}\n{copyParam.Notes}";
            ViewModel.Out.Order.PaymentMethod = copyParam.PaymentMethod;

            // Copy items (create new instances to avoid reference issues)
            foreach (var item in copyParam.Items)
            {
                ViewModel.Out.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountPercent = item.DiscountPercent
                });
            }
            ViewModel.Out.Order.CalculateTotals();
        }
        else
        {
            // Handle int parameter properly - boxing means we can't use "as int?"
            int? orderId = e.Parameter switch
            {
                int id => id,
                string s when int.TryParse(s, out var parsed) => parsed,
                _ => null
            };

            await ViewModel.In.Load(orderId);
        }

        UpdateUI();
        ApplyLocalization();
    }

    private void UpdateUI()
    {
        PageTitle.Text = ViewModel.Out.Title;
        OrderStatusText.Text = ViewModel.Out.Order.Status.ToString();
        OrderNumberText.Text = ViewModel.Out.Order.OrderNumber;
        OrderDatePicker.Date = ViewModel.Out.Order.OrderDate;
        CustomerComboBox.ItemsSource = ViewModel.Out.Customers;
        CustomerComboBox.SelectedItem = ViewModel.Out.SelectedCustomer;
        OrderItemsListView.ItemsSource = ViewModel.Out.Items;

        ShippingAddressText.Text = ViewModel.Out.Order.ShippingAddress ?? string.Empty;
        ShippingCityText.Text = ViewModel.Out.Order.ShippingCity ?? string.Empty;
        ShippingPostalCodeText.Text = ViewModel.Out.Order.ShippingPostalCode ?? string.Empty;
        NotesText.Text = ViewModel.Out.Order.Notes ?? string.Empty;

        UpdateTotals();

        // Show Copy and Edit buttons only for existing orders
        CopyButton.Visibility = ViewModel.Out.IsNew ? Visibility.Collapsed : Visibility.Visible;
        EditButton.Visibility = ViewModel.Out.IsNew ? Visibility.Collapsed : Visibility.Visible;
        SaveButton.IsEnabled = ViewModel.Out.IsEditing;
        BackButton.Visibility = Frame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateTotals()
    {
        SubtotalText.Text = $"${ViewModel.Out.Order.Subtotal:N0}";
        TaxAmountText.Text = $"${ViewModel.Out.Order.TaxAmount:N0}";
        TotalAmountText.Text = $"${ViewModel.Out.Order.TotalAmount:N0}";
        ShippingCostBox.Value = (double)ViewModel.Out.Order.ShippingCost;
        DiscountAmountBox.Value = (double)ViewModel.Out.Order.DiscountAmount;
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.In.Edit();
        SaveButton.IsEnabled = true;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        // Update order from form
        if (OrderDatePicker.Date.HasValue)
        {
            ViewModel.Out.Order.OrderDate = OrderDatePicker.Date.Value.DateTime;
        }
        ViewModel.Out.Order.ShippingAddress = ShippingAddressText.Text;
        ViewModel.Out.Order.ShippingCity = ShippingCityText.Text;
        ViewModel.Out.Order.ShippingPostalCode = ShippingPostalCodeText.Text;
        ViewModel.Out.Order.Notes = NotesText.Text;
        ViewModel.Out.Order.ShippingCost = (decimal)ShippingCostBox.Value;
        ViewModel.Out.Order.DiscountAmount = (decimal)DiscountAmountBox.Value;
        ViewModel.In.SelectCustomer(CustomerComboBox.SelectedItem as Customer);

        await ViewModel.In.Save();
        UpdateUI();
    }

    private async void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // If dirty, ask for confirmation
        if (ViewModel.Out.IsDirty)
        {
            var windowService = App.Services.GetRequiredService<IWindowService>();
            var confirmed = await windowService.ShowConfirmAsync(
                _localization.Get("common.discardChanges"),
                _localization.Get("common.discardChangesMessage"));
            if (!confirmed) return;
        }

        // Go back to previous page
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private async void ProductSearch_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ViewModel.In.UpdateProductSearchText(args.QueryText);
        await ViewModel.In.SearchProducts();

        // Show products in a flyout or add the first result
        if (ViewModel.Out.Products.Count > 0)
        {
            ViewModel.In.AddItem(ViewModel.Out.Products[0]);
            OrderItemsListView.ItemsSource = null;
            OrderItemsListView.ItemsSource = ViewModel.Out.Items;
            UpdateTotals();
        }
    }

    private void ClearItems_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.In.ClearItems();
        OrderItemsListView.ItemsSource = null;
        OrderItemsListView.ItemsSource = ViewModel.Out.Items;
        UpdateTotals();
    }

    private void IncreaseQty_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is OrderItem item)
        {
            ViewModel.In.IncreaseQuantity(item);
            RefreshItems();
        }
    }

    private void DecreaseQty_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is OrderItem item)
        {
            ViewModel.In.DecreaseQuantity(item);
            RefreshItems();
        }
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is OrderItem item)
        {
            ViewModel.In.RemoveItem(item);
            RefreshItems();
        }
    }

    private void RefreshItems()
    {
        OrderItemsListView.ItemsSource = null;
        OrderItemsListView.ItemsSource = ViewModel.Out.Items;
        UpdateTotals();
    }

    private void Calculate_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Out.Order.ShippingCost = (decimal)ShippingCostBox.Value;
        ViewModel.Out.Order.DiscountAmount = (decimal)DiscountAmountBox.Value;
        ViewModel.Out.Order.Items = ViewModel.Out.Items.ToList();
        ViewModel.Out.Order.CalculateTotals();
        UpdateTotals();
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        // Create a copy of the current order
        var copyParam = new OrderCopyParameter
        {
            SourceOrderId = ViewModel.Out.Order.Id,
            Customer = ViewModel.Out.SelectedCustomer,
            Items = ViewModel.Out.Items.ToList(),
            ShippingAddress = ViewModel.Out.Order.ShippingAddress,
            ShippingCity = ViewModel.Out.Order.ShippingCity,
            ShippingPostalCode = ViewModel.Out.Order.ShippingPostalCode,
            Notes = ViewModel.Out.Order.Notes,
            PaymentMethod = ViewModel.Out.Order.PaymentMethod
        };

        // Try to find parent OrderModulePage for nested tab navigation
        var parent = FindParentOrderModulePage();
        if (parent != null)
        {
            parent.OpenNewOrder(copyParam);
        }
        else
        {
            // Fallback: navigate within the same Frame
            Frame.Navigate(typeof(OrderDetailPage), copyParam);
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        // Navigate back within the same tab's Frame
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private OrderModulePage? FindParentOrderModulePage()
    {
        // Navigate up the visual tree to find the OrderModulePage
        DependencyObject? current = this;
        while (current != null)
        {
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
            if (current is OrderModulePage modulePage)
            {
                return modulePage;
            }
        }

        // Alternative: check Frame's parent hierarchy
        if (Frame?.Parent is TabViewItem item)
        {
            var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(item);
            while (parent != null)
            {
                if (parent is OrderModulePage page)
                {
                    return page;
                }
                parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
            }
        }

        return null;
    }
}

/// <summary>
/// Parameter for copying an order.
/// </summary>
public class OrderCopyParameter
{
    public int SourceOrderId { get; set; }
    public Customer? Customer { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    public string? ShippingAddress { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? Notes { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
}
