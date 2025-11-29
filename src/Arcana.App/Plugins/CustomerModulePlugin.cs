using Arcana.App.Views;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;

namespace Arcana.App.Plugins;

/// <summary>
/// Customer module plugin - provides customer management functionality.
/// 客戶模組插件 - 提供客戶管理功能
/// </summary>
public class CustomerModulePlugin : PluginBase
{
    public override PluginMetadata Metadata => new()
    {
        Id = "arcana.module.customer",
        Name = "Customer Module",
        Version = new Version(1, 0, 0),
        Description = "Customer management module",
        Type = PluginType.Module
    };

    protected override void RegisterContributions(IPluginContext context)
    {
        // Register views
        RegisterView(new ViewDefinition
        {
            Id = "CustomerListPage",
            Title = "客戶管理",
            Icon = "\uE716",
            Type = ViewType.Page,
            ViewType = typeof(CustomerListPage),
            Category = "業務"
        });

        // Register menu items
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.customer",
                Title = "客戶",
                Location = MenuLocation.MainMenu,
                Order = 11
            },
            new MenuItemDefinition
            {
                Id = "menu.customer.list",
                Title = "客戶管理",
                Location = MenuLocation.MainMenu,
                ParentId = "menu.customer",
                Icon = "\uE716",
                Order = 1,
                Command = "customer.list"
            },
            new MenuItemDefinition
            {
                Id = "menu.customer.new",
                Title = "新增客戶",
                Location = MenuLocation.MainMenu,
                ParentId = "menu.customer",
                Icon = "\uE77B",
                Order = 2,
                Command = "customer.new"
            }
        );

        // Register function tree items
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "tree.customer",
                Title = "客戶管理",
                Location = MenuLocation.FunctionTree,
                Icon = "\uE716",
                Order = 11,
                Command = "customer.list"
            }
        );

        // Register commands
        RegisterCommand("customer.list", () =>
        {
            return Context!.Navigation.NavigateToAsync("CustomerListPage");
        });

        RegisterCommand("customer.new", () =>
        {
            return Context!.Navigation.NavigateToNewTabAsync("CustomerDetailPage");
        });

        LogInfo("Customer module plugin activated");
    }
}
