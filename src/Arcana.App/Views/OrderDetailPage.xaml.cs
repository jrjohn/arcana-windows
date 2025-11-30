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
        if (ViewModel?.Order != null)
        {
            OrderStatusText.Text = _localization.Get($"order.status.{ViewModel.Order.Status.ToString().ToLowerInvariant()}");
        }
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Handle int parameter properly - boxing means we can't use "as int?"
        int? orderId = e.Parameter switch
        {
            int id => id,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => null
        };

        await ViewModel.LoadAsync(orderId);
        UpdateUI();
        ApplyLocalization();
    }

    private void UpdateUI()
    {
        PageTitle.Text = ViewModel.Title;
        OrderStatusText.Text = ViewModel.Order.Status.ToString();
        OrderNumberText.Text = ViewModel.Order.OrderNumber;
        OrderDatePicker.Date = ViewModel.Order.OrderDate;
        CustomerComboBox.ItemsSource = ViewModel.Customers;
        CustomerComboBox.SelectedItem = ViewModel.SelectedCustomer;
        OrderItemsListView.ItemsSource = ViewModel.Items;

        ShippingAddressText.Text = ViewModel.Order.ShippingAddress ?? string.Empty;
        ShippingCityText.Text = ViewModel.Order.ShippingCity ?? string.Empty;
        ShippingPostalCodeText.Text = ViewModel.Order.ShippingPostalCode ?? string.Empty;
        NotesText.Text = ViewModel.Order.Notes ?? string.Empty;

        UpdateTotals();

        EditButton.Visibility = ViewModel.IsNew ? Visibility.Collapsed : Visibility.Visible;
        SaveButton.IsEnabled = ViewModel.IsEditing;
        BackButton.Visibility = Frame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateTotals()
    {
        SubtotalText.Text = $"${ViewModel.Order.Subtotal:N0}";
        TaxAmountText.Text = $"${ViewModel.Order.TaxAmount:N0}";
        TotalAmountText.Text = $"${ViewModel.Order.TotalAmount:N0}";
        ShippingCostBox.Value = (double)ViewModel.Order.ShippingCost;
        DiscountAmountBox.Value = (double)ViewModel.Order.DiscountAmount;
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.EditCommand.Execute(null);
        SaveButton.IsEnabled = true;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        // Update order from form
        if (OrderDatePicker.Date.HasValue)
        {
            ViewModel.Order.OrderDate = OrderDatePicker.Date.Value.DateTime;
        }
        ViewModel.Order.ShippingAddress = ShippingAddressText.Text;
        ViewModel.Order.ShippingCity = ShippingCityText.Text;
        ViewModel.Order.ShippingPostalCode = ShippingPostalCodeText.Text;
        ViewModel.Order.Notes = NotesText.Text;
        ViewModel.Order.ShippingCost = (decimal)ShippingCostBox.Value;
        ViewModel.Order.DiscountAmount = (decimal)DiscountAmountBox.Value;
        ViewModel.SelectedCustomer = CustomerComboBox.SelectedItem as Customer;

        await ViewModel.SaveCommand.ExecuteAsync(null);
        UpdateUI();
    }

    private async void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // If dirty, ask for confirmation
        if (ViewModel.IsDirty)
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
        ViewModel.ProductSearchText = args.QueryText;
        await ViewModel.SearchProductsCommand.ExecuteAsync(null);

        // Show products in a flyout or add the first result
        if (ViewModel.Products.Count > 0)
        {
            ViewModel.AddItemCommand.Execute(ViewModel.Products[0]);
            OrderItemsListView.ItemsSource = null;
            OrderItemsListView.ItemsSource = ViewModel.Items;
            UpdateTotals();
        }
    }

    private void ClearItems_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearItemsCommand.Execute(null);
        OrderItemsListView.ItemsSource = null;
        OrderItemsListView.ItemsSource = ViewModel.Items;
        UpdateTotals();
    }

    private void IncreaseQty_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is OrderItem item)
        {
            ViewModel.IncreaseQuantityCommand.Execute(item);
            RefreshItems();
        }
    }

    private void DecreaseQty_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is OrderItem item)
        {
            ViewModel.DecreaseQuantityCommand.Execute(item);
            RefreshItems();
        }
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is OrderItem item)
        {
            ViewModel.RemoveItemCommand.Execute(item);
            RefreshItems();
        }
    }

    private void RefreshItems()
    {
        OrderItemsListView.ItemsSource = null;
        OrderItemsListView.ItemsSource = ViewModel.Items;
        UpdateTotals();
    }

    private void Calculate_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Order.ShippingCost = (decimal)ShippingCostBox.Value;
        ViewModel.Order.DiscountAmount = (decimal)DiscountAmountBox.Value;
        ViewModel.Order.Items = ViewModel.Items.ToList();
        ViewModel.Order.CalculateTotals();
        UpdateTotals();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        // Navigate back within the same tab's Frame
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }
}
