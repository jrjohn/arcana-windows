using Arcana.Plugins.Contracts.Manifest;
using FluentAssertions;
using Xunit;

namespace Arcana.Plugins.Tests.Manifest;

public class PluginManifestTests
{
    [Fact]
    public void PluginManifest_Properties_ShouldBeSetCorrectly()
    {
        var manifest = new PluginManifest
        {
            Id = "arcana.module.order",
            Name = "Order Module",
            Version = "1.0.0",
            Description = "Manages orders",
            Author = "Arcana",
            Main = "OrderModule.dll",
            PluginClass = "Arcana.Order.OrderPlugin",
            Type = "Module",
            Dependencies = ["arcana.core"],
            ActivationEvents = ["onStartup"],
            Icon = "icons/order.png",
            L10n = new Dictionary<string, string> { ["en"] = "locales/en.json" }
        };

        manifest.Id.Should().Be("arcana.module.order");
        manifest.Name.Should().Be("Order Module");
        manifest.Version.Should().Be("1.0.0");
        manifest.Description.Should().Be("Manages orders");
        manifest.Author.Should().Be("Arcana");
        manifest.Main.Should().Be("OrderModule.dll");
        manifest.PluginClass.Should().Be("Arcana.Order.OrderPlugin");
        manifest.Type.Should().Be("Module");
        manifest.Dependencies.Should().ContainSingle().Which.Should().Be("arcana.core");
        manifest.ActivationEvents.Should().ContainSingle().Which.Should().Be("onStartup");
        manifest.Icon.Should().Be("icons/order.png");
        manifest.L10n.Should().ContainKey("en");
    }

    [Fact]
    public void PluginManifest_DefaultType_ShouldBeModule()
    {
        var manifest = new PluginManifest { Id = "test", Name = "Test", Version = "1.0.0" };
        manifest.Type.Should().Be("Module");
    }

    [Fact]
    public void ManifestContributions_Properties_ShouldBeSetCorrectly()
    {
        var contrib = new ManifestContributions
        {
            Views = [new ManifestViewDefinition { Id = "view1", TitleKey = "title.key" }],
            Menus = [new ManifestMenuDefinition { Id = "menu1", Location = "MainMenu" }],
            Commands = [new ManifestCommandDefinition { Id = "cmd1" }],
            Toolbars = [new ManifestToolbarDefinition { Id = "toolbar1" }],
            Keybindings = [new ManifestKeybindingDefinition { Command = "cmd1", Key = "Ctrl+S" }],
            Configuration = new ManifestConfigurationDefinition { Title = "Settings" }
        };

        contrib.Views.Should().HaveCount(1);
        contrib.Menus.Should().HaveCount(1);
        contrib.Commands.Should().HaveCount(1);
        contrib.Toolbars.Should().HaveCount(1);
        contrib.Keybindings.Should().HaveCount(1);
        contrib.Configuration.Should().NotBeNull();
    }

    [Fact]
    public void ManifestViewDefinition_Properties_ShouldBeSetCorrectly()
    {
        var view = new ManifestViewDefinition
        {
            Id = "view.orders",
            TitleKey = "views.orders.title",
            Title = "Orders",
            Icon = "icons/list.png",
            Type = "Page",
            ViewClass = "Arcana.Order.Views.OrderListView",
            ViewModelClass = "Arcana.Order.ViewModels.OrderListViewModel",
            Category = "Operations",
            CategoryKey = "category.operations",
            Order = 10,
            CanHaveMultipleInstances = true,
            ModuleId = "arcana.module.order",
            IsModuleDefaultTab = true,
            ModuleTabOrder = 1
        };

        view.Id.Should().Be("view.orders");
        view.TitleKey.Should().Be("views.orders.title");
        view.Title.Should().Be("Orders");
        view.Type.Should().Be("Page");
        view.Order.Should().Be(10);
        view.CanHaveMultipleInstances.Should().BeTrue();
        view.IsModuleDefaultTab.Should().BeTrue();
        view.ModuleTabOrder.Should().Be(1);
    }

    [Fact]
    public void ManifestViewDefinition_DefaultType_ShouldBePage()
    {
        var view = new ManifestViewDefinition { Id = "v", TitleKey = "k" };
        view.Type.Should().Be("Page");
    }

    [Fact]
    public void ManifestMenuDefinition_Properties_ShouldBeSetCorrectly()
    {
        var menu = new ManifestMenuDefinition
        {
            Id = "menu.file.open",
            Location = "FileMenu",
            TitleKey = "menu.open",
            Title = "Open",
            ParentId = "menu.file",
            Icon = "icons/open.png",
            Shortcut = "Ctrl+O",
            Command = "file.open",
            Order = 5,
            Group = "file",
            When = "workspaceOpened",
            IsSeparator = false,
            ModuleId = "arcana.module.core",
            Children = []
        };

        menu.Id.Should().Be("menu.file.open");
        menu.Location.Should().Be("FileMenu");
        menu.Command.Should().Be("file.open");
        menu.Order.Should().Be(5);
        menu.IsSeparator.Should().BeFalse();
        menu.Children.Should().BeEmpty();
    }

    [Fact]
    public void ManifestCommandDefinition_Properties_ShouldBeSetCorrectly()
    {
        var cmd = new ManifestCommandDefinition
        {
            Id = "file.open",
            TitleKey = "cmd.open",
            Title = "Open File",
            Category = "File",
            Icon = "icons/open.png",
            Enablement = "workspaceOpened"
        };

        cmd.Id.Should().Be("file.open");
        cmd.Title.Should().Be("Open File");
        cmd.Category.Should().Be("File");
        cmd.Enablement.Should().Be("workspaceOpened");
    }

    [Fact]
    public void ManifestToolbarDefinition_Properties_ShouldBeSetCorrectly()
    {
        var toolbar = new ManifestToolbarDefinition
        {
            Id = "toolbar.main",
            TitleKey = "toolbar.main.title",
            Title = "Main Toolbar",
            Items =
            [
                new ManifestToolbarItemDefinition
                {
                    Command = "file.save",
                    Icon = "icons/save.png",
                    When = "documentOpen",
                    Group = "file"
                }
            ]
        };

        toolbar.Id.Should().Be("toolbar.main");
        toolbar.Items.Should().HaveCount(1);
        toolbar.Items![0].Command.Should().Be("file.save");
        toolbar.Items[0].Group.Should().Be("file");
    }

    [Fact]
    public void ManifestKeybindingDefinition_Properties_ShouldBeSetCorrectly()
    {
        var kb = new ManifestKeybindingDefinition
        {
            Command = "file.save",
            Key = "Ctrl+S",
            When = "documentOpen",
            Mac = "Cmd+S",
            Win = "Ctrl+S",
            Linux = "Ctrl+S"
        };

        kb.Command.Should().Be("file.save");
        kb.Key.Should().Be("Ctrl+S");
        kb.Mac.Should().Be("Cmd+S");
        kb.Win.Should().Be("Ctrl+S");
        kb.Linux.Should().Be("Ctrl+S");
    }

    [Fact]
    public void ManifestConfigurationDefinition_Properties_ShouldBeSetCorrectly()
    {
        var config = new ManifestConfigurationDefinition
        {
            Title = "Plugin Settings",
            TitleKey = "settings.title",
            Properties = new Dictionary<string, ManifestConfigurationProperty>
            {
                ["maxItems"] = new ManifestConfigurationProperty
                {
                    Type = "number",
                    Default = 100,
                    Description = "Max items",
                    DescriptionKey = "settings.maxItems.desc",
                    Minimum = 1,
                    Maximum = 1000
                },
                ["theme"] = new ManifestConfigurationProperty
                {
                    Type = "string",
                    Default = "light",
                    Enum = [new object()],
                    EnumDescriptions = ["Light theme"]
                }
            }
        };

        config.Title.Should().Be("Plugin Settings");
        config.Properties.Should().HaveCount(2);
        config.Properties!["maxItems"].Type.Should().Be("number");
        config.Properties["maxItems"].Minimum.Should().Be(1);
        config.Properties["maxItems"].Maximum.Should().Be(1000);
        config.Properties["theme"].EnumDescriptions.Should().ContainSingle();
    }

    [Fact]
    public void PluginManifest_WithContributes_ShouldSetCorrectly()
    {
        var manifest = new PluginManifest
        {
            Id = "test",
            Name = "Test",
            Version = "1.0.0",
            Contributes = new ManifestContributions
            {
                Views = [new ManifestViewDefinition { Id = "v1", TitleKey = "k1" }]
            }
        };

        manifest.Contributes.Should().NotBeNull();
        manifest.Contributes!.Views.Should().HaveCount(1);
    }
}
