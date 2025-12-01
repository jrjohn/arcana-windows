using Arcana.App.Views;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;

namespace Arcana.App.Plugins;

/// <summary>
/// Product module plugin - provides product management functionality.
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

    protected override async Task OnActivateAsync(IPluginContext context)
    {
        // Load localization from external JSON files
        var localesPath = Path.Combine(AppContext.BaseDirectory, "Plugins", "ProductModule", "locales");
        await LoadExternalLocalizationAsync(localesPath);
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

        // Note: FunctionTree items are not registered here because they duplicate
        // the built-in navigation items defined in MainWindow.xaml

        // Quick Access - New Product
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "quick.newProduct",
                Title = L("product.new"),
                Location = MenuLocation.QuickAccess,
                Icon = "\uE719",
                Order = 3,
                Group = "business",
                Command = "product.new"
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
