using Arcana.Plugins.Contracts.Manifest;
using Arcana.Plugins.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Plugins.Tests.Services;

public class ActivationEventServiceTests
{
    private readonly Mock<ManifestService> _manifestServiceMock;
    private readonly Mock<ILogger<ActivationEventService>> _loggerMock;
    private readonly ActivationEventService _service;

    public ActivationEventServiceTests()
    {
        _manifestServiceMock = new Mock<ManifestService>();
        _loggerMock = new Mock<ILogger<ActivationEventService>>();
        _service = new ActivationEventService(_manifestServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void RegisterPendingPlugin_WithEvents_ShouldBePending()
    {
        _service.RegisterPendingPlugin("plugin-a", new[] { ActivationEvents.OnStartup });

        _service.IsPendingActivation("plugin-a").Should().BeTrue();
    }

    [Fact]
    public void RegisterPendingPlugin_WithNoEvents_ShouldNotBePending()
    {
        _service.RegisterPendingPlugin("plugin-a", Array.Empty<string>());

        _service.IsPendingActivation("plugin-a").Should().BeFalse();
    }

    [Fact]
    public void IsPendingActivation_UnregisteredPlugin_ShouldReturnFalse()
    {
        _service.IsPendingActivation("nonexistent-plugin").Should().BeFalse();
    }

    [Fact]
    public void MarkActivated_ShouldRemoveFromPending()
    {
        _service.RegisterPendingPlugin("plugin-a", new[] { ActivationEvents.OnStartup });
        _service.MarkActivated("plugin-a");

        _service.IsPendingActivation("plugin-a").Should().BeFalse();
    }

    [Fact]
    public void GetPendingPlugins_ShouldReturnAllPending()
    {
        _service.RegisterPendingPlugin("plugin-a", new[] { ActivationEvents.OnStartup });
        _service.RegisterPendingPlugin("plugin-b", new[] { ActivationEvents.ForCommand("test") });

        var pending = _service.GetPendingPlugins();

        pending.Should().ContainKey("plugin-a");
        pending.Should().ContainKey("plugin-b");
    }

    [Fact]
    public void GetPendingPlugins_ShouldExcludeActivated()
    {
        _service.RegisterPendingPlugin("plugin-a", new[] { ActivationEvents.OnStartup });
        _service.RegisterPendingPlugin("plugin-b", new[] { ActivationEvents.OnStartup });
        _service.MarkActivated("plugin-a");

        var pending = _service.GetPendingPlugins();

        pending.Should().NotContainKey("plugin-a");
        pending.Should().ContainKey("plugin-b");
    }

    [Fact]
    public async Task FireAsync_StartupEvent_ShouldActivateOnStartupPlugins()
    {
        var activated = new List<string>();
        _service.RegisterPendingPlugin("plugin-a", new[] { ActivationEvents.OnStartup });
        _service.SetActivationCallback(id =>
        {
            activated.Add(id);
            return Task.CompletedTask;
        });

        await _service.FireAsync(ActivationEventType.OnStartup);

        activated.Should().Contain("plugin-a");
    }

    [Fact]
    public async Task FireAsync_StartupEvent_ShouldMarkPluginAsActivated()
    {
        _service.RegisterPendingPlugin("plugin-a", new[] { ActivationEvents.OnStartup });
        _service.SetActivationCallback(_ => Task.CompletedTask);

        await _service.FireAsync(ActivationEventType.OnStartup);

        _service.IsPendingActivation("plugin-a").Should().BeFalse();
    }

    [Fact]
    public async Task FireAsync_CommandEvent_ShouldActivateMatchingPlugin()
    {
        var activated = new List<string>();
        _service.RegisterPendingPlugin("plugin-cmd", new[] { ActivationEvents.ForCommand("order.new") });
        _service.SetActivationCallback(id =>
        {
            activated.Add(id);
            return Task.CompletedTask;
        });

        await _service.FireAsync(ActivationEventType.OnCommand, "order.new");

        activated.Should().Contain("plugin-cmd");
    }

    [Fact]
    public async Task FireAsync_CommandEvent_WrongCommand_ShouldNotActivate()
    {
        var activated = new List<string>();
        _service.RegisterPendingPlugin("plugin-cmd", new[] { ActivationEvents.ForCommand("order.new") });
        _service.SetActivationCallback(id =>
        {
            activated.Add(id);
            return Task.CompletedTask;
        });

        await _service.FireAsync(ActivationEventType.OnCommand, "customer.edit");

        activated.Should().BeEmpty();
    }

    [Fact]
    public async Task FireAsync_WildcardPlugin_ShouldActivateOnAnyEvent()
    {
        var activated = new List<string>();
        _service.RegisterPendingPlugin("wildcard-plugin", new[] { ActivationEvents.Star });
        _service.SetActivationCallback(id =>
        {
            activated.Add(id);
            return Task.CompletedTask;
        });

        await _service.FireAsync(ActivationEventType.OnStartup);

        activated.Should().Contain("wildcard-plugin");
    }

    [Fact]
    public async Task FireAsync_AlreadyActivatedPlugin_ShouldNotActivateAgain()
    {
        int callCount = 0;
        _service.RegisterPendingPlugin("plugin-a", new[] { ActivationEvents.OnStartup });
        _service.SetActivationCallback(_ =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        await _service.FireAsync(ActivationEventType.OnStartup);
        await _service.FireAsync(ActivationEventType.OnStartup);

        callCount.Should().Be(1);
    }

    [Fact]
    public async Task FireAsync_NoCallback_ShouldNotThrow()
    {
        _service.RegisterPendingPlugin("plugin-a", new[] { ActivationEvents.OnStartup });
        // No callback set

        var act = async () => await _service.FireAsync(ActivationEventType.OnStartup);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FireAsync_RaisesPluginActivatedEvent()
    {
        PluginActivatedEventArgs? capturedArgs = null;
        _service.PluginActivated += (_, args) => capturedArgs = args;
        _service.RegisterPendingPlugin("plugin-a", new[] { ActivationEvents.OnStartup });
        _service.SetActivationCallback(_ => Task.CompletedTask);

        await _service.FireAsync(ActivationEventType.OnStartup);

        capturedArgs.Should().NotBeNull();
        capturedArgs!.PluginId.Should().Be("plugin-a");
        capturedArgs.ActivationEvent.Should().Be(ActivationEvents.OnStartup);
    }

    [Fact]
    public async Task FireAsync_CallbackThrows_ShouldNotThrow()
    {
        _service.RegisterPendingPlugin("plugin-err", new[] { ActivationEvents.OnStartup });
        _service.SetActivationCallback(_ => throw new InvalidOperationException("activation failed"));

        var act = async () => await _service.FireAsync(ActivationEventType.OnStartup);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FireAsync_StringOverload_ShouldWork()
    {
        var activated = new List<string>();
        _service.RegisterPendingPlugin("plugin-a", new[] { ActivationEvents.OnStartup });
        _service.SetActivationCallback(id =>
        {
            activated.Add(id);
            return Task.CompletedTask;
        });

        await _service.FireAsync(ActivationEvents.OnStartup);

        activated.Should().Contain("plugin-a");
    }

    [Fact]
    public async Task FireAsync_ViewEvent_ShouldActivateMatchingViewPlugin()
    {
        var activated = new List<string>();
        _service.RegisterPendingPlugin("view-plugin", new[] { ActivationEvents.ForView("OrderPage") });
        _service.SetActivationCallback(id =>
        {
            activated.Add(id);
            return Task.CompletedTask;
        });

        await _service.FireAsync(ActivationEventType.OnView, "OrderPage");

        activated.Should().Contain("view-plugin");
    }

    [Fact]
    public async Task FireAsync_LanguageEvent_ShouldActivateMatchingPlugin()
    {
        var activated = new List<string>();
        _service.RegisterPendingPlugin("lang-plugin", new[] { ActivationEvents.ForLanguage("zh-TW") });
        _service.SetActivationCallback(id =>
        {
            activated.Add(id);
            return Task.CompletedTask;
        });

        await _service.FireAsync(ActivationEventType.OnLanguage, "zh-TW");

        activated.Should().Contain("lang-plugin");
    }

    [Fact]
    public void GetPluginsForEvent_ReturnsMatchingPendingPlugins()
    {
        _service.RegisterPendingPlugin("plugin-a", new[] { ActivationEvents.OnStartup });
        _service.RegisterPendingPlugin("plugin-b", new[] { ActivationEvents.ForCommand("test") });

        var plugins = _service.GetPluginsForEvent(ActivationEventType.OnStartup);

        plugins.Should().Contain("plugin-a");
        plugins.Should().NotContain("plugin-b");
    }

    [Fact]
    public void GetPluginsForEvent_ExcludesActivatedPlugins()
    {
        _service.RegisterPendingPlugin("plugin-a", new[] { ActivationEvents.OnStartup });
        _service.MarkActivated("plugin-a");

        var plugins = _service.GetPluginsForEvent(ActivationEventType.OnStartup);

        plugins.Should().NotContain("plugin-a");
    }

    [Fact]
    public async Task FireAsync_MultiplePlugins_AllShouldActivate()
    {
        var activated = new List<string>();
        _service.RegisterPendingPlugin("plugin-1", new[] { ActivationEvents.OnStartup });
        _service.RegisterPendingPlugin("plugin-2", new[] { ActivationEvents.OnStartup });
        _service.RegisterPendingPlugin("plugin-3", new[] { ActivationEvents.OnStartup });
        _service.SetActivationCallback(id =>
        {
            activated.Add(id);
            return Task.CompletedTask;
        });

        await _service.FireAsync(ActivationEventType.OnStartup);

        activated.Should().HaveCount(3);
        activated.Should().Contain("plugin-1");
        activated.Should().Contain("plugin-2");
        activated.Should().Contain("plugin-3");
    }

    [Fact]
    public async Task FireAsync_UnknownEventType_ShouldNotActivateAnyPlugin()
    {
        var activated = new List<string>();
        _service.RegisterPendingPlugin("plugin-a", new[] { ActivationEvents.OnStartup });
        _service.SetActivationCallback(id =>
        {
            activated.Add(id);
            return Task.CompletedTask;
        });

        // Fire an event that doesn't match
        await _service.FireAsync(ActivationEventType.OnCommand, "unrelated.command");

        activated.Should().BeEmpty();
    }
}
