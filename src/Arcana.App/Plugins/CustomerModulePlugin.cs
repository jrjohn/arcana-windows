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
            ["customer.title"] = "客戶",
            ["customer.list"] = "客戶管理",
            ["customer.new"] = "新增客戶",
            ["customer.detail"] = "客戶明細",
            ["customer.name"] = "客戶名稱",
            ["customer.code"] = "客戶代碼",
            ["customer.contact"] = "聯絡人",
            ["customer.phone"] = "電話",
            ["customer.email"] = "電子郵件",
            ["customer.address"] = "地址",
            ["menu.business"] = "業務"
        });

        // English (en-US)
        RegisterResources("en-US", new Dictionary<string, string>
        {
            ["customer.title"] = "Customer",
            ["customer.list"] = "Customer Management",
            ["customer.new"] = "New Customer",
            ["customer.detail"] = "Customer Detail",
            ["customer.name"] = "Customer Name",
            ["customer.code"] = "Customer Code",
            ["customer.contact"] = "Contact",
            ["customer.phone"] = "Phone",
            ["customer.email"] = "Email",
            ["customer.address"] = "Address",
            ["menu.business"] = "Business"
        });

        // Japanese (ja-JP)
        RegisterResources("ja-JP", new Dictionary<string, string>
        {
            ["customer.title"] = "顧客",
            ["customer.list"] = "顧客管理",
            ["customer.new"] = "新規顧客",
            ["customer.detail"] = "顧客詳細",
            ["customer.name"] = "顧客名",
            ["customer.code"] = "顧客コード",
            ["customer.contact"] = "連絡先",
            ["customer.phone"] = "電話",
            ["customer.email"] = "メール",
            ["customer.address"] = "住所",
            ["menu.business"] = "業務"
        });
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
