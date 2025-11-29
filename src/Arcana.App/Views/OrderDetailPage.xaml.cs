using Arcana.App.ViewModels;
using Arcana.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Arcana.App.Views;

/// <summary>
/// Order detail page.
/// 訂單明細頁面
/// </summary>
public sealed partial class OrderDetailPage : Page
{
    private OrderDetailViewModel ViewModel { get; }

    public OrderDetailPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<OrderDetailViewModel>();
        DataContext = ViewModel;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        int? orderId = e.Parameter as int?;
        await ViewModel.LoadAsync(orderId);
        UpdateUI();
    }

    private void UpdateUI()
    {
        PageTitle.Text = ViewModel.Title;
        OrderStatus.Text = ViewModel.Order.Status.ToString();
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
        await ViewModel.CancelCommand.ExecuteAsync(null);
        UpdateUI();
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
}
