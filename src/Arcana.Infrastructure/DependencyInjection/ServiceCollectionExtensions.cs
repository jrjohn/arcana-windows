using Arcana.Core.Common;
using Arcana.Core.Security;
using Arcana.Data.Local;
using Arcana.Data.Repository;
using Arcana.Domain.Services;
using Arcana.Domain.Validation;
using Arcana.Infrastructure.Platform;
using Arcana.Infrastructure.Security;
using Arcana.Infrastructure.Services;
using Arcana.Infrastructure.Localization;
using Arcana.Infrastructure.Settings;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;
using Arcana.Plugins.Services;
using Arcana.Sync;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Arcana.Infrastructure.DependencyInjection;

/// <summary>
/// Service collection extensions for dependency injection.
/// 服務集合擴展用於依賴注入
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all application services.
    /// </summary>
    public static IServiceCollection AddArcanaServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.AddSingleton(configuration);

        // Logging
        services.AddLogging(builder =>
        {
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Arcana", "logs", "app-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger();

            builder.AddSerilog(logger);
        });

        // Core services
        services.AddCoreServices();

        // Security services
        services.AddSecurityServices(configuration);

        // Data services
        services.AddDataServices(configuration);

        // Domain services
        services.AddDomainServices();

        // Plugin services
        services.AddPluginServices();

        // Sync services
        services.AddSyncServices();

        return services;
    }

    /// <summary>
    /// Adds core services.
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<INetworkMonitor, NetworkMonitor>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();

        return services;
    }

    /// <summary>
    /// Adds security and authentication services.
    /// 添加安全和身份驗證服務
    /// </summary>
    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Password hasher
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // Token service with configuration
        services.AddSingleton<ITokenService>(sp =>
        {
            var options = new TokenServiceOptions();
            configuration.GetSection("Security:Token").Bind(options);
            return new TokenService(options);
        });

        // Current user service (singleton for desktop app)
        services.AddSingleton<ICurrentUserService, CurrentUserService>();

        // Auth service (scoped to use DbContext)
        services.AddScoped<IAuthService, AuthService>();

        // Authorization service
        services.AddScoped<IAuthorizationService, AuthorizationService>();

        return services;
    }

    /// <summary>
    /// Adds data layer services.
    /// </summary>
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database context
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Arcana",
            "data",
            "arcana.db");

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
        });

        // Repositories
        services.AddScoped(typeof(Arcana.Data.Repository.IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(Arcana.Data.Repository.IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        // Unit of Work
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
        });
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();

        return services;
    }

    /// <summary>
    /// Adds domain layer services.
    /// </summary>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Services
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductService, ProductService>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<OrderValidator>();

        return services;
    }

    /// <summary>
    /// Adds plugin system services.
    /// </summary>
    public static IServiceCollection AddPluginServices(this IServiceCollection services)
    {
        // Plugin infrastructure
        services.AddSingleton<IMessageBus, MessageBus>();
        services.AddSingleton<IEventAggregator, EventAggregator>();
        services.AddSingleton<ISharedStateStore, SharedStateStore>();
        services.AddSingleton<ICommandService, CommandService>();
        services.AddSingleton<IMenuRegistry, MenuRegistry>();
        services.AddSingleton<IViewRegistry, ViewRegistry>();

        // Plugin health monitor
        services.AddSingleton<PluginHealthMonitor>();

        // Plugin manager (core)
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<PluginManager>>();
            var pluginsPath = Path.Combine(AppContext.BaseDirectory, "plugins");
            return new PluginManager(sp, logger, pluginsPath);
        });

        // Plugin manager service (full-featured, implements IPluginManager)
        services.AddSingleton<IPluginManager>(sp =>
        {
            var pluginManager = sp.GetRequiredService<PluginManager>();
            var healthMonitor = sp.GetRequiredService<PluginHealthMonitor>();
            var logger = sp.GetRequiredService<ILogger<PluginManagerService>>();
            var pluginsPath = Path.Combine(AppContext.BaseDirectory, "plugins");
            return new PluginManagerService(pluginManager, healthMonitor, logger, pluginsPath);
        });

        return services;
    }

    /// <summary>
    /// Adds sync services.
    /// </summary>
    public static IServiceCollection AddSyncServices(this IServiceCollection services)
    {
        services.AddSingleton<ISyncService, SyncService>();

        return services;
    }
}
