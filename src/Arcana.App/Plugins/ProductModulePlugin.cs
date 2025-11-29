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
            ["product.title"] = "產品",
            ["product.list"] = "產品管理",
            ["product.new"] = "新增產品",
            ["product.detail"] = "產品明細",
            ["product.categories"] = "產品分類",
            ["product.name"] = "產品名稱",
            ["product.code"] = "產品代碼",
            ["product.price"] = "單價",
            ["product.stock"] = "庫存",
            ["product.unit"] = "單位",
            ["menu.business"] = "業務"
        });

        // English (en-US)
        RegisterResources("en-US", new Dictionary<string, string>
        {
            ["product.title"] = "Product",
            ["product.list"] = "Product Management",
            ["product.new"] = "New Product",
            ["product.detail"] = "Product Detail",
            ["product.categories"] = "Product Categories",
            ["product.name"] = "Product Name",
            ["product.code"] = "Product Code",
            ["product.price"] = "Unit Price",
            ["product.stock"] = "Stock",
            ["product.unit"] = "Unit",
            ["menu.business"] = "Business"
        });

        // Japanese (ja-JP)
        RegisterResources("ja-JP", new Dictionary<string, string>
        {
            ["product.title"] = "製品",
            ["product.list"] = "製品管理",
            ["product.new"] = "新規製品",
            ["product.detail"] = "製品詳細",
            ["product.categories"] = "製品カテゴリ",
            ["product.name"] = "製品名",
            ["product.code"] = "製品コード",
            ["product.price"] = "単価",
            ["product.stock"] = "在庫",
            ["product.unit"] = "単位",
            ["menu.business"] = "業務"
        });
    }

    protected override void RegisterContributions(IPluginContext context)
    {
        // Register views
        RegisterView(new ViewDefinition
        {
            Id = "ProductListPage",
            Title = L("product.list"),
            Icon = "\uE719",
            Type = ViewType.Page,
            ViewClass = typeof(ProductListPage),
            Category = L("menu.business")
        });

        // Register menu items under Business menu
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.business.product",
                Title = L("product.title"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.business",
                Icon = "\uE719",
                Order = 3
            },
            new MenuItemDefinition
            {
                Id = "menu.business.product.list",
                Title = L("product.list"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.business.product",
                Icon = "\uE719",
                Order = 1,
                Command = "product.list"
            },
            new MenuItemDefinition
            {
                Id = "menu.business.product.new",
                Title = L("product.new"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.business.product",
                Icon = "\uE7BF",
                Order = 2,
                Command = "product.new"
            },
            new MenuItemDefinition
            {
                Id = "menu.business.product.categories",
                Title = L("product.categories"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.business.product",
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
                Title = L("product.list"),
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
