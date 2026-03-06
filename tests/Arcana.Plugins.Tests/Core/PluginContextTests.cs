using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Plugins.Tests.Core;

public class PluginContextTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<CommandService> _commandServiceMock;
    private readonly Mock<WindowService> _windowServiceMock;
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly Mock<IEventAggregator> _eventAggregatorMock;
    private readonly Mock<ISharedStateStore> _sharedStateMock;
    private readonly Mock<IMenuRegistry> _menuRegistryMock;
    private readonly Mock<IViewRegistry> _viewRegistryMock;
    private readonly Mock<INavigationService> _navigationMock;
    private readonly Mock<INavGraph> _navGraphMock;
    private readonly Mock<LocalizationService> _localizationMock;

    public PluginContextTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerMock = new Mock<ILogger>();
        _commandServiceMock = new Mock<CommandService>();
        _windowServiceMock = new Mock<WindowService>();
        _messageBusMock = new Mock<IMessageBus>();
        _eventAggregatorMock = new Mock<IEventAggregator>();
        _sharedStateMock = new Mock<ISharedStateStore>();
        _menuRegistryMock = new Mock<IMenuRegistry>();
        _viewRegistryMock = new Mock<IViewRegistry>();
        _navigationMock = new Mock<INavigationService>();
        _navGraphMock = new Mock<INavGraph>();
        _localizationMock = new Mock<LocalizationService>();

        // Setup ILoggerFactory
        _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);

        // Setup IServiceProvider to return our mocks via GetRequiredService
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILoggerFactory))).Returns(_loggerFactoryMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(CommandService))).Returns(_commandServiceMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(WindowService))).Returns(_windowServiceMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IMessageBus))).Returns(_messageBusMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IEventAggregator))).Returns(_eventAggregatorMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ISharedStateStore))).Returns(_sharedStateMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IMenuRegistry))).Returns(_menuRegistryMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IViewRegistry))).Returns(_viewRegistryMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(INavigationService))).Returns(_navigationMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(INavGraph))).Returns(_navGraphMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(LocalizationService))).Returns(_localizationMock.Object);
    }

    private PluginContext CreateContext(
        string pluginId = "test.plugin",
        string pluginPath = "/plugins/test",
        string dataPath = "/data/test") =>
        new PluginContext(pluginId, pluginPath, dataPath, _serviceProviderMock.Object);

    [Fact]
    public void Constructor_ShouldSetPluginId()
    {
        var ctx = CreateContext(pluginId: "my.plugin");
        ctx.PluginId.Should().Be("my.plugin");
    }

    [Fact]
    public void Constructor_ShouldSetPluginPath()
    {
        var ctx = CreateContext(pluginPath: "/custom/path");
        ctx.PluginPath.Should().Be("/custom/path");
    }

    [Fact]
    public void Constructor_ShouldSetDataPath()
    {
        var ctx = CreateContext(dataPath: "/data/custom");
        ctx.DataPath.Should().Be("/data/custom");
    }

    [Fact]
    public void Constructor_ShouldResolveLogger()
    {
        var ctx = CreateContext();
        ctx.Logger.Should().NotBeNull();
        ctx.Logger.Should().BeSameAs(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldCreateLoggerWithPluginId()
    {
        CreateContext(pluginId: "my.plugin");
        _loggerFactoryMock.Verify(f => f.CreateLogger("Plugin.my.plugin"), Times.Once);
    }

    [Fact]
    public void Constructor_ShouldResolveCommandService()
    {
        var ctx = CreateContext();
        ctx.Commands.Should().BeSameAs(_commandServiceMock.Object);
    }

    [Fact]
    public void Constructor_ShouldResolveWindowService()
    {
        var ctx = CreateContext();
        ctx.Window.Should().BeSameAs(_windowServiceMock.Object);
    }

    [Fact]
    public void Constructor_ShouldResolveMessageBus()
    {
        var ctx = CreateContext();
        ctx.MessageBus.Should().BeSameAs(_messageBusMock.Object);
    }

    [Fact]
    public void Constructor_ShouldResolveEventAggregator()
    {
        var ctx = CreateContext();
        ctx.Events.Should().BeSameAs(_eventAggregatorMock.Object);
    }

    [Fact]
    public void Constructor_ShouldResolveSharedState()
    {
        var ctx = CreateContext();
        ctx.SharedState.Should().BeSameAs(_sharedStateMock.Object);
    }

    [Fact]
    public void Constructor_ShouldResolveMenuRegistry()
    {
        var ctx = CreateContext();
        ctx.Menus.Should().BeSameAs(_menuRegistryMock.Object);
    }

    [Fact]
    public void Constructor_ShouldResolveViewRegistry()
    {
        var ctx = CreateContext();
        ctx.Views.Should().BeSameAs(_viewRegistryMock.Object);
    }

    [Fact]
    public void Constructor_ShouldResolveNavigation()
    {
        var ctx = CreateContext();
        ctx.Navigation.Should().BeSameAs(_navigationMock.Object);
    }

    [Fact]
    public void Constructor_ShouldResolveNavGraph()
    {
        var ctx = CreateContext();
        ctx.NavGraph.Should().BeSameAs(_navGraphMock.Object);
    }

    [Fact]
    public void Constructor_ShouldResolveLocalization()
    {
        var ctx = CreateContext();
        ctx.Localization.Should().BeSameAs(_localizationMock.Object);
    }

    [Fact]
    public void Constructor_Subscriptions_ShouldBeEmptyInitially()
    {
        var ctx = CreateContext();
        ctx.Subscriptions.Should().BeEmpty();
    }

    [Fact]
    public void Subscriptions_ShouldBeModifiable()
    {
        var ctx = CreateContext();
        var sub = new Mock<IDisposable>();
        ctx.Subscriptions.Add(sub.Object);
        ctx.Subscriptions.Should().HaveCount(1);
    }

    [Fact]
    public void GetService_RegisteredType_ShouldReturnInstance()
    {
        var ctx = CreateContext();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IMessageBus))).Returns(_messageBusMock.Object);

        var result = ctx.GetService<IMessageBus>();

        result.Should().BeSameAs(_messageBusMock.Object);
    }

    [Fact]
    public void GetService_UnregisteredType_ShouldReturnNull()
    {
        var ctx = CreateContext();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IDisposable))).Returns((object?)null);

        var result = ctx.GetService<IDisposable>();

        result.Should().BeNull();
    }

    [Fact]
    public void GetRequiredService_RegisteredType_ShouldReturnInstance()
    {
        var ctx = CreateContext();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IMessageBus))).Returns(_messageBusMock.Object);

        var result = ctx.GetRequiredService<IMessageBus>();

        result.Should().BeSameAs(_messageBusMock.Object);
    }

    [Fact]
    public void Constructor_WithDifferentPluginIds_ShouldCreateUniqueContexts()
    {
        var ctx1 = CreateContext(pluginId: "plugin.one");
        var ctx2 = CreateContext(pluginId: "plugin.two");

        ctx1.PluginId.Should().Be("plugin.one");
        ctx2.PluginId.Should().Be("plugin.two");
    }
}
