using Arcana.Core.Common;
using Arcana.Core.Security;
using Arcana.Data.Dao;
using Arcana.Data.Dao.Impl;
using Arcana.Data.Local;
using Arcana.Data.Repository;
using Arcana.Data.Repository.Impl;
using Arcana.Domain.Services;
using Arcana.Domain.Validation;
using Arcana.Infrastructure.Platform;
using Arcana.Infrastructure.Security;
using Arcana.Infrastructure.Services.Impl;
using Arcana.Infrastructure.Localization;
using Arcana.Infrastructure.Settings;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Contracts.Manifest;
using Arcana.Plugins.Core;
using Arcana.Plugins.Services;
using Arcana.Sync;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using CoreCommon = Arcana.Core.Common;
using DataRepository = Arcana.Data.Repository;

namespace Arcana.Infrastructure.DependencyInjection;

/// <summary>
/// Service collection extensions for dependency injection.
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
        services.AddSingleton<NetworkMonitor, NetworkMonitor>();
        services.AddSingleton<SettingsService, SettingsService>();
        services.AddSingleton<LocalizationService, LocalizationService>();

        return services;
    }

    /// <summary>
    /// Adds security and authentication services.
    /// </summary>
    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Password hasher
        services.AddSingleton<PasswordHasher, PasswordHasher>();

        // Token service with configuration
        services.AddSingleton<TokenService>(sp =>
        {
            var options = new TokenServiceOptions();
            configuration.GetSection("Security:Token").Bind(options);
            return new TokenService(options);
        });

        // Current user service (singleton for desktop app)
        services.AddSingleton<CurrentUserService, CurrentUserService>();

        // Auth service (scoped to use DbContext)
        services.AddScoped<AuthService, AuthService>();

        // Authorization service
        services.AddScoped<AuthorizationService, AuthorizationService>();

        return services;
    }

    /// <summary>
    /// Adds data layer services (DAOs, Repositories, Unit of Work).
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

        // DAOs (EF Core access is isolated here)
        services.AddScoped<CustomerDao, CustomerDaoImpl>();
        services.AddScoped<OrderDao, OrderDaoImpl>();
        services.AddScoped<ProductDao, ProductDaoImpl>();

        // Generic repository (Arcana.Core.Common.Repository<T> → RepositoryImpl<T>)
        services.AddScoped(typeof(CoreCommon.Repository<>), typeof(DataRepository.RepositoryImpl<>));
        services.AddScoped(typeof(CoreCommon.Repository<,>), typeof(DataRepository.RepositoryImpl<,>));

        // Entity-specific repositories (inject corresponding DAOs)
        services.AddScoped<OrderRepository, OrderRepositoryImpl>();
        services.AddScoped<CustomerRepository, CustomerRepositoryImpl>();
        services.AddScoped<ProductRepository, ProductRepositoryImpl>();

        // Unit of Work
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
        });
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<UnitOfWorkFactory, UnitOfWorkFactory>();

        return services;
    }

    /// <summary>
    /// Adds domain layer services (service impls in Infrastructure).
    /// </summary>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Service implementations (interfaces are in Arcana.Domain.Services)
        services.AddScoped<OrderService, OrderServiceImpl>();
        services.AddScoped<CustomerService, CustomerServiceImpl>();
        services.AddScoped<ProductService, ProductServiceImpl>();

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
        services.AddSingleton<CommandService, CommandService>();
        services.AddSingleton<IMenuRegistry, MenuRegistry>();
        services.AddSingleton<IViewRegistry, ViewRegistry>();

        // Plugin health monitor
        services.AddSingleton<PluginHealthMonitor>();

        // Manifest and lazy loading services
        services.AddSingleton<ManifestService, ManifestService>();
        services.AddSingleton<ActivationEventService>();
        services.AddSingleton<IActivationEventService>(sp =>
            sp.GetRequiredService<ActivationEventService>());
        services.AddSingleton<LazyContributionService>();

        // Plugin manager (core)
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<PluginManager>>();
            var pluginsPath = Path.Combine(AppContext.BaseDirectory, "plugins");
            var manager = new PluginManager(sp, logger, pluginsPath);

            // Initialize lazy loading
            var manifestService = sp.GetRequiredService<ManifestService>();
            var activationService = sp.GetRequiredService<ActivationEventService>();
            var lazyContribService = sp.GetRequiredService<LazyContributionService>();
            manager.InitializeLazyLoading(manifestService, activationService, lazyContribService);

            return manager;
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
        services.AddSingleton<SyncService, SyncService>();

        return services;
    }
}
