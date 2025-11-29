using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Arcana.App.Plugins;

/// <summary>
/// Core menu plugin - provides the main application menus.
/// 核心菜單插件 - 提供主要應用程式菜單
/// </summary>
public class CoreMenuPlugin : PluginBase
{
    public override PluginMetadata Metadata => new()
    {
        Id = "arcana.core.menu",
        Name = "Core Menu",
        Version = new Version(1, 0, 0),
        Description = "Provides core application menus",
        Type = PluginType.Menu
    };

    protected override void RegisterContributions(IPluginContext context)
    {
        // File Menu
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.file",
                Title = "檔案",
                Location = MenuLocation.MainMenu,
                Order = 1
            },
            new MenuItemDefinition
            {
                Id = "menu.file.new",
                Title = "新增",
                Location = MenuLocation.FileMenu,
                ParentId = "menu.file",
                Icon = "\uE710",
                Shortcut = "Ctrl+N",
                Order = 1,
                Command = "file.new"
            },
            new MenuItemDefinition
            {
                Id = "menu.file.open",
                Title = "開啟",
                Location = MenuLocation.FileMenu,
                ParentId = "menu.file",
                Icon = "\uE8E5",
                Shortcut = "Ctrl+O",
                Order = 2,
                Command = "file.open"
            },
            new MenuItemDefinition
            {
                Id = "menu.file.save",
                Title = "儲存",
                Location = MenuLocation.FileMenu,
                ParentId = "menu.file",
                Icon = "\uE74E",
                Shortcut = "Ctrl+S",
                Order = 3,
                Command = "file.save"
            },
            new MenuItemDefinition
            {
                Id = "menu.file.separator1",
                Title = "",
                Location = MenuLocation.FileMenu,
                ParentId = "menu.file",
                IsSeparator = true,
                Order = 10
            },
            new MenuItemDefinition
            {
                Id = "menu.file.exit",
                Title = "結束",
                Location = MenuLocation.FileMenu,
                ParentId = "menu.file",
                Shortcut = "Alt+F4",
                Order = 99,
                Command = "app.exit"
            }
        );

        // Edit Menu
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.edit",
                Title = "編輯",
                Location = MenuLocation.MainMenu,
                Order = 2
            },
            new MenuItemDefinition
            {
                Id = "menu.edit.undo",
                Title = "復原",
                Location = MenuLocation.EditMenu,
                ParentId = "menu.edit",
                Icon = "\uE7A7",
                Shortcut = "Ctrl+Z",
                Order = 1,
                Command = "edit.undo"
            },
            new MenuItemDefinition
            {
                Id = "menu.edit.redo",
                Title = "重做",
                Location = MenuLocation.EditMenu,
                ParentId = "menu.edit",
                Icon = "\uE7A6",
                Shortcut = "Ctrl+Y",
                Order = 2,
                Command = "edit.redo"
            },
            new MenuItemDefinition
            {
                Id = "menu.edit.separator1",
                Title = "",
                Location = MenuLocation.EditMenu,
                ParentId = "menu.edit",
                IsSeparator = true,
                Order = 10
            },
            new MenuItemDefinition
            {
                Id = "menu.edit.cut",
                Title = "剪下",
                Location = MenuLocation.EditMenu,
                ParentId = "menu.edit",
                Icon = "\uE8C6",
                Shortcut = "Ctrl+X",
                Order = 11,
                Command = "edit.cut"
            },
            new MenuItemDefinition
            {
                Id = "menu.edit.copy",
                Title = "複製",
                Location = MenuLocation.EditMenu,
                ParentId = "menu.edit",
                Icon = "\uE8C8",
                Shortcut = "Ctrl+C",
                Order = 12,
                Command = "edit.copy"
            },
            new MenuItemDefinition
            {
                Id = "menu.edit.paste",
                Title = "貼上",
                Location = MenuLocation.EditMenu,
                ParentId = "menu.edit",
                Icon = "\uE77F",
                Shortcut = "Ctrl+V",
                Order = 13,
                Command = "edit.paste"
            }
        );

        // Business Menu (業務) - parent for module plugins
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.business",
                Title = "業務",
                Location = MenuLocation.MainMenu,
                Order = 3
            }
        );

        // View Menu
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.view",
                Title = "檢視",
                Location = MenuLocation.MainMenu,
                Order = 4
            },
            new MenuItemDefinition
            {
                Id = "menu.view.refresh",
                Title = "重新整理",
                Location = MenuLocation.ViewMenu,
                ParentId = "menu.view",
                Icon = "\uE72C",
                Shortcut = "F5",
                Order = 1,
                Command = "view.refresh"
            },
            new MenuItemDefinition
            {
                Id = "menu.view.fullscreen",
                Title = "全螢幕",
                Location = MenuLocation.ViewMenu,
                ParentId = "menu.view",
                Shortcut = "F11",
                Order = 2,
                Command = "view.fullscreen"
            }
        );

        // Tools Menu
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.tools",
                Title = "工具",
                Location = MenuLocation.MainMenu,
                Order = 5
            },
            new MenuItemDefinition
            {
                Id = "menu.tools.settings",
                Title = "設定",
                Location = MenuLocation.ToolsMenu,
                ParentId = "menu.tools",
                Icon = "\uE713",
                Shortcut = "Ctrl+,",
                Order = 99,
                Command = "app.settings"
            }
        );

        // Help Menu
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.help",
                Title = "說明",
                Location = MenuLocation.MainMenu,
                Order = 99
            },
            new MenuItemDefinition
            {
                Id = "menu.help.docs",
                Title = "文件",
                Location = MenuLocation.HelpMenu,
                ParentId = "menu.help",
                Icon = "\uE7BC",
                Order = 1,
                Command = "help.docs"
            },
            new MenuItemDefinition
            {
                Id = "menu.help.about",
                Title = "關於",
                Location = MenuLocation.HelpMenu,
                ParentId = "menu.help",
                Order = 99,
                Command = "app.about"
            }
        );

        // Register commands
        RegisterCommand("file.new", () =>
        {
            // Navigate to home page or create new document
            Context!.Navigation.NavigateToAsync("HomePage");
            return Task.CompletedTask;
        });

        RegisterCommand("file.open", async () =>
        {
            var files = await Context!.Window.ShowOpenFileDialogAsync(new FileDialogOptions
            {
                Title = "開啟檔案",
                Filters = [new FileFilter("所有檔案", "*")]
            });
            // TODO: Handle file open
        });

        RegisterCommand("file.save", () =>
        {
            // TODO: Save current document
            return Task.CompletedTask;
        });

        RegisterCommand("app.exit", () =>
        {
            Microsoft.UI.Xaml.Application.Current.Exit();
            return Task.CompletedTask;
        });

        RegisterCommand("app.settings", () =>
        {
            Context!.Navigation.NavigateToAsync("SettingsPage");
            return Task.CompletedTask;
        });

        RegisterCommand("app.about", async () =>
        {
            await Context!.Window.ShowInfoAsync(
                "關於 Arcana\n\nArcana 企業管理系統\n版本: 1.0.0\n\n© 2024 Arcana Software",
                "確定");
        });

        RegisterCommand("help.docs", async () =>
        {
            // Open documentation URL
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://docs.arcana.app"));
        });

        RegisterCommand("view.refresh", () =>
        {
            // Publish refresh event
            return Task.CompletedTask;
        });

        RegisterCommand("view.fullscreen", () =>
        {
            // Toggle fullscreen - requires window handle access
            return Task.CompletedTask;
        });

        // Edit commands (placeholder - typically handled by focused control)
        RegisterCommand("edit.undo", () => Task.CompletedTask);
        RegisterCommand("edit.redo", () => Task.CompletedTask);
        RegisterCommand("edit.cut", () => Task.CompletedTask);
        RegisterCommand("edit.copy", () => Task.CompletedTask);
        RegisterCommand("edit.paste", () => Task.CompletedTask);

        LogInfo("Core menu plugin activated");
    }
}
