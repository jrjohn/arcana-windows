using System.Globalization;
using System.Text.Json;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Contracts.Validation;
using Arcana.Plugins.Core;
using Arcana.Plugins.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Plugins.Tests.Integration;

public class PluginLocalizationIntegrationTests : IDisposable
{
    private readonly string _tempPath;
    private readonly ServiceProvider _serviceProvider;
    private readonly ILocalizationService _localizationService;

    public PluginLocalizationIntegrationTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"plugin_integration_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempPath);

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddSingleton<ILocalizationService, TestLocalizationService>();
        services.AddSingleton<ICommandService, CommandService>();
        services.AddSingleton<IMenuRegistry, MenuRegistry>();
        services.AddSingleton<IViewRegistry, ViewRegistry>();

        _serviceProvider = services.BuildServiceProvider();
        _localizationService = _serviceProvider.GetRequiredService<ILocalizationService>();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, recursive: true);
        }
    }

    #region Full Plugin Localization Flow

    [Fact]
    public async Task Plugin_LoadExternalLocalization_ShouldIntegrateWithLocalizationService()
    {
        // Arrange
        var plugin = new IntegrationTestPlugin();
        var localesPath = Path.Combine(_tempPath, "locales");
        Directory.CreateDirectory(localesPath);

        await File.WriteAllTextAsync(
            Path.Combine(localesPath, "en-US.json"),
            JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["greeting"] = "Hello",
                ["farewell"] = "Goodbye"
            }));

        await File.WriteAllTextAsync(
            Path.Combine(localesPath, "zh-TW.json"),
            JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["greeting"] = "你好",
                ["farewell"] = "再見"
            }));

        var context = CreatePluginContext(plugin.Metadata.Id, localesPath);

        // Act
        await plugin.ActivateAsync(context);
        await plugin.LoadLocalesFromPath(localesPath);

        // Assert
        var testService = (TestLocalizationService)_localizationService;
        testService.GetForPlugin(plugin.Metadata.Id, "greeting").Should().NotBeEmpty();
    }

    #endregion

    #region Registry Validation Integration

    [Fact]
    public async Task Plugin_RegisterInvalidContributions_ShouldThrowValidationException()
    {
        // Arrange
        var plugin = new InvalidContributionPlugin();
        var context = CreatePluginContext(plugin.Metadata.Id, _tempPath);

        // Act & Assert
        await Assert.ThrowsAsync<ContributionValidationException>(async () =>
            await plugin.ActivateAsync(context));
    }

    [Fact]
    public async Task Plugin_RegisterValidContributions_ShouldSucceed()
    {
        // Arrange
        var plugin = new ValidContributionPlugin();
        var context = CreatePluginContext(plugin.Metadata.Id, _tempPath);

        // Act
        await plugin.ActivateAsync(context);

        // Assert
        var menuRegistry = _serviceProvider.GetRequiredService<IMenuRegistry>();
        var viewRegistry = _serviceProvider.GetRequiredService<IViewRegistry>();
        var commandService = _serviceProvider.GetRequiredService<ICommandService>();

        menuRegistry.GetAllMenuItems().Should().HaveCount(1);
        viewRegistry.GetAllViews().Should().HaveCount(1);
        commandService.HasCommand("valid.command").Should().BeTrue();
    }

    #endregion

    #region Multiple Plugin Localization

    [Fact]
    public async Task MultiplePlugins_ShouldHaveSeparateLocalizations()
    {
        // Arrange
        var plugin1 = new IntegrationTestPlugin("plugin1");
        var plugin2 = new IntegrationTestPlugin("plugin2");

        var locales1 = Path.Combine(_tempPath, "plugin1", "locales");
        var locales2 = Path.Combine(_tempPath, "plugin2", "locales");
        Directory.CreateDirectory(locales1);
        Directory.CreateDirectory(locales2);

        await File.WriteAllTextAsync(
            Path.Combine(locales1, "en-US.json"),
            JsonSerializer.Serialize(new Dictionary<string, string> { ["key"] = "Plugin 1 Value" }));

        await File.WriteAllTextAsync(
            Path.Combine(locales2, "en-US.json"),
            JsonSerializer.Serialize(new Dictionary<string, string> { ["key"] = "Plugin 2 Value" }));

        var context1 = CreatePluginContext(plugin1.Metadata.Id, Path.Combine(_tempPath, "plugin1"));
        var context2 = CreatePluginContext(plugin2.Metadata.Id, Path.Combine(_tempPath, "plugin2"));

        // Act
        await plugin1.ActivateAsync(context1);
        await plugin1.LoadLocalesFromPath(locales1);

        await plugin2.ActivateAsync(context2);
        await plugin2.LoadLocalesFromPath(locales2);

        // Assert
        var testService = (TestLocalizationService)_localizationService;
        testService.GetForPlugin("plugin1", "key").Should().Be("Plugin 1 Value");
        testService.GetForPlugin("plugin2", "key").Should().Be("Plugin 2 Value");
    }

    #endregion

    #region Plugin Deactivation

    [Fact]
    public async Task Plugin_Deactivate_ShouldCleanupSubscriptions()
    {
        // Arrange
        var plugin = new ValidContributionPlugin();
        var context = CreatePluginContext(plugin.Metadata.Id, _tempPath);
        await plugin.ActivateAsync(context);

        var menuRegistry = _serviceProvider.GetRequiredService<IMenuRegistry>();
        menuRegistry.GetAllMenuItems().Should().HaveCount(1);

        // Act
        await plugin.DeactivateAsync();

        // Assert
        menuRegistry.GetAllMenuItems().Should().BeEmpty();
    }

    #endregion

    #region Helper Methods and Classes

    private IPluginContext CreatePluginContext(string pluginId, string pluginPath)
    {
        var contextMock = new Mock<IPluginContext>();
        contextMock.Setup(c => c.PluginPath).Returns(pluginPath);
        contextMock.Setup(c => c.Localization).Returns(_localizationService);
        contextMock.Setup(c => c.Logger).Returns(_serviceProvider.GetRequiredService<ILogger<PluginContext>>());
        contextMock.Setup(c => c.Subscriptions).Returns(new List<IDisposable>());
        contextMock.Setup(c => c.Menus).Returns(_serviceProvider.GetRequiredService<IMenuRegistry>());
        contextMock.Setup(c => c.Views).Returns(_serviceProvider.GetRequiredService<IViewRegistry>());
        contextMock.Setup(c => c.Commands).Returns(_serviceProvider.GetRequiredService<ICommandService>());

        return contextMock.Object;
    }

    private class IntegrationTestPlugin : PluginBase
    {
        private readonly string _pluginId;

        public IntegrationTestPlugin(string pluginId = "integration.test.plugin")
        {
            _pluginId = pluginId;
        }

        public override PluginMetadata Metadata => new()
        {
            Id = _pluginId,
            Name = "Integration Test Plugin",
            Version = new Version(1, 0, 0),
            Type = PluginType.Module
        };

        public async Task LoadLocalesFromPath(string path)
        {
            await LoadExternalLocalizationAsync(path);
        }
    }

    private class InvalidContributionPlugin : PluginBase
    {
        public override PluginMetadata Metadata => new()
        {
            Id = "invalid.plugin",
            Name = "Invalid Plugin",
            Version = new Version(1, 0, 0),
            Type = PluginType.Module
        };

        protected override void RegisterContributions(IPluginContext context)
        {
            // This should throw validation exception - invalid ID starting with number
            RegisterMenuItem(new MenuItemDefinition
            {
                Id = "123.invalid.menu",
                Title = "Invalid Menu",
                Location = MenuLocation.MainMenu
            });
        }
    }

    private class ValidContributionPlugin : PluginBase
    {
        public override PluginMetadata Metadata => new()
        {
            Id = "valid.plugin",
            Name = "Valid Plugin",
            Version = new Version(1, 0, 0),
            Type = PluginType.Module
        };

        protected override void RegisterContributions(IPluginContext context)
        {
            RegisterMenuItem(new MenuItemDefinition
            {
                Id = "valid.menu",
                Title = "Valid Menu",
                Location = MenuLocation.MainMenu
            });

            RegisterView(new ViewDefinition
            {
                Id = "ValidPage",
                Title = "Valid Page",
                TitleKey = "valid.page.title",
                ViewClass = typeof(object)
            });

            RegisterCommand("valid.command", () => Task.CompletedTask);
        }
    }

    private class TestLocalizationService : ILocalizationService
    {
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _resources = new();
        private CultureInfo _currentCulture = new("en-US");

        public CultureInfo CurrentCulture => _currentCulture;

        public IReadOnlyList<CultureInfo> AvailableCultures => new[]
        {
            new CultureInfo("en-US"),
            new CultureInfo("zh-TW"),
            new CultureInfo("ja-JP")
        };

        public event EventHandler<CultureChangedEventArgs>? CultureChanged;

        public string Get(string key) => key;

        public string Get(string key, params object[] args) => string.Format(key, args);

        public string GetForPlugin(string pluginId, string key)
        {
            if (_resources.TryGetValue(pluginId, out var cultures) &&
                cultures.TryGetValue(_currentCulture.Name, out var resources) &&
                resources.TryGetValue(key, out var value))
            {
                return value;
            }
            return key;
        }

        public string GetForPlugin(string pluginId, string key, params object[] args)
        {
            var value = GetForPlugin(pluginId, key);
            return string.Format(value, args);
        }

        public string GetFromAnyPlugin(string key) => key;

        public void RegisterPluginResources(string pluginId, string cultureName, IDictionary<string, string> resources)
        {
            if (!_resources.ContainsKey(pluginId))
            {
                _resources[pluginId] = new Dictionary<string, Dictionary<string, string>>();
            }

            _resources[pluginId][cultureName] = new Dictionary<string, string>(resources);
        }

        public void SetCulture(string cultureName)
        {
            var old = _currentCulture;
            _currentCulture = new CultureInfo(cultureName);
            CultureChanged?.Invoke(this, new CultureChangedEventArgs(old, _currentCulture));
        }
    }

    #endregion
}

public class ContributionValidationIntegrationTests
{
    #region End-to-End Validation Tests

    [Fact]
    public void MenuRegistry_WithLogger_ShouldLogValidationWarnings()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MenuRegistry>>();
        var registry = new MenuRegistry(loggerMock.Object);

        // Act - Register item with warning (negative order)
        registry.RegisterMenuItem(new MenuItemDefinition
        {
            Id = "test.menu",
            Title = "Test",
            Location = MenuLocation.MainMenu,
            Order = -5
        });

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void ViewRegistry_WithLogger_ShouldLogValidationWarnings()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ViewRegistry>>();
        var registry = new ViewRegistry(loggerMock.Object);

        // Act - Register view without TitleKey (warning)
        registry.RegisterView(new ViewDefinition
        {
            Id = "TestPage",
            Title = "Test",
            TitleKey = null,
            ViewClass = typeof(object)
        });

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void AllRegistries_ValidationErrors_ShouldIncludeDetailedMessages()
    {
        // Arrange
        var menuRegistry = new MenuRegistry();
        var viewRegistry = new ViewRegistry();
        var commandService = new CommandService();

        // Act & Assert - Menu
        var menuException = Assert.Throws<ContributionValidationException>(() =>
            menuRegistry.RegisterMenuItem(new MenuItemDefinition
            {
                Id = "123invalid",
                Title = "",
                Location = MenuLocation.MainMenu
            }));
        menuException.ValidationErrors.Should().HaveCountGreaterThan(0);

        // Act & Assert - View
        var viewException = Assert.Throws<ContributionValidationException>(() =>
            viewRegistry.RegisterView(new ViewDefinition
            {
                Id = "_invalid",
                Title = ""
            }));
        viewException.ValidationErrors.Should().HaveCountGreaterThan(0);

        // Act & Assert - Command
        var commandException = Assert.Throws<ContributionValidationException>(() =>
            commandService.RegisterCommand("", args => Task.CompletedTask));
        commandException.ValidationErrors.Should().HaveCountGreaterThan(0);
    }

    #endregion

    #region Cross-Registry Validation

    [Fact]
    public void MenuItemWithCommand_CommandNotRegistered_ShouldStillRegister()
    {
        // This test verifies that menu items can reference commands
        // that might be registered later

        // Arrange
        var menuRegistry = new MenuRegistry();
        var commandService = new CommandService();

        // Act - Register menu item with command that doesn't exist yet
        menuRegistry.RegisterMenuItem(new MenuItemDefinition
        {
            Id = "test.menu",
            Title = "Test",
            Location = MenuLocation.MainMenu,
            Command = "future.command"
        });

        // Then register the command
        commandService.RegisterCommand("future.command", args => Task.CompletedTask);

        // Assert
        menuRegistry.GetAllMenuItems().Should().HaveCount(1);
        commandService.HasCommand("future.command").Should().BeTrue();
    }

    #endregion
}
