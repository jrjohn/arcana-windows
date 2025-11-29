using Arcana.App.ViewModels;
using Arcana.App.Views;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Arcana.App.Plugins;

/// <summary>
/// System plugin - provides system management functionality.
/// 系統插件 - 提供系統管理功能
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

    protected override void RegisterContributions(IPluginContext context)
    {
        // Register views
        RegisterView(new ViewDefinition
        {
            Id = "PluginManagerPage",
            Title = "Plugin Manager",
            Icon = "\uEA86",
            Type = ViewType.Page,
            ViewType = typeof(PluginManagerPage),
            ViewModelType = typeof(PluginManagerViewModel),
            Category = "System"
        });

        RegisterView(new ViewDefinition
        {
            Id = "SettingsPage",
            Title = "Settings",
            Icon = "\uE713",
            Type = ViewType.Page,
            ViewType = typeof(SettingsPage),
            Category = "System"
        });

        // Register menu items
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.system",
                Title = "System",
                Location = MenuLocation.MainMenu,
                Order = 100
            },
            new MenuItemDefinition
            {
                Id = "menu.system.plugins",
                Title = "Plugin Manager",
                Location = MenuLocation.MainMenu,
                ParentId = "menu.system",
                Icon = "\uEA86",
                Order = 1,
                Command = "system.plugins"
            },
            new MenuItemDefinition
            {
                Id = "menu.system.settings",
                Title = "Settings",
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
                Title = "System",
                Location = MenuLocation.FunctionTree,
                Icon = "\uE770",
                Order = 100
            },
            new MenuItemDefinition
            {
                Id = "tree.system.plugins",
                Title = "Plugin Manager",
                Location = MenuLocation.FunctionTree,
                ParentId = "tree.system",
                Icon = "\uEA86",
                Order = 1,
                Command = "system.plugins"
            },
            new MenuItemDefinition
            {
                Id = "tree.system.settings",
                Title = "Settings",
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
