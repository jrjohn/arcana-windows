using Arcana.Sync.Crdt;
using FluentAssertions;
using Xunit;

namespace Arcana.Sync.Tests.Crdt;

public class MVRegisterTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_ShouldBeEmpty()
    {
        // Act
        var register = new MVRegister<string>();

        // Assert
        register.Values.Should().BeEmpty();
        register.HasConflict.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithValue_ShouldContainSingleValue()
    {
        // Arrange
        var clock = new VectorClock().Increment("node1");

        // Act
        var register = new MVRegister<string>("value", clock);

        // Assert
        register.Values.Should().HaveCount(1);
        register.Values[0].Value.Should().Be("value");
        register.HasConflict.Should().BeFalse();
    }

    #endregion

    #region Set Tests

    [Fact]
    public void Set_ToEmpty_ShouldAddValue()
    {
        // Arrange
        var register = new MVRegister<string>();
        var clock = new VectorClock().Increment("node1");

        // Act
        register.Set("value", clock);

        // Assert
        register.Values.Should().HaveCount(1);
        register.Values[0].Value.Should().Be("value");
    }

    [Fact]
    public void Set_NewerValue_ShouldReplacePrevious()
    {
        // Arrange
        var clock1 = new VectorClock().Increment("node1");
        var clock2 = clock1.Increment("node1"); // clock2 happens after clock1

        var register = new MVRegister<string>("old", clock1);

        // Act
        register.Set("new", clock2);

        // Assert
        register.Values.Should().HaveCount(1);
        register.Values[0].Value.Should().Be("new");
        register.HasConflict.Should().BeFalse();
    }

    [Fact]
    public void Set_ConcurrentValues_ShouldCreateConflict()
    {
        // Arrange
        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2"); // Concurrent with clock1

        var register = new MVRegister<string>();

        // Act
        register.Set("value1", clock1);
        register.Set("value2", clock2);

        // Assert
        register.Values.Should().HaveCount(2);
        register.HasConflict.Should().BeTrue();
    }

    [Fact]
    public void Set_OlderValue_ShouldNotAdd()
    {
        // Arrange
        var clock1 = new VectorClock().Increment("node1");
        var clock2 = clock1.Increment("node1"); // clock2 is newer

        var register = new MVRegister<string>("new", clock2);

        // Act
        register.Set("old", clock1); // Try to add older value

        // Assert
        register.Values.Should().HaveCount(1);
        register.Values[0].Value.Should().Be("new");
    }

    [Fact]
    public void Set_NewValueSupersedingMultiple_ShouldReplaceAll()
    {
        // Arrange - Create concurrent values first
        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var register = new MVRegister<string>();
        register.Set("value1", clock1);
        register.Set("value2", clock2);
        register.HasConflict.Should().BeTrue();

        // Create clock that supersedes both
        var mergedClock = clock1.Merge(clock2).Increment("node1");

        // Act
        register.Set("resolved", mergedClock);

        // Assert
        register.Values.Should().HaveCount(1);
        register.Values[0].Value.Should().Be("resolved");
        register.HasConflict.Should().BeFalse();
    }

    #endregion

    #region SingleValue Tests

    [Fact]
    public void SingleValue_WithOneValue_ShouldReturnValue()
    {
        // Arrange
        var clock = new VectorClock().Increment("node1");
        var register = new MVRegister<string>("single", clock);

        // Act
        var value = register.SingleValue;

        // Assert
        value.Should().Be("single");
    }

    [Fact]
    public void SingleValue_WithMultipleValues_ShouldThrow()
    {
        // Arrange
        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var register = new MVRegister<string>();
        register.Set("value1", clock1);
        register.Set("value2", clock2);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => register.SingleValue);
    }

    #endregion

    #region Merge Tests

    [Fact]
    public void Merge_EmptyRegisters_ShouldReturnEmpty()
    {
        // Arrange
        var register1 = new MVRegister<string>();
        var register2 = new MVRegister<string>();

        // Act
        var merged = register1.Merge(register2);

        // Assert
        merged.Values.Should().BeEmpty();
    }

    [Fact]
    public void Merge_OneEmpty_ShouldReturnOther()
    {
        // Arrange
        var clock = new VectorClock().Increment("node1");
        var register1 = new MVRegister<string>("value", clock);
        var register2 = new MVRegister<string>();

        // Act
        var merged = register1.Merge(register2);

        // Assert
        merged.Values.Should().HaveCount(1);
        merged.Values[0].Value.Should().Be("value");
    }

    [Fact]
    public void Merge_OneNewerThanOther_ShouldReturnNewer()
    {
        // Arrange
        var clock1 = new VectorClock().Increment("node1");
        var clock2 = clock1.Increment("node1");

        var register1 = new MVRegister<string>("old", clock1);
        var register2 = new MVRegister<string>("new", clock2);

        // Act
        var merged = register1.Merge(register2);

        // Assert
        merged.Values.Should().HaveCount(1);
        merged.Values[0].Value.Should().Be("new");
    }

    [Fact]
    public void Merge_ConcurrentValues_ShouldKeepBoth()
    {
        // Arrange
        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var register1 = new MVRegister<string>("value1", clock1);
        var register2 = new MVRegister<string>("value2", clock2);

        // Act
        var merged = register1.Merge(register2);

        // Assert
        merged.Values.Should().HaveCount(2);
        merged.HasConflict.Should().BeTrue();
        merged.Values.Select(v => v.Value).Should().Contain("value1");
        merged.Values.Select(v => v.Value).Should().Contain("value2");
    }

    [Fact]
    public void Merge_ShouldBeCommutative()
    {
        // Arrange
        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var register1 = new MVRegister<string>("value1", clock1);
        var register2 = new MVRegister<string>("value2", clock2);

        // Act
        var merged1 = register1.Merge(register2);
        var merged2 = register2.Merge(register1);

        // Assert
        merged1.Values.Select(v => v.Value).Should().BeEquivalentTo(merged2.Values.Select(v => v.Value));
    }

    [Fact]
    public void Merge_ShouldRemoveDuplicateClocks()
    {
        // Arrange - same clock in both registers
        var clock = new VectorClock().Increment("node1");
        var register1 = new MVRegister<string>("value", clock);
        var register2 = new MVRegister<string>("value", clock);

        // Act
        var merged = register1.Merge(register2);

        // Assert
        merged.Values.Should().HaveCount(1);
    }

    [Fact]
    public void Merge_ComplexScenario_ShouldResolveCorrectly()
    {
        // Arrange - Multiple values with complex causal relationships
        var clockA1 = new VectorClock().Increment("nodeA");
        var clockB1 = new VectorClock().Increment("nodeB");
        var clockA2 = clockA1.Merge(clockB1).Increment("nodeA"); // A2 supersedes both A1 and B1

        var register1 = new MVRegister<string>();
        register1.Set("A1", clockA1);
        register1.Set("B1", clockB1);

        var register2 = new MVRegister<string>("A2", clockA2);

        // Act
        var merged = register1.Merge(register2);

        // Assert - Only A2 should remain as it supersedes both A1 and B1
        merged.Values.Should().HaveCount(1);
        merged.Values[0].Value.Should().Be("A2");
    }

    #endregion

    #region Resolve Tests

    [Fact]
    public void Resolve_ShouldClearAllAndSetSingle()
    {
        // Arrange
        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var register = new MVRegister<string>();
        register.Set("value1", clock1);
        register.Set("value2", clock2);
        register.HasConflict.Should().BeTrue();

        var mergedClock = clock1.Merge(clock2).Increment("resolver");

        // Act
        register.Resolve("resolved", mergedClock);

        // Assert
        register.Values.Should().HaveCount(1);
        register.Values[0].Value.Should().Be("resolved");
        register.HasConflict.Should().BeFalse();
    }

    [Fact]
    public void Resolve_WithUserSelection_ShouldSetSelectedValue()
    {
        // Arrange
        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var register = new MVRegister<string>();
        register.Set("option1", clock1);
        register.Set("option2", clock2);

        // User selects option1
        var mergedClock = clock1.Merge(clock2).Increment("user");

        // Act
        register.Resolve("option1", mergedClock);

        // Assert
        register.SingleValue.Should().Be("option1");
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void Scenario_ThreeWayConflict()
    {
        // Three users edit the same document concurrently
        var clockA = new VectorClock().Increment("userA");
        var clockB = new VectorClock().Increment("userB");
        var clockC = new VectorClock().Increment("userC");

        var register = new MVRegister<string>();
        register.Set("User A's version", clockA);
        register.Set("User B's version", clockB);
        register.Set("User C's version", clockC);

        // All three should be kept as concurrent
        register.Values.Should().HaveCount(3);
        register.HasConflict.Should().BeTrue();

        // Resolve by merging content
        var resolvedClock = clockA.Merge(clockB).Merge(clockC).Increment("resolver");
        register.Resolve("Merged version from all users", resolvedClock);

        register.HasConflict.Should().BeFalse();
        register.SingleValue.Should().Be("Merged version from all users");
    }

    [Fact]
    public void Scenario_OfflineSync()
    {
        // User works offline, then syncs with server
        var serverClock = new VectorClock().Increment("server");
        var offlineClock = new VectorClock().Increment("offline_user");

        var serverRegister = new MVRegister<string>("Server version", serverClock);
        var offlineRegister = new MVRegister<string>("Offline changes", offlineClock);

        // When syncing, both versions are kept for user resolution
        var merged = serverRegister.Merge(offlineRegister);

        merged.HasConflict.Should().BeTrue();
        merged.Values.Should().HaveCount(2);
    }

    #endregion
}
