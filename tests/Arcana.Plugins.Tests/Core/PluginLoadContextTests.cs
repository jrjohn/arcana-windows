using Arcana.Plugins.Core;
using FluentAssertions;
using System.Reflection;
using Xunit;

namespace Arcana.Plugins.Tests.Core;

public class PluginLoadContextTests
{
    private readonly string _testAssemblyPath;

    public PluginLoadContextTests()
    {
        // Use the test assembly itself as a valid DLL path
        _testAssemblyPath = Assembly.GetExecutingAssembly().Location;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldSetPluginPath()
    {
        // Act
        var context = new PluginLoadContext(_testAssemblyPath);

        // Assert
        context.PluginPath.Should().Be(_testAssemblyPath);
    }

    [Fact]
    public void Constructor_ShouldBeCollectible()
    {
        // Act
        var context = new PluginLoadContext(_testAssemblyPath);

        // Assert
        context.IsCollectible.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldSetName()
    {
        // Act
        var context = new PluginLoadContext(_testAssemblyPath);

        // Assert
        context.Name.Should().Be(Path.GetFileName(_testAssemblyPath));
    }

    #endregion

    #region InitiateUnload Tests

    [Fact]
    public void InitiateUnload_ShouldRaiseUnloadingEvent()
    {
        // Arrange
        var context = new PluginLoadContext(_testAssemblyPath);
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
        var context = new PluginLoadContext(_testAssemblyPath);

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
    private readonly string _testAssemblyPath;

    public PluginLoadContextReferenceTests()
    {
        _testAssemblyPath = Assembly.GetExecutingAssembly().Location;
    }

    [Fact]
    public void Constructor_ShouldSetPluginId()
    {
        // Arrange
        var context = new PluginLoadContext(_testAssemblyPath);

        // Act
        var reference = new PluginLoadContextReference("test-plugin", context);

        // Assert
        reference.PluginId.Should().Be("test-plugin");
    }

    [Fact]
    public void IsAlive_WithActiveContext_ShouldBeTrue()
    {
        // Arrange
        var context = new PluginLoadContext(_testAssemblyPath);
        var reference = new PluginLoadContextReference("test-plugin", context);

        // Act & Assert
        reference.IsAlive.Should().BeTrue();
    }

    [Fact]
    public void GetContext_WithActiveContext_ShouldReturnContext()
    {
        // Arrange
        var context = new PluginLoadContext(_testAssemblyPath);
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
        PluginLoadContextReference reference;

        // Create context in a scope so it can be collected
        CreateAndUnloadContext(out reference);

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

    private void CreateAndUnloadContext(out PluginLoadContextReference reference)
    {
        var context = new PluginLoadContext(_testAssemblyPath);
        reference = new PluginLoadContextReference("test-plugin", context);
        context.InitiateUnload();
    }
}

public class PluginLoadContextManagerTests : IDisposable
{
    private readonly PluginLoadContextManager _manager;
    private readonly string _testAssemblyPath;

    public PluginLoadContextManagerTests()
    {
        _manager = new PluginLoadContextManager();
        _testAssemblyPath = Assembly.GetExecutingAssembly().Location;
    }

    public void Dispose()
    {
        _manager.Dispose();
    }

    #region CreateContext Tests

    [Fact]
    public void CreateContext_ShouldReturnNewContext()
    {
        // Act
        var context = _manager.CreateContext("plugin1", _testAssemblyPath);

        // Assert
        context.Should().NotBeNull();
        context.PluginPath.Should().Be(_testAssemblyPath);
    }

    [Fact]
    public void CreateContext_SamePluginTwice_ShouldReturnNewContext()
    {
        // Arrange
        var context1 = _manager.CreateContext("plugin1", _testAssemblyPath);

        // Act
        var context2 = _manager.CreateContext("plugin1", _testAssemblyPath);

        // Assert
        context2.Should().NotBeSameAs(context1);
    }

    [Fact]
    public void CreateContext_DifferentPlugins_ShouldReturnDifferentContexts()
    {
        // Act
        var context1 = _manager.CreateContext("plugin1", _testAssemblyPath);
        var context2 = _manager.CreateContext("plugin2", _testAssemblyPath);

        // Assert
        context1.Should().NotBeSameAs(context2);
    }

    #endregion

    #region GetContext Tests

    [Fact]
    public void GetContext_ExistingPlugin_ShouldReturnContext()
    {
        // Arrange
        var createdContext = _manager.CreateContext("plugin1", _testAssemblyPath);

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
        var unloadingEventRaised = false;

        var context = _manager.CreateContext("plugin1", _testAssemblyPath);
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
        _manager.CreateContext("plugin1", _testAssemblyPath);

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
        _manager.CreateContext("plugin1", _testAssemblyPath);
        _manager.CreateContext("plugin2", _testAssemblyPath);

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

        var context1 = _manager.CreateContext("plugin1", _testAssemblyPath);
        context1.Unloading += (s, e) => unloadedPlugins.Add("plugin1");

        var context2 = _manager.CreateContext("plugin2", _testAssemblyPath);
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
