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

    protected override Task OnActivateAsync(IPluginContext context)
    {
        RegisterPluginResources();
        return Task.CompletedTask;
    }

    private void RegisterPluginResources()
    {
        // Traditional Chinese (zh-TW)
        RegisterResources("zh-TW", new Dictionary<string, string>
        {
            ["order.title"] = "訂單",
            ["order.list"] = "訂單管理",
            ["order.new"] = "新增訂單",
            ["order.detail"] = "訂單明細",
            ["order.number"] = "訂單編號",
            ["order.date"] = "訂單日期",
            ["order.customer"] = "客戶",
            ["order.status"] = "狀態",
            ["order.total"] = "總金額",
            ["order.items"] = "訂單項目",
            ["menu.business"] = "業務",
            ["module.order.new"] = "新增訂單",
            ["module.order.list"] = "訂單列表"
        });

        // English (en-US)
        RegisterResources("en-US", new Dictionary<string, string>
        {
            ["order.title"] = "Order",
            ["order.list"] = "Order Management",
            ["order.new"] = "New Order",
            ["order.detail"] = "Order Detail",
            ["order.number"] = "Order Number",
            ["order.date"] = "Order Date",
            ["order.customer"] = "Customer",
            ["order.status"] = "Status",
            ["order.total"] = "Total Amount",
            ["order.items"] = "Order Items",
            ["menu.business"] = "Business",
            ["module.order.new"] = "New Order",
            ["module.order.list"] = "Order List"
        });

        // Japanese (ja-JP)
        RegisterResources("ja-JP", new Dictionary<string, string>
        {
            ["order.title"] = "注文",
            ["order.list"] = "注文管理",
            ["order.new"] = "新規注文",
            ["order.detail"] = "注文詳細",
            ["order.number"] = "注文番号",
            ["order.date"] = "注文日",
            ["order.customer"] = "顧客",
            ["order.status"] = "ステータス",
            ["order.total"] = "合計金額",
            ["order.items"] = "注文項目",
            ["menu.business"] = "業務",
            ["module.order.new"] = "新規注文",
            ["module.order.list"] = "注文一覧"
        });
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

        // Register function tree items
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "tree.order",
                Title = L("order.list"),
                Location = MenuLocation.FunctionTree,
                Icon = "\uE7C3",
                Order = 10,
                Command = "order.list"
            }
        );

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
