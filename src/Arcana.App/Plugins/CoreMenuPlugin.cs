using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Arcana.App.Plugins;

/// <summary>
/// Core menu plugin - provides the main application menus.
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
            ["menu.file"] = "檔案",
            ["menu.file.new"] = "新增",
            ["menu.file.open"] = "開啟",
            ["menu.file.save"] = "儲存",
            ["menu.file.exit"] = "結束",
            ["menu.edit"] = "編輯",
            ["menu.edit.undo"] = "復原",
            ["menu.edit.redo"] = "重做",
            ["menu.edit.cut"] = "剪下",
            ["menu.edit.copy"] = "複製",
            ["menu.edit.paste"] = "貼上",
            ["menu.business"] = "業務",
            ["menu.view"] = "檢視",
            ["menu.view.refresh"] = "重新整理",
            ["menu.view.fullscreen"] = "全螢幕",
            ["menu.tools"] = "工具",
            ["menu.tools.settings"] = "設定",
            ["menu.help"] = "說明",
            ["menu.help.docs"] = "文件",
            ["menu.help.about"] = "關於",
            ["quick.home"] = "首頁",
            ["common.all.files"] = "所有檔案",
            ["common.ok"] = "確定",
            ["app.version"] = "1.0.0",
            ["app.about.content"] = "Arcana 企業管理系統\n版本: {0}\n\n© 2024 Arcana Software"
        });

        // English (en-US)
        RegisterResources("en-US", new Dictionary<string, string>
        {
            ["menu.file"] = "File",
            ["menu.file.new"] = "New",
            ["menu.file.open"] = "Open",
            ["menu.file.save"] = "Save",
            ["menu.file.exit"] = "Exit",
            ["menu.edit"] = "Edit",
            ["menu.edit.undo"] = "Undo",
            ["menu.edit.redo"] = "Redo",
            ["menu.edit.cut"] = "Cut",
            ["menu.edit.copy"] = "Copy",
            ["menu.edit.paste"] = "Paste",
            ["menu.business"] = "Business",
            ["menu.view"] = "View",
            ["menu.view.refresh"] = "Refresh",
            ["menu.view.fullscreen"] = "Fullscreen",
            ["menu.tools"] = "Tools",
            ["menu.tools.settings"] = "Settings",
            ["menu.help"] = "Help",
            ["menu.help.docs"] = "Documentation",
            ["menu.help.about"] = "About",
            ["quick.home"] = "Home",
            ["common.all.files"] = "All Files",
            ["common.ok"] = "OK",
            ["app.version"] = "1.0.0",
            ["app.about.content"] = "Arcana Enterprise Management System\nVersion: {0}\n\n© 2024 Arcana Software"
        });

        // Japanese (ja-JP)
        RegisterResources("ja-JP", new Dictionary<string, string>
        {
            ["menu.file"] = "ファイル",
            ["menu.file.new"] = "新規",
            ["menu.file.open"] = "開く",
            ["menu.file.save"] = "保存",
            ["menu.file.exit"] = "終了",
            ["menu.edit"] = "編集",
            ["menu.edit.undo"] = "元に戻す",
            ["menu.edit.redo"] = "やり直し",
            ["menu.edit.cut"] = "切り取り",
            ["menu.edit.copy"] = "コピー",
            ["menu.edit.paste"] = "貼り付け",
            ["menu.business"] = "業務",
            ["menu.view"] = "表示",
            ["menu.view.refresh"] = "更新",
            ["menu.view.fullscreen"] = "全画面",
            ["menu.tools"] = "ツール",
            ["menu.tools.settings"] = "設定",
            ["menu.help"] = "ヘルプ",
            ["menu.help.docs"] = "ドキュメント",
            ["menu.help.about"] = "Arcanaについて",
            ["quick.home"] = "ホーム",
            ["common.all.files"] = "すべてのファイル",
            ["common.ok"] = "OK",
            ["app.version"] = "1.0.0",
            ["app.about.content"] = "Arcana 企業管理システム\nバージョン: {0}\n\n© 2024 Arcana Software"
        });
    }

    protected override void RegisterContributions(IPluginContext context)
    {
        // File Menu
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.file",
                Title = L("menu.file"),
                Location = MenuLocation.MainMenu,
                Order = 1
            },
            new MenuItemDefinition
            {
                Id = "menu.file.new",
                Title = L("menu.file.new"),
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
                Title = L("menu.file.open"),
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
                Title = L("menu.file.save"),
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
                Title = L("menu.file.exit"),
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
                Title = L("menu.edit"),
                Location = MenuLocation.MainMenu,
                Order = 2
            },
            new MenuItemDefinition
            {
                Id = "menu.edit.undo",
                Title = L("menu.edit.undo"),
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
                Title = L("menu.edit.redo"),
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
                Title = L("menu.edit.cut"),
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
                Title = L("menu.edit.copy"),
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
                Title = L("menu.edit.paste"),
                Location = MenuLocation.EditMenu,
                ParentId = "menu.edit",
                Icon = "\uE77F",
                Shortcut = "Ctrl+V",
                Order = 13,
                Command = "edit.paste"
            }
        );

        // Business Menu - parent for module plugins
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.business",
                Title = L("menu.business"),
                Location = MenuLocation.MainMenu,
                Order = 3
            }
        );

        // View Menu
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "menu.view",
                Title = L("menu.view"),
                Location = MenuLocation.MainMenu,
                Order = 4
            },
            new MenuItemDefinition
            {
                Id = "menu.view.refresh",
                Title = L("menu.view.refresh"),
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
                Title = L("menu.view.fullscreen"),
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
                Title = L("menu.tools"),
                Location = MenuLocation.MainMenu,
                Order = 5
            },
            new MenuItemDefinition
            {
                Id = "menu.tools.settings",
                Title = L("menu.tools.settings"),
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
                Title = L("menu.help"),
                Location = MenuLocation.MainMenu,
                Order = 99
            },
            new MenuItemDefinition
            {
                Id = "menu.help.docs",
                Title = L("menu.help.docs"),
                Location = MenuLocation.HelpMenu,
                ParentId = "menu.help",
                Icon = "\uE7BC",
                Order = 1,
                Command = "help.docs"
            },
            new MenuItemDefinition
            {
                Id = "menu.help.about",
                Title = L("menu.help.about"),
                Location = MenuLocation.HelpMenu,
                ParentId = "menu.help",
                Order = 99,
                Command = "app.about"
            }
        );

        // Register commands
        RegisterCommand("file.new", () =>
        {
            Context!.Navigation.NavigateToAsync("HomePage");
            return Task.CompletedTask;
        });

        RegisterCommand("file.open", async () =>
        {
            var files = await Context!.Window.ShowOpenFileDialogAsync(new FileDialogOptions
            {
                Title = L("menu.file.open"),
                Filters = [new FileFilter(L("common.all.files"), "*")]
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
            var version = L("app.version");
            var content = L("app.about.content", version);
            await Context!.Window.ShowInfoAsync(content, L("common.ok"));
        });

        RegisterCommand("help.docs", async () =>
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://docs.arcana.app"));
        });

        RegisterCommand("view.refresh", () =>
        {
            return Task.CompletedTask;
        });

        RegisterCommand("view.fullscreen", () =>
        {
            return Task.CompletedTask;
        });

        // Edit commands (placeholder)
        RegisterCommand("edit.undo", () => Task.CompletedTask);
        RegisterCommand("edit.redo", () => Task.CompletedTask);
        RegisterCommand("edit.cut", () => Task.CompletedTask);
        RegisterCommand("edit.copy", () => Task.CompletedTask);
        RegisterCommand("edit.paste", () => Task.CompletedTask);

        // Quick Access - Home
        RegisterMenuItems(
            new MenuItemDefinition
            {
                Id = "quick.home",
                Title = L("quick.home"),
                Location = MenuLocation.QuickAccess,
                Icon = "\uE80F",
                Order = 99,
                Group = "navigation",
                Command = "quick.home"
            }
        );

        RegisterCommand("quick.home", () =>
        {
            Context!.Navigation.NavigateToAsync("HomePage");
            return Task.CompletedTask;
        });

        // Subscribe to culture change to rebuild menus
        Context!.Localization.CultureChanged += OnCultureChanged;
        Context.Subscriptions.Add(new CultureChangeSubscription(Context.Localization, OnCultureChanged));

        LogInfo("Core menu plugin activated");
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        // Rebuild menus when language changes
        // This will be handled by the MainWindow subscribing to MenusChanged event
        LogInfo("Culture changed from {0} to {1}", e.OldCulture.Name, e.NewCulture.Name);
    }
}

internal class CultureChangeSubscription : IDisposable
{
    private readonly ILocalizationService _localization;
    private readonly EventHandler<CultureChangedEventArgs> _handler;

    public CultureChangeSubscription(ILocalizationService localization, EventHandler<CultureChangedEventArgs> handler)
    {
        _localization = localization;
        _handler = handler;
    }

    public void Dispose()
    {
        _localization.CultureChanged -= _handler;
    }
}
