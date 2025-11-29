using Arcana.App.ViewModels;
using Arcana.App.Views;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Arcana.App.Plugins;

/// <summary>
/// Order module plugin - provides order management functionality.
/// 訂單模組插件 - 提供訂單管理功能
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

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<OrderListViewModel>();
        services.AddTransient<OrderDetailViewModel>();
    }

    protected override void RegisterContributions(IPluginContext context)
    {
        // Register views
        RegisterView(new ViewDefinition
        {
            Id = "OrderListPage",
            Title = "訂單管理",
            Icon = "\uE7C3",
            Type = ViewType.Page,
            ViewType = typeof(OrderListPage),
            ViewModelType = typeof(OrderListViewModel),
            Category = "業務"
        });

        RegisterView(new ViewDefinition
        {
            Id = "OrderDetailPage",
            Title = "訂單明細",
            Icon = "\uE7C3",
            Type = ViewType.Page,
            ViewType = typeof(OrderDetailPage),
            ViewModelType = typeof(OrderDetailViewModel),
            CanHaveMultipleInstances = true,
            Category = "業務"
        });

        // Register menu items
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.order",
                Title = "訂單",
                Location = MenuLocation.MainMenu,
                Order = 10
            },
            new MenuItemDefinition
            {
                Id = "menu.order.list",
                Title = "訂單管理",
                Location = MenuLocation.MainMenu,
                ParentId = "menu.order",
                Icon = "\uE7C3",
                Order = 1,
                Command = "order.list"
            },
            new MenuItemDefinition
            {
                Id = "menu.order.new",
                Title = "新增訂單",
                Location = MenuLocation.MainMenu,
                ParentId = "menu.order",
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
                Title = "訂單管理",
                Location = MenuLocation.FunctionTree,
                Icon = "\uE7C3",
                Order = 10,
                Command = "order.list"
            }
        );

        // Register commands
        RegisterCommand("order.list", () =>
        {
            return Context!.Navigation.NavigateToAsync("OrderListPage");
        });

        RegisterCommand("order.new", () =>
        {
            return Context!.Navigation.NavigateToNewTabAsync("OrderDetailPage");
        });

        RegisterCommand<int>("order.view", orderId =>
        {
            return Context!.Navigation.NavigateToNewTabAsync("OrderDetailPage", orderId);
        });

        LogInfo("Order module plugin activated");
    }
}
