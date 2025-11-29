using Arcana.App.ViewModels;
using Arcana.App.Views;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Arcana.App.Plugins;

/// <summary>
/// System plugin - provides system management functionality.
/// </summary>
public class SystemPlugin : PluginBase
{
    public override PluginMetadata Metadata => new()
    {
        Id = "arcana.system",
        Name = "System",
        Version = new Version(1, 0, 0),
        Description = "System management and settings",
        Type = PluginType.Module,
        Author = "Arcana Team"
    };

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<PluginManagerViewModel>();
    }

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
            ["menu.system"] = "系統",
            ["menu.system.plugins"] = "外掛管理",
            ["menu.system.settings"] = "設定"
        });

        // English (en-US)
        RegisterResources("en-US", new Dictionary<string, string>
        {
            ["menu.system"] = "System",
            ["menu.system.plugins"] = "Plugin Manager",
            ["menu.system.settings"] = "Settings"
        });

        // Japanese (ja-JP)
        RegisterResources("ja-JP", new Dictionary<string, string>
        {
            ["menu.system"] = "システム",
            ["menu.system.plugins"] = "プラグイン管理",
            ["menu.system.settings"] = "設定"
        });
    }

    protected override void RegisterContributions(IPluginContext context)
    {
        // Register views
        RegisterView(new ViewDefinition
        {
            Id = "PluginManagerPage",
            Title = L("menu.system.plugins"),
            Icon = "\uEA86",
            Type = ViewType.Page,
            ViewClass = typeof(PluginManagerPage),
            ViewModelType = typeof(PluginManagerViewModel),
            Category = L("menu.system")
        });

        RegisterView(new ViewDefinition
        {
            Id = "SettingsPage",
            Title = L("menu.system.settings"),
            Icon = "\uE713",
            Type = ViewType.Page,
            ViewClass = typeof(SettingsPage),
            Category = L("menu.system")
        });

        // Register menu items
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.system",
                Title = L("menu.system"),
                Location = MenuLocation.MainMenu,
                Order = 100
            },
            new MenuItemDefinition
            {
                Id = "menu.system.plugins",
                Title = L("menu.system.plugins"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.system",
                Icon = "\uEA86",
                Order = 1,
                Command = "system.plugins"
            },
            new MenuItemDefinition
            {
                Id = "menu.system.settings",
                Title = L("menu.system.settings"),
                Location = MenuLocation.MainMenu,
                ParentId = "menu.system",
                Icon = "\uE713",
                Shortcut = "Ctrl+,",
                Order = 99,
                Command = "system.settings"
            }
        );

        // Register function tree items
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "tree.system",
                Title = L("menu.system"),
                Location = MenuLocation.FunctionTree,
                Icon = "\uE770",
                Order = 100
            },
            new MenuItemDefinition
            {
                Id = "tree.system.plugins",
                Title = L("menu.system.plugins"),
                Location = MenuLocation.FunctionTree,
                ParentId = "tree.system",
                Icon = "\uEA86",
                Order = 1,
                Command = "system.plugins"
            },
            new MenuItemDefinition
            {
                Id = "tree.system.settings",
                Title = L("menu.system.settings"),
                Location = MenuLocation.FunctionTree,
                ParentId = "tree.system",
                Icon = "\uE713",
                Order = 99,
                Command = "system.settings"
            }
        );

        // Register commands
        RegisterCommand("system.plugins", () =>
        {
            return Context!.Navigation.NavigateToNewTabAsync("PluginManagerPage");
        });

        RegisterCommand("system.settings", () =>
        {
            return Context!.Navigation.NavigateToAsync("SettingsPage");
        });

        LogInfo("System plugin activated");
    }
}
