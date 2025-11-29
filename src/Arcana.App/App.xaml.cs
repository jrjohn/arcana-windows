using Arcana.App.ViewModels;
using Arcana.Data.Local;
using Arcana.Infrastructure.DependencyInjection;
using Arcana.Plugins.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace Arcana.App;

/// <summary>
/// Application entry point.
/// 應用程式進入點
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

                // Register ViewModels
                services.AddTransient<PluginManagerViewModel>();
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
        _window.Activate();
    }

    private async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    private async Task InitializePluginsAsync()
    {
        var pluginManager = Services.GetRequiredService<PluginManager>();

        // Register built-in plugins
        // pluginManager.RegisterBuiltInPlugin(new CoreMenuPlugin());
        // pluginManager.RegisterBuiltInPlugin(new OrderModulePlugin());

        await pluginManager.DiscoverPluginsAsync();
        await pluginManager.ActivateAllAsync();
    }
}
