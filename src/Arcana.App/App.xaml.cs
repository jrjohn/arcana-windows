using Arcana.App.Navigation;
using Arcana.App.Plugins;
using Arcana.App.Services;
using Arcana.App.ViewModels;
using Arcana.Data.Local;
using Arcana.Domain.Entities;
using Arcana.Infrastructure.DependencyInjection;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace Arcana.App;

/// <summary>
/// Application entry point.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    private IHost? _host;

    public static IServiceProvider Services { get; private set; } = null!;
    public static IConfiguration Configuration { get; private set; } = null!;
    public static Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Build configuration
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        // Build host
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddArcanaServices(Configuration);
                services.AddSingleton<MainWindow>();

                // Register Navigation Service
                services.AddSingleton<INavigationService, DynamicNavigationService>();

                // Register Window Service
                services.AddSingleton<IWindowService, WindowService>();

                // Register App Services
                services.AddSingleton<ThemeService>();
                services.AddSingleton<AppSettingsService>();
                services.AddSingleton<IDocumentManager, DocumentManager>();

                // Register ViewModels
                services.AddTransient<PluginManagerViewModel>();
                services.AddTransient<OrderListViewModel>();
                services.AddTransient<OrderDetailViewModel>();
            })
            .Build();

        Services = _host.Services;

        // Initialize database
        await InitializeDatabaseAsync();

        // Initialize plugins
        await InitializePluginsAsync();

        // Show main window
        _window = Services.GetRequiredService<MainWindow>();
        MainWindow = _window;

        // Apply saved language before UI is shown
        ApplyLanguageSettings();

        // Apply saved theme after content is loaded
        if (_window.Content is FrameworkElement rootElement)
        {
            rootElement.Loaded += OnRootElementLoaded;
        }

        _window.Activate();
    }

    private void ApplyLanguageSettings()
    {
        var settingsService = Services.GetRequiredService<AppSettingsService>();
        var localizationService = Services.GetRequiredService<ILocalizationService>();

        var savedLanguage = settingsService.LanguageCode;
        if (!string.IsNullOrEmpty(savedLanguage))
        {
            localizationService.SetCulture(savedLanguage);
        }
    }

    private void OnRootElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement rootElement)
        {
            rootElement.Loaded -= OnRootElementLoaded;

            var settingsService = Services.GetRequiredService<AppSettingsService>();
            var themeService = Services.GetRequiredService<ThemeService>();
            var savedThemeId = settingsService.ThemeId;

            themeService.ApplyTheme(savedThemeId, rootElement);
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Delete and recreate database if schema is outdated
        // This ensures all tables exist. Remove this in production and use migrations instead.
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Seed sample data
        await SeedSampleDataAsync(context);
    }

    private static async Task SeedSampleDataAsync(AppDbContext context)
    {
        // Check if data already exists
        if (await context.Customers.AnyAsync())
            return;

        // Create sample customers
        var customers = new List<Customer>
        {
            new() { Code = "C001", Name = "Taipei Tech Co., Ltd.", ContactName = "David Wang", Phone = "02-2345-6789", Email = "wang@taipei-tech.com", Address = "100 Xinyi Road, Xinyi District", City = "Taipei", TaxId = "12345678", CreditLimit = 100000, IsActive = true },
            new() { Code = "C002", Name = "Hsinchu Electronics Inc.", ContactName = "Sarah Lee", Phone = "03-5678-1234", Email = "lee@hsinchu-elec.com", Address = "200 Guangfu Road, East District", City = "Hsinchu", TaxId = "23456789", CreditLimit = 200000, IsActive = true },
            new() { Code = "C003", Name = "Taichung Precision Industries", ContactName = "Michelle Zhang", Phone = "04-2345-6789", Email = "zhang@taichung-precision.com", Address = "300 Industrial Road, Xitun District", City = "Taichung", TaxId = "34567890", CreditLimit = 150000, IsActive = true },
            new() { Code = "C004", Name = "Kaohsiung Trading Co.", ContactName = "Michael Chen", Phone = "07-7654-3210", Email = "chen@kaohsiung-trade.com", Address = "400 Zhongshan Road, Qianzhen District", City = "Kaohsiung", TaxId = "45678901", CreditLimit = 80000, IsActive = true },
            new() { Code = "C005", Name = "Taoyuan Logistics Center", ContactName = "Jennifer Lin", Phone = "03-3456-7890", Email = "lin@taoyuan-logistics.com", Address = "500 Zhongzheng Road, Zhongli District", City = "Taoyuan", TaxId = "56789012", CreditLimit = 120000, IsActive = true },
        };
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        // Create sample product categories
        var categories = new List<ProductCategory>
        {
            new() { Code = "ELEC", Name = "Electronics", SortOrder = 1 },
            new() { Code = "COMP", Name = "Computer Peripherals", SortOrder = 2 },
            new() { Code = "OFFICE", Name = "Office Supplies", SortOrder = 3 },
        };
        context.ProductCategories.AddRange(categories);
        await context.SaveChangesAsync();

        // Create sample products
        var products = new List<Product>
        {
            new() { Code = "P001", Name = "Laptop 15-inch", CategoryId = categories[0].Id, Unit = "pc", Price = 35000, Cost = 28000, StockQuantity = 50, MinStockLevel = 10, IsActive = true },
            new() { Code = "P002", Name = "Wireless Mouse", CategoryId = categories[1].Id, Unit = "pc", Price = 800, Cost = 400, StockQuantity = 200, MinStockLevel = 50, IsActive = true },
            new() { Code = "P003", Name = "Mechanical Keyboard", CategoryId = categories[1].Id, Unit = "pc", Price = 2500, Cost = 1500, StockQuantity = 100, MinStockLevel = 20, IsActive = true },
            new() { Code = "P004", Name = "27-inch Monitor", CategoryId = categories[0].Id, Unit = "pc", Price = 12000, Cost = 8000, StockQuantity = 30, MinStockLevel = 5, IsActive = true },
            new() { Code = "P005", Name = "USB Flash Drive 64GB", CategoryId = categories[1].Id, Unit = "pc", Price = 350, Cost = 150, StockQuantity = 500, MinStockLevel = 100, IsActive = true },
            new() { Code = "P006", Name = "Printer", CategoryId = categories[2].Id, Unit = "pc", Price = 8000, Cost = 5000, StockQuantity = 25, MinStockLevel = 5, IsActive = true },
            new() { Code = "P007", Name = "A4 Copy Paper (500 sheets)", CategoryId = categories[2].Id, Unit = "pack", Price = 120, Cost = 80, StockQuantity = 1000, MinStockLevel = 200, IsActive = true },
            new() { Code = "P008", Name = "Webcam", CategoryId = categories[1].Id, Unit = "pc", Price = 1500, Cost = 800, StockQuantity = 80, MinStockLevel = 15, IsActive = true },
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Create 10 sample orders with order items
        var random = new Random(42);
        var statuses = new[] { OrderStatus.Draft, OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Completed };
        var paymentStatuses = new[] { PaymentStatus.Unpaid, PaymentStatus.PartialPaid, PaymentStatus.Paid };

        for (int i = 1; i <= 10; i++)
        {
            var customer = customers[random.Next(customers.Count)];
            var orderDate = DateTime.Today.AddDays(-random.Next(1, 30));
            var status = statuses[random.Next(statuses.Length)];
            var paymentStatus = paymentStatuses[random.Next(paymentStatuses.Length)];

            var order = new Order
            {
                OrderNumber = $"ORD-{DateTime.Now:yyyyMM}-{i:D4}",
                OrderDate = orderDate,
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                Status = status,
                PaymentStatus = paymentStatus,
                PaymentMethod = (PaymentMethod)random.Next(3),
                TaxRate = 5m,
                ShippingAddress = customer.Address,
                ShippingCity = customer.City,
                ExpectedDeliveryDate = orderDate.AddDays(random.Next(3, 14)),
                Notes = i % 3 == 0 ? "Urgent, please prioritize" : null
            };

            // Add 1-5 random order items
            var itemCount = random.Next(1, 6);
            var usedProducts = new HashSet<int>();

            for (int j = 1; j <= itemCount; j++)
            {
                Product product;
                do
                {
                    product = products[random.Next(products.Count)];
                } while (usedProducts.Contains(product.Id));

                usedProducts.Add(product.Id);

                var quantity = random.Next(1, 10);
                var discountPercent = random.Next(0, 3) * 5; // 0%, 5%, or 10%

                order.Items.Add(new OrderItem
                {
                    LineNumber = j,
                    ProductId = product.Id,
                    ProductCode = product.Code,
                    ProductName = product.Name,
                    Unit = product.Unit,
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    DiscountPercent = discountPercent
                });
            }

            // Calculate totals
            order.CalculateTotals();

            // Set paid amount based on payment status
            order.PaidAmount = paymentStatus switch
            {
                PaymentStatus.Paid => order.TotalAmount,
                PaymentStatus.PartialPaid => Math.Round(order.TotalAmount * 0.5m, 0),
                _ => 0
            };

            context.Orders.Add(order);
        }

        await context.SaveChangesAsync();
    }

    private async Task InitializePluginsAsync()
    {
        var pluginManager = Services.GetRequiredService<PluginManager>();

        // Register all built-in plugins
        pluginManager.RegisterBuiltInPlugin(new CoreMenuPlugin());
        pluginManager.RegisterBuiltInPlugin(new SystemPlugin());
        pluginManager.RegisterBuiltInPlugin(new OrderModulePlugin());
        pluginManager.RegisterBuiltInPlugin(new CustomerModulePlugin());
        pluginManager.RegisterBuiltInPlugin(new ProductModulePlugin());

        // Discover external plugins from plugins directory
        await pluginManager.DiscoverPluginsAsync();

        // Activate all plugins
        await pluginManager.ActivateAllAsync();
    }
}
