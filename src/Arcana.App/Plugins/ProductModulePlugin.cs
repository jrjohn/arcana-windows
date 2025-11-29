using Arcana.App.Views;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;

namespace Arcana.App.Plugins;

/// <summary>
/// Product module plugin - provides product management functionality.
/// 產品模組插件 - 提供產品管理功能
/// </summary>
public class ProductModulePlugin : PluginBase
{
    public override PluginMetadata Metadata => new()
    {
        Id = "arcana.module.product",
        Name = "Product Module",
        Version = new Version(1, 0, 0),
        Description = "Product management module",
        Type = PluginType.Module
    };

    protected override void RegisterContributions(IPluginContext context)
    {
        // Register views
        RegisterView(new ViewDefinition
        {
            Id = "ProductListPage",
            Title = "產品管理",
            Icon = "\uE719",
            Type = ViewType.Page,
            ViewType = typeof(ProductListPage),
            Category = "業務"
        });

        // Register menu items
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.product",
                Title = "產品",
                Location = MenuLocation.MainMenu,
                Order = 12
            },
            new MenuItemDefinition
            {
                Id = "menu.product.list",
                Title = "產品管理",
                Location = MenuLocation.MainMenu,
                ParentId = "menu.product",
                Icon = "\uE719",
                Order = 1,
                Command = "product.list"
            },
            new MenuItemDefinition
            {
                Id = "menu.product.new",
                Title = "新增產品",
                Location = MenuLocation.MainMenu,
                ParentId = "menu.product",
                Icon = "\uE7BF",
                Order = 2,
                Command = "product.new"
            },
            new MenuItemDefinition
            {
                Id = "menu.product.categories",
                Title = "產品分類",
                Location = MenuLocation.MainMenu,
                ParentId = "menu.product",
                Icon = "\uE8FD",
                Order = 3,
                Command = "product.categories"
            }
        );

        // Register function tree items
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "tree.product",
                Title = "產品管理",
                Location = MenuLocation.FunctionTree,
                Icon = "\uE719",
                Order = 12,
                Command = "product.list"
            }
        );

        // Register commands
        RegisterCommand("product.list", () =>
        {
            return Context!.Navigation.NavigateToAsync("ProductListPage");
        });

        RegisterCommand("product.new", () =>
        {
            return Context!.Navigation.NavigateToNewTabAsync("ProductDetailPage");
        });

        RegisterCommand("product.categories", () =>
        {
            return Context!.Navigation.NavigateToAsync("ProductCategoriesPage");
        });

        LogInfo("Product module plugin activated");
    }
}
