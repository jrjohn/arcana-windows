using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Plugins.Tests.Core;

public class PluginBaseTests
{
    private readonly Mock<IPluginContext> _contextMock;
    private readonly Mock<CommandService> _commandServiceMock;
    private readonly Mock<IMenuRegistry> _menuRegistryMock;
    private readonly Mock<IViewRegistry> _viewRegistryMock;
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly Mock<IEventAggregator> _eventAggregatorMock;
    private readonly Mock<LocalizationService> _localizationMock;
    private readonly Mock<ILogger> _loggerMock;

    public PluginBaseTests()
    {
        _commandServiceMock = new Mock<CommandService>();
        _menuRegistryMock = new Mock<IMenuRegistry>();
        _viewRegistryMock = new Mock<IViewRegistry>();
        _messageBusMock = new Mock<IMessageBus>();
        _eventAggregatorMock = new Mock<IEventAggregator>();
        _localizationMock = new Mock<LocalizationService>();
        _loggerMock = new Mock<ILogger>();

        _contextMock = new Mock<IPluginContext>();
        _contextMock.Setup(c => c.Commands).Returns(_commandServiceMock.Object);
        _contextMock.Setup(c => c.Menus).Returns(_menuRegistryMock.Object);
        _contextMock.Setup(c => c.Views).Returns(_viewRegistryMock.Object);
        _contextMock.Setup(c => c.MessageBus).Returns(_messageBusMock.Object);
        _contextMock.Setup(c => c.Events).Returns(_eventAggregatorMock.Object);
        _contextMock.Setup(c => c.Localization).Returns(_localizationMock.Object);
        _contextMock.Setup(c => c.Logger).Returns(_loggerMock.Object);
        _contextMock.Setup(c => c.Subscriptions).Returns(new List<IDisposable>());
        _contextMock.Setup(c => c.PluginPath).Returns("/plugins/test");
    }

    // ─── State transitions ────────────────────────────────────────────────────

    [Fact]
    public void InitialState_ShouldBeNotLoaded()
    {
        var plugin = new SimpleTestPlugin();
        plugin.State.Should().Be(PluginState.NotLoaded);
    }

    [Fact]
    public async Task ActivateAsync_ShouldTransitionToActiveState()
    {
        var plugin = new SimpleTestPlugin();
        await plugin.ActivateAsync(_contextMock.Object);
        plugin.State.Should().Be(PluginState.Active);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldTransitionToDeactivatedState()
    {
        var plugin = new SimpleTestPlugin();
        await plugin.ActivateAsync(_contextMock.Object);
        await plugin.DeactivateAsync();
        plugin.State.Should().Be(PluginState.Deactivated);
    }

    [Fact]
    public async Task ActivateAsync_WhenActivationThrows_ShouldTransitionToErrorState()
    {
        var plugin = new ThrowOnActivatePlugin();

        await Assert.ThrowsAsync<InvalidOperationException>(() => plugin.ActivateAsync(_contextMock.Object));
        plugin.State.Should().Be(PluginState.Error);
    }

    [Fact]
    public async Task DeactivateAsync_WhenDeactivationThrows_ShouldTransitionToErrorState()
    {
        var plugin = new ThrowOnDeactivatePlugin();
        await plugin.ActivateAsync(_contextMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => plugin.DeactivateAsync());
        plugin.State.Should().Be(PluginState.Error);
    }

    [Fact]
    public async Task ActivateAsync_ShouldCallOnActivateAsync()
    {
        var plugin = new TrackingPlugin();
        await plugin.ActivateAsync(_contextMock.Object);
        plugin.OnActivateCalled.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAsync_ShouldCallOnDeactivateAsync()
    {
        var plugin = new TrackingPlugin();
        await plugin.ActivateAsync(_contextMock.Object);
        await plugin.DeactivateAsync();
        plugin.OnDeactivateCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateAsync_ShouldCallRegisterContributions()
    {
        var plugin = new TrackingPlugin();
        await plugin.ActivateAsync(_contextMock.Object);
        plugin.RegisterContributionsCalled.Should().BeTrue();
    }

    // ─── Deactivate disposes subscriptions ───────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_ShouldDisposeAllSubscriptions()
    {
        var plugin = new SimpleTestPlugin();
        var sub1 = new Mock<IDisposable>();
        var sub2 = new Mock<IDisposable>();

        await plugin.ActivateAsync(_contextMock.Object);
        _contextMock.Object.Subscriptions.Add(sub1.Object);
        _contextMock.Object.Subscriptions.Add(sub2.Object);

        await plugin.DeactivateAsync();

        sub1.Verify(s => s.Dispose(), Times.Once);
        sub2.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_ShouldClearSubscriptions()
    {
        var plugin = new SimpleTestPlugin();
        var sub = new Mock<IDisposable>();

        await plugin.ActivateAsync(_contextMock.Object);
        _contextMock.Object.Subscriptions.Add(sub.Object);

        await plugin.DeactivateAsync();

        _contextMock.Object.Subscriptions.Should().BeEmpty();
    }

    // ─── ConfigureServices ────────────────────────────────────────────────────

    [Fact]
    public void ConfigureServices_ShouldNotThrow()
    {
        var plugin = new SimpleTestPlugin();
        var services = new ServiceCollection();
        Action act = () => plugin.ConfigureServices(services);
        act.Should().NotThrow();
    }

    // ─── DisposeAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DisposeAsync_WhenActive_ShouldDeactivate()
    {
        var plugin = new SimpleTestPlugin();
        await plugin.ActivateAsync(_contextMock.Object);
        await plugin.DisposeAsync();
        plugin.State.Should().Be(PluginState.Deactivated);
    }

    [Fact]
    public async Task DisposeAsync_WhenNotActive_ShouldNotCallDeactivate()
    {
        var plugin = new TrackingPlugin();
        // Don't activate – just dispose
        await plugin.DisposeAsync();
        plugin.OnDeactivateCalled.Should().BeFalse();
    }

    // ─── RegisterCommand helper ───────────────────────────────────────────────

    [Fact]
    public async Task RegisterCommand_ShouldCallCommandServiceAndAddToSubscriptions()
    {
        var plugin = new CommandRegisteringPlugin();
        var disposable = new Mock<IDisposable>().Object;

        _commandServiceMock
            .Setup(cs => cs.RegisterCommand(It.IsAny<string>(), It.IsAny<Func<object?[], Task>>()))
            .Returns(disposable);

        await plugin.ActivateAsync(_contextMock.Object);

        _commandServiceMock.Verify(cs => cs.RegisterCommand(
            It.IsAny<string>(),
            It.IsAny<Func<object?[], Task>>()), Times.AtLeastOnce);
    }

    // ─── Subscribe / SubscribeToEvent ────────────────────────────────────────

    [Fact]
    public async Task Subscribe_ShouldAddToSubscriptions()
    {
        var plugin = new MessageSubscribingPlugin();
        var disposable = new Mock<IDisposable>().Object;

        _messageBusMock
            .Setup(mb => mb.Subscribe(It.IsAny<Action<TestMessage>>()))
            .Returns(disposable);

        await plugin.ActivateAsync(_contextMock.Object);

        _contextMock.Object.Subscriptions.Should().Contain(disposable);
    }

    [Fact]
    public async Task SubscribeToEvent_ShouldAddToSubscriptions()
    {
        var plugin = new EventSubscribingPlugin();
        var disposable = new Mock<IDisposable>().Object;

        _eventAggregatorMock
            .Setup(ea => ea.Subscribe(It.IsAny<Action<TestEvent>>()))
            .Returns(disposable);

        await plugin.ActivateAsync(_contextMock.Object);

        _contextMock.Object.Subscriptions.Should().Contain(disposable);
    }

    // ─── PublishAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_ShouldCallMessageBus()
    {
        var plugin = new MessagePublishingPlugin();
        _messageBusMock.Setup(mb => mb.PublishAsync(It.IsAny<TestMessage>()))
            .Returns(Task.CompletedTask);

        await plugin.ActivateAsync(_contextMock.Object);
        await plugin.DoPublishAsync();

        _messageBusMock.Verify(mb => mb.PublishAsync(It.IsAny<TestMessage>()), Times.Once);
    }

    // ─── Log helpers ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LogInfo_ShouldCallLoggerLogInformation()
    {
        var plugin = new LoggingPlugin();
        await plugin.ActivateAsync(_contextMock.Object);
        plugin.TestLogInfo("test message");

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task LogWarning_ShouldCallLoggerLogWarning()
    {
        var plugin = new LoggingPlugin();
        await plugin.ActivateAsync(_contextMock.Object);
        plugin.TestLogWarning("warn message");

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task LogError_ShouldCallLoggerLogError()
    {
        var plugin = new LoggingPlugin();
        await plugin.ActivateAsync(_contextMock.Object);
        plugin.TestLogError(new Exception("test"), "error message");

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    // ─── Metadata ─────────────────────────────────────────────────────────────

    [Fact]
    public void Metadata_ShouldReturnCorrectValues()
    {
        var plugin = new SimpleTestPlugin();
        plugin.Metadata.Id.Should().Be("test.simple");
        plugin.Metadata.Name.Should().Be("Simple Test Plugin");
        plugin.Metadata.Type.Should().Be(PluginType.Module);
    }

    // ─── Helpers: Test Plugin Implementations ────────────────────────────────

    private static PluginMetadata CreateMetadata(string id = "test.simple", string name = "Simple Test Plugin") =>
        new PluginMetadata { Id = id, Name = name, Version = new Version(1, 0, 0), Type = PluginType.Module };

    private class SimpleTestPlugin : PluginBase
    {
        public override PluginMetadata Metadata => CreateMetadata();
    }

    private class TrackingPlugin : PluginBase
    {
        public bool OnActivateCalled { get; private set; }
        public bool OnDeactivateCalled { get; private set; }
        public bool RegisterContributionsCalled { get; private set; }

        public override PluginMetadata Metadata => CreateMetadata("test.tracking", "Tracking Plugin");

        protected override Task OnActivateAsync(IPluginContext context)
        {
            OnActivateCalled = true;
            return Task.CompletedTask;
        }

        protected override Task OnDeactivateAsync()
        {
            OnDeactivateCalled = true;
            return Task.CompletedTask;
        }

        protected override void RegisterContributions(IPluginContext context)
        {
            RegisterContributionsCalled = true;
        }
    }

    private class ThrowOnActivatePlugin : PluginBase
    {
        public override PluginMetadata Metadata => CreateMetadata("test.throw-activate", "Throw On Activate");

        protected override Task OnActivateAsync(IPluginContext context)
        {
            throw new InvalidOperationException("Activation failed!");
        }
    }

    private class ThrowOnDeactivatePlugin : PluginBase
    {
        public override PluginMetadata Metadata => CreateMetadata("test.throw-deactivate", "Throw On Deactivate");

        protected override Task OnDeactivateAsync()
        {
            throw new InvalidOperationException("Deactivation failed!");
        }
    }

    private class CommandRegisteringPlugin : PluginBase
    {
        public override PluginMetadata Metadata => CreateMetadata("test.cmd", "Command Plugin");

        protected override void RegisterContributions(IPluginContext context)
        {
            RegisterCommand("test.command", () => Task.CompletedTask);
        }
    }

    private class MessageSubscribingPlugin : PluginBase
    {
        public override PluginMetadata Metadata => CreateMetadata("test.sub", "Subscribe Plugin");

        protected override void RegisterContributions(IPluginContext context)
        {
            Subscribe<TestMessage>(msg => { });
        }
    }

    private class EventSubscribingPlugin : PluginBase
    {
        public override PluginMetadata Metadata => CreateMetadata("test.evtsub", "Event Subscribe Plugin");

        protected override void RegisterContributions(IPluginContext context)
        {
            SubscribeToEvent<TestEvent>(_ => { });
        }
    }

    private class MessagePublishingPlugin : PluginBase
    {
        public override PluginMetadata Metadata => CreateMetadata("test.pub", "Publish Plugin");

        public Task DoPublishAsync() => PublishAsync(new TestMessage());
    }

    private class LoggingPlugin : PluginBase
    {
        public override PluginMetadata Metadata => CreateMetadata("test.log", "Logging Plugin");

        public void TestLogInfo(string msg) => LogInfo(msg);
        public void TestLogWarning(string msg) => LogWarning(msg);
        public void TestLogError(Exception ex, string msg) => LogError(ex, msg);
    }

    // ─── Dummy message and event types ───────────────────────────────────────

    private class TestMessage { }

    private class TestEvent : IApplicationEvent { }
}
