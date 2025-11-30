using Arcana.App.Views;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;

namespace Arcana.App.Plugins;

/// <summary>
/// Customer module plugin - provides customer management functionality.
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

    protected override async Task OnActivateAsync(IPluginContext context)
    {
        // Load localization from external JSON files
        var localesPath = Path.Combine(AppContext.BaseDirectory, "Plugins", "CustomerModule", "locales");
        await LoadExternalLocalizationAsync(localesPath);
    }

    protected override void RegisterContributions(IPluginContext context)
    {
        // Register views
        RegisterView(new ViewDefinition
        {
            Id = "CustomerListPage",
            Title = L("customer.list"),
            Icon = "\uE716",
            Type = ViewType.Page,
            ViewClass = typeof(CustomerListPage),
            Category = L("menu.business")
        });

        // Register menu items under Business menu
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.business.customer",
                Title = L("customer.title"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.business",
                Icon = "\uE716",
                Order = 2
            },
            new MenuItemDefinition
            {
                Id = "menu.business.customer.list",
                Title = L("customer.list"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.business.customer",
                Icon = "\uE716",
                Order = 1,
                Command = "customer.list"
            },
            new MenuItemDefinition
            {
                Id = "menu.business.customer.new",
                Title = L("customer.new"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.business.customer",
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
                Title = L("customer.list"),
                Location = MenuLocation.FunctionTree,
                Icon = "\uE716",
                Order = 11,
                Command = "customer.list"
            }
        );

        // Quick Access - New Customer
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "quick.newCustomer",
                Title = L("customer.new"),
                Location = MenuLocation.QuickAccess,
                Icon = "\uE716",
                Order = 2,
                Group = "business",
                Command = "customer.new"
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
