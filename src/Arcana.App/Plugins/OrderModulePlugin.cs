using Arcana.App.ViewModels;
using Arcana.App.Views;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Arcana.App.Plugins;

/// <summary>
/// Order module plugin - provides order management functionality.
/// </summary>
public class OrderModulePlugin : PluginBase
{
    public override PluginMetadata Metadata => new()
    {
        Id = "arcana.module.order",
        Name = "Order Module",
        Version = new Version(1, 0, 0),
        Description = "Order management module",
        Type = PluginType.Module
    };

    protected override async Task OnActivateAsync(IPluginContext context)
    {
        // Load localization from external JSON files
        var localesPath = Path.Combine(AppContext.BaseDirectory, "Plugins", "OrderModule", "locales");
        await LoadExternalLocalizationAsync(localesPath);
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<OrderListViewModel>();
        services.AddTransient<OrderDetailViewModel>();
    }

    protected override void RegisterContributions(IPluginContext context)
    {
        // Register views
        // OrderListPage is a default tab in the OrderModule - it's always created when the module loads
        RegisterView(new ViewDefinition
        {
            Id = "OrderListPage",
            Title = L("order.list"),
            TitleKey = "order.list", // For dynamic localization when language changes
            Icon = "\uE8A5",
            Type = ViewType.Page,
            ViewClass = typeof(OrderListPage),
            ViewModelType = typeof(OrderListViewModel),
            Category = L("menu.business"),
            // Module default tab configuration
            ModuleId = "OrderModule",
            IsModuleDefaultTab = true,
            ModuleTabOrder = 0
        });

        RegisterView(new ViewDefinition
        {
            Id = "OrderDetailPage",
            Title = L("order.detail"),
            TitleKey = "order.detail", // For dynamic localization when language changes
            Icon = "\uE7C3",
            Type = ViewType.Page,
            ViewClass = typeof(OrderDetailPage),
            ViewModelType = typeof(OrderDetailViewModel),
            CanHaveMultipleInstances = true,
            Category = L("menu.business"),
            // Not a default tab - opened on demand
            ModuleId = "OrderModule"
        });

        // Register menu items under Business menu
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.business.order",
                Title = L("order.title"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.business",
                Icon = "\uE7C3",
                Order = 1
            },
            new MenuItemDefinition
            {
                Id = "menu.business.order.list",
                Title = L("order.list"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.business.order",
                Icon = "\uE7C3",
                Order = 1,
                Command = "order.list"
            },
            new MenuItemDefinition
            {
                Id = "menu.business.order.new",
                Title = L("order.new"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.business.order",
                Icon = "\uE710",
                Shortcut = "Ctrl+Shift+O",
                Order = 2,
                Command = "order.new"
            }
        );

        // Note: FunctionTree items are not registered here because they duplicate
        // the built-in navigation items defined in MainWindow.xaml

        // Quick Access - New Order (main tab strip)
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "quick.newOrder",
                Title = L("order.new"),
                Location = MenuLocation.QuickAccess,
                Icon = "\uE7C3",
                Order = 1,
                Group = "business",
                Command = "order.new"
            }
        );

        // Module Quick Access - Order module nested tab strip
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "module.order.new",
                Title = L("order.new"),
                Location = MenuLocation.ModuleQuickAccess,
                ModuleId = "OrderModule",
                Icon = "\uE710",
                Order = 1,
                Group = "actions",
                Command = "module.order.new"
            },
            new MenuItemDefinition
            {
                Id = "module.order.list",
                Title = L("module.order.list"),
                Location = MenuLocation.ModuleQuickAccess,
                ModuleId = "OrderModule",
                Icon = "\uE8A5",
                Order = 10,
                Group = "navigation",
                Command = "module.order.list"
            }
        );

        // Register commands
        RegisterCommand("order.list", () =>
        {
            return Context!.Navigation.NavigateToAsync("OrderListPage");
        });

        RegisterCommand("order.new", () =>
        {
            // Navigate within OrderListPage tab instead of creating a new tab
            return Context!.Navigation.NavigateWithinTabAsync("OrderListPage", "OrderDetailPage");
        });

        RegisterCommand<int>("order.view", orderId =>
        {
            // Open existing order in a new tab for multi-tab reference capability
            return Context!.Navigation.NavigateToNewTabAsync("OrderDetailPage", orderId);
        });

        LogInfo("Order module plugin activated");
    }
}
