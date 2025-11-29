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
            new() { Code = "C001", Name = "台北科技有限公司", ContactName = "王大明", Phone = "02-2345-6789", Email = "wang@taipei-tech.com", Address = "台北市信義區信義路100號", City = "台北市", TaxId = "12345678", CreditLimit = 100000, IsActive = true },
            new() { Code = "C002", Name = "新竹電子股份有限公司", ContactName = "李小華", Phone = "03-5678-1234", Email = "lee@hsinchu-elec.com", Address = "新竹市東區光復路200號", City = "新竹市", TaxId = "23456789", CreditLimit = 200000, IsActive = true },
            new() { Code = "C003", Name = "台中精密工業", ContactName = "張美玲", Phone = "04-2345-6789", Email = "zhang@taichung-precision.com", Address = "台中市西屯區工業路300號", City = "台中市", TaxId = "34567890", CreditLimit = 150000, IsActive = true },
            new() { Code = "C004", Name = "高雄貿易商行", ContactName = "陳志明", Phone = "07-7654-3210", Email = "chen@kaohsiung-trade.com", Address = "高雄市前鎮區中山路400號", City = "高雄市", TaxId = "45678901", CreditLimit = 80000, IsActive = true },
            new() { Code = "C005", Name = "桃園物流中心", ContactName = "林佳蓉", Phone = "03-3456-7890", Email = "lin@taoyuan-logistics.com", Address = "桃園市中壢區中正路500號", City = "桃園市", TaxId = "56789012", CreditLimit = 120000, IsActive = true },
        };
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        // Create sample product categories
        var categories = new List<ProductCategory>
        {
            new() { Code = "ELEC", Name = "電子產品", SortOrder = 1 },
            new() { Code = "COMP", Name = "電腦周邊", SortOrder = 2 },
            new() { Code = "OFFICE", Name = "辦公用品", SortOrder = 3 },
        };
        context.ProductCategories.AddRange(categories);
        await context.SaveChangesAsync();

        // Create sample products
        var products = new List<Product>
        {
            new() { Code = "P001", Name = "筆記型電腦 15吋", CategoryId = categories[0].Id, Unit = "台", Price = 35000, Cost = 28000, StockQuantity = 50, MinStockLevel = 10, IsActive = true },
            new() { Code = "P002", Name = "無線滑鼠", CategoryId = categories[1].Id, Unit = "個", Price = 800, Cost = 400, StockQuantity = 200, MinStockLevel = 50, IsActive = true },
            new() { Code = "P003", Name = "機械鍵盤", CategoryId = categories[1].Id, Unit = "個", Price = 2500, Cost = 1500, StockQuantity = 100, MinStockLevel = 20, IsActive = true },
            new() { Code = "P004", Name = "27吋螢幕", CategoryId = categories[0].Id, Unit = "台", Price = 12000, Cost = 8000, StockQuantity = 30, MinStockLevel = 5, IsActive = true },
            new() { Code = "P005", Name = "USB隨身碟 64GB", CategoryId = categories[1].Id, Unit = "個", Price = 350, Cost = 150, StockQuantity = 500, MinStockLevel = 100, IsActive = true },
            new() { Code = "P006", Name = "印表機", CategoryId = categories[2].Id, Unit = "台", Price = 8000, Cost = 5000, StockQuantity = 25, MinStockLevel = 5, IsActive = true },
            new() { Code = "P007", Name = "A4影印紙 (500張)", CategoryId = categories[2].Id, Unit = "包", Price = 120, Cost = 80, StockQuantity = 1000, MinStockLevel = 200, IsActive = true },
            new() { Code = "P008", Name = "網路攝影機", CategoryId = categories[1].Id, Unit = "個", Price = 1500, Cost = 800, StockQuantity = 80, MinStockLevel = 15, IsActive = true },
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
