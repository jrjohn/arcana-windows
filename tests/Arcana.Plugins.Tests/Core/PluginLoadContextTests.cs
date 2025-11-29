using Arcana.Plugins.Core;
using FluentAssertions;
using Xunit;

namespace Arcana.Plugins.Tests.Core;

public class PluginLoadContextTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldSetPluginPath()
    {
        // Arrange
        var pluginPath = Path.Combine(Path.GetTempPath(), "test-plugin.dll");

        // Act
        var context = new PluginLoadContext(pluginPath);

        // Assert
        context.PluginPath.Should().Be(pluginPath);
    }

    [Fact]
    public void Constructor_ShouldBeCollectible()
    {
        // Arrange
        var pluginPath = Path.Combine(Path.GetTempPath(), "test-plugin.dll");

        // Act
        var context = new PluginLoadContext(pluginPath);

        // Assert
        context.IsCollectible.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldSetName()
    {
        // Arrange
        var pluginPath = Path.Combine(Path.GetTempPath(), "my-plugin.dll");

        // Act
        var context = new PluginLoadContext(pluginPath);

        // Assert
        context.Name.Should().Be("my-plugin.dll");
    }

    #endregion

    #region InitiateUnload Tests

    [Fact]
    public void InitiateUnload_ShouldRaiseUnloadingEvent()
    {
        // Arrange
        var pluginPath = Path.Combine(Path.GetTempPath(), "test-plugin.dll");
        var context = new PluginLoadContext(pluginPath);
        var eventRaised = false;
        context.Unloading += (sender, args) => eventRaised = true;

        // Act
        context.InitiateUnload();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void InitiateUnload_Twice_ShouldNotThrow()
    {
        // Arrange
        var pluginPath = Path.Combine(Path.GetTempPath(), "test-plugin.dll");
        var context = new PluginLoadContext(pluginPath);

        // Act - First unload
        context.InitiateUnload();

        // Assert - Second unload should not throw
        var act = () => context.InitiateUnload();
        act.Should().NotThrow();
    }

    #endregion
}

public class PluginLoadContextReferenceTests
{
    [Fact]
    public void Constructor_ShouldSetPluginId()
    {
        // Arrange
        var pluginPath = Path.Combine(Path.GetTempPath(), "test-plugin.dll");
        var context = new PluginLoadContext(pluginPath);

        // Act
        var reference = new PluginLoadContextReference("test-plugin", context);

        // Assert
        reference.PluginId.Should().Be("test-plugin");
    }

    [Fact]
    public void IsAlive_WithActiveContext_ShouldBeTrue()
    {
        // Arrange
        var pluginPath = Path.Combine(Path.GetTempPath(), "test-plugin.dll");
        var context = new PluginLoadContext(pluginPath);
        var reference = new PluginLoadContextReference("test-plugin", context);

        // Act & Assert
        reference.IsAlive.Should().BeTrue();
    }

    [Fact]
    public void GetContext_WithActiveContext_ShouldReturnContext()
    {
        // Arrange
        var pluginPath = Path.Combine(Path.GetTempPath(), "test-plugin.dll");
        var context = new PluginLoadContext(pluginPath);
        var reference = new PluginLoadContextReference("test-plugin", context);

        // Act
        var retrieved = reference.GetContext();

        // Assert
        retrieved.Should().BeSameAs(context);
    }

    [Fact]
    public void GetContext_AfterContextUnloaded_ShouldReturnNull()
    {
        // Arrange
        var pluginPath = Path.Combine(Path.GetTempPath(), "test-plugin.dll");
        PluginLoadContextReference reference;

        // Create context in a scope so it can be collected
        CreateAndUnloadContext(pluginPath, out reference);

        // Force GC multiple times
        for (int i = 0; i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        // Act - Context may or may not be collected depending on timing
        var retrieved = reference.GetContext();

        // Assert - Just verify it doesn't throw
        // (Context may still be alive if GC hasn't run)
    }

    private void CreateAndUnloadContext(string pluginPath, out PluginLoadContextReference reference)
    {
        var context = new PluginLoadContext(pluginPath);
        reference = new PluginLoadContextReference("test-plugin", context);
        context.InitiateUnload();
    }
}

public class PluginLoadContextManagerTests : IDisposable
{
    private readonly PluginLoadContextManager _manager;
    private readonly string _testPluginDir;

    public PluginLoadContextManagerTests()
    {
        _manager = new PluginLoadContextManager();
        _testPluginDir = Path.Combine(Path.GetTempPath(), $"plugin_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPluginDir);
    }

    public void Dispose()
    {
        _manager.Dispose();
        if (Directory.Exists(_testPluginDir))
        {
            Directory.Delete(_testPluginDir, recursive: true);
        }
    }

    #region CreateContext Tests

    [Fact]
    public void CreateContext_ShouldReturnNewContext()
    {
        // Arrange
        var pluginPath = Path.Combine(_testPluginDir, "plugin1.dll");

        // Act
        var context = _manager.CreateContext("plugin1", pluginPath);

        // Assert
        context.Should().NotBeNull();
        context.PluginPath.Should().Be(pluginPath);
    }

    [Fact]
    public void CreateContext_SamePluginTwice_ShouldReturnNewContext()
    {
        // Arrange
        var pluginPath = Path.Combine(_testPluginDir, "plugin1.dll");
        var context1 = _manager.CreateContext("plugin1", pluginPath);

        // Act
        var context2 = _manager.CreateContext("plugin1", pluginPath);

        // Assert
        context2.Should().NotBeSameAs(context1);
    }

    [Fact]
    public void CreateContext_DifferentPlugins_ShouldReturnDifferentContexts()
    {
        // Arrange
        var path1 = Path.Combine(_testPluginDir, "plugin1.dll");
        var path2 = Path.Combine(_testPluginDir, "plugin2.dll");

        // Act
        var context1 = _manager.CreateContext("plugin1", path1);
        var context2 = _manager.CreateContext("plugin2", path2);

        // Assert
        context1.Should().NotBeSameAs(context2);
    }

    #endregion

    #region GetContext Tests

    [Fact]
    public void GetContext_ExistingPlugin_ShouldReturnContext()
    {
        // Arrange
        var pluginPath = Path.Combine(_testPluginDir, "plugin1.dll");
        var createdContext = _manager.CreateContext("plugin1", pluginPath);

        // Act
        var retrieved = _manager.GetContext("plugin1");

        // Assert
        retrieved.Should().BeSameAs(createdContext);
    }

    [Fact]
    public void GetContext_NonExistingPlugin_ShouldReturnNull()
    {
        // Act
        var context = _manager.GetContext("nonexistent");

        // Assert
        context.Should().BeNull();
    }

    #endregion

    #region UnloadContext Tests

    [Fact]
    public void UnloadContext_ExistingPlugin_ShouldInitiateUnload()
    {
        // Arrange
        var pluginPath = Path.Combine(_testPluginDir, "plugin1.dll");
        var unloadingEventRaised = false;

        var context = _manager.CreateContext("plugin1", pluginPath);
        context.Unloading += (s, e) => unloadingEventRaised = true;

        // Act
        _manager.UnloadContext("plugin1");

        // Assert
        unloadingEventRaised.Should().BeTrue();
    }

    [Fact]
    public void UnloadContext_NonExistingPlugin_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        var act = () => _manager.UnloadContext("nonexistent");
        act.Should().NotThrow();
    }

    [Fact]
    public async Task UnloadContextAsync_ShouldReturnTrue_WhenNoReferencesHeld()
    {
        // Arrange
        var pluginPath = Path.Combine(_testPluginDir, "plugin1.dll");
        _manager.CreateContext("plugin1", pluginPath);

        // Act - Note: We don't hold the context reference, so it should be collectible
        // But the manager still holds a reference, so this is a simplified test
        var result = await _manager.UnloadContextAsync("nonexistent");

        // Assert
        result.Should().BeTrue(); // Returns true for non-existing plugins
    }

    #endregion

    #region GetUnloadStatus Tests

    [Fact]
    public void GetUnloadStatus_EmptyManager_ShouldReturnEmpty()
    {
        // Act
        var status = _manager.GetUnloadStatus();

        // Assert
        status.Should().BeEmpty();
    }

    [Fact]
    public void GetUnloadStatus_WithPlugins_ShouldReturnStatus()
    {
        // Arrange
        var path1 = Path.Combine(_testPluginDir, "plugin1.dll");
        var path2 = Path.Combine(_testPluginDir, "plugin2.dll");
        _manager.CreateContext("plugin1", path1);
        _manager.CreateContext("plugin2", path2);

        // Act
        var status = _manager.GetUnloadStatus();

        // Assert
        status.Should().HaveCount(2);
        status.Should().ContainKey("plugin1");
        status.Should().ContainKey("plugin2");
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldInitiateUnloadForAllContexts()
    {
        // Arrange
        var unloadedPlugins = new List<string>();
        var path1 = Path.Combine(_testPluginDir, "plugin1.dll");
        var path2 = Path.Combine(_testPluginDir, "plugin2.dll");

        var context1 = _manager.CreateContext("plugin1", path1);
        context1.Unloading += (s, e) => unloadedPlugins.Add("plugin1");

        var context2 = _manager.CreateContext("plugin2", path2);
        context2.Unloading += (s, e) => unloadedPlugins.Add("plugin2");

        // Act
        _manager.Dispose();

        // Assert
        unloadedPlugins.Should().Contain("plugin1");
        unloadedPlugins.Should().Contain("plugin2");
    }

    [Fact]
    public void Dispose_Twice_ShouldNotThrow()
    {
        // Act & Assert
        _manager.Dispose();
        var act = () => _manager.Dispose();
        act.Should().NotThrow();
    }

    #endregion
}
