using Arcana.Sync.Crdt;
using FluentAssertions;
using Xunit;

namespace Arcana.Sync.Tests.Crdt;

public class LWWRegisterTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldSetInitialValues()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var register = new LWWRegister<string>("test", timestamp, "node1");

        // Assert
        register.Value.Should().Be("test");
        register.Timestamp.Should().Be(timestamp);
        register.NodeId.Should().Be("node1");
    }

    [Fact]
    public void Constructor_WithNullValue_ShouldAllowNull()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var register = new LWWRegister<string>(null, timestamp, "node1");

        // Assert
        register.Value.Should().BeNull();
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_NewerTimestamp_ShouldUpdateValue()
    {
        // Arrange
        var oldTime = DateTime.UtcNow;
        var newTime = oldTime.AddSeconds(1);
        var register = new LWWRegister<string>("old", oldTime, "node1");

        // Act
        var result = register.Update("new", newTime, "node2");

        // Assert
        result.Should().BeTrue();
        register.Value.Should().Be("new");
        register.Timestamp.Should().Be(newTime);
        register.NodeId.Should().Be("node2");
    }

    [Fact]
    public void Update_OlderTimestamp_ShouldNotUpdate()
    {
        // Arrange
        var newTime = DateTime.UtcNow;
        var oldTime = newTime.AddSeconds(-1);
        var register = new LWWRegister<string>("current", newTime, "node1");

        // Act
        var result = register.Update("old", oldTime, "node2");

        // Assert
        result.Should().BeFalse();
        register.Value.Should().Be("current");
        register.Timestamp.Should().Be(newTime);
        register.NodeId.Should().Be("node1");
    }

    [Fact]
    public void Update_SameTimestampHigherNodeId_ShouldUpdate()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var register = new LWWRegister<string>("old", timestamp, "nodeA");

        // Act - nodeZ > nodeA lexicographically
        var result = register.Update("new", timestamp, "nodeZ");

        // Assert
        result.Should().BeTrue();
        register.Value.Should().Be("new");
        register.NodeId.Should().Be("nodeZ");
    }

    [Fact]
    public void Update_SameTimestampLowerNodeId_ShouldNotUpdate()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var register = new LWWRegister<string>("current", timestamp, "nodeZ");

        // Act - nodeA < nodeZ lexicographically
        var result = register.Update("new", timestamp, "nodeA");

        // Assert
        result.Should().BeFalse();
        register.Value.Should().Be("current");
        register.NodeId.Should().Be("nodeZ");
    }

    [Fact]
    public void Update_SameTimestampSameNodeId_ShouldNotUpdate()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var register = new LWWRegister<string>("current", timestamp, "node1");

        // Act
        var result = register.Update("new", timestamp, "node1");

        // Assert
        result.Should().BeFalse();
        register.Value.Should().Be("current");
    }

    [Fact]
    public void Update_WithNullValue_ShouldUpdateToNull()
    {
        // Arrange
        var oldTime = DateTime.UtcNow;
        var newTime = oldTime.AddSeconds(1);
        var register = new LWWRegister<string>("value", oldTime, "node1");

        // Act
        var result = register.Update(null, newTime, "node2");

        // Assert
        result.Should().BeTrue();
        register.Value.Should().BeNull();
    }

    #endregion

    #region Merge Tests

    [Fact]
    public void Merge_OtherNewer_ShouldReturnOther()
    {
        // Arrange
        var oldTime = DateTime.UtcNow;
        var newTime = oldTime.AddSeconds(1);
        var register1 = new LWWRegister<string>("old", oldTime, "node1");
        var register2 = new LWWRegister<string>("new", newTime, "node2");

        // Act
        var merged = register1.Merge(register2);

        // Assert
        merged.Value.Should().Be("new");
        merged.Timestamp.Should().Be(newTime);
        merged.NodeId.Should().Be("node2");
    }

    [Fact]
    public void Merge_ThisNewer_ShouldReturnThis()
    {
        // Arrange
        var oldTime = DateTime.UtcNow;
        var newTime = oldTime.AddSeconds(1);
        var register1 = new LWWRegister<string>("new", newTime, "node1");
        var register2 = new LWWRegister<string>("old", oldTime, "node2");

        // Act
        var merged = register1.Merge(register2);

        // Assert
        merged.Value.Should().Be("new");
        merged.Timestamp.Should().Be(newTime);
        merged.NodeId.Should().Be("node1");
    }

    [Fact]
    public void Merge_SameTimestampHigherNodeId_ShouldReturnHigherNode()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var register1 = new LWWRegister<string>("value1", timestamp, "nodeA");
        var register2 = new LWWRegister<string>("value2", timestamp, "nodeZ");

        // Act
        var merged = register1.Merge(register2);

        // Assert - nodeZ wins
        merged.Value.Should().Be("value2");
        merged.NodeId.Should().Be("nodeZ");
    }

    [Fact]
    public void Merge_ShouldReturnNewInstance()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var register1 = new LWWRegister<string>("value1", timestamp, "node1");
        var register2 = new LWWRegister<string>("value2", timestamp.AddSeconds(-1), "node2");

        // Act
        var merged = register1.Merge(register2);

        // Assert
        merged.Should().NotBeSameAs(register1);
        merged.Should().NotBeSameAs(register2);
    }

    [Fact]
    public void Merge_ShouldBeCommutative()
    {
        // Arrange
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(1);
        var register1 = new LWWRegister<string>("value1", timestamp1, "node1");
        var register2 = new LWWRegister<string>("value2", timestamp2, "node2");

        // Act
        var merged1 = register1.Merge(register2);
        var merged2 = register2.Merge(register1);

        // Assert
        merged1.Value.Should().Be(merged2.Value);
        merged1.Timestamp.Should().Be(merged2.Timestamp);
    }

    #endregion

    #region Type Tests

    [Fact]
    public void LWWRegister_WithComplexType_ShouldWork()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var complexObject = new TestEntity { Id = 1, Name = "Test" };

        // Act
        var register = new LWWRegister<TestEntity>(complexObject, timestamp, "node1");

        // Assert
        register.Value.Should().NotBeNull();
        register.Value!.Id.Should().Be(1);
        register.Value.Name.Should().Be("Test");
    }

    [Fact]
    public void LWWRegister_WithValueType_ShouldWork()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var register = new LWWRegister<int>(42, timestamp, "node1");

        // Assert
        register.Value.Should().Be(42);
    }

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}

public class LWWMapTests
{
    #region Set Tests

    [Fact]
    public void Set_NewField_ShouldAddField()
    {
        // Arrange
        var map = new LWWMap();
        var timestamp = DateTime.UtcNow;

        // Act
        map.Set("field1", "value1", timestamp, "node1");

        // Assert
        map.Fields.Should().ContainKey("field1");
        map.Get<string>("field1").Should().Be("value1");
    }

    [Fact]
    public void Set_ExistingFieldNewerTimestamp_ShouldUpdate()
    {
        // Arrange
        var map = new LWWMap();
        var oldTime = DateTime.UtcNow;
        var newTime = oldTime.AddSeconds(1);
        map.Set("field1", "old", oldTime, "node1");

        // Act
        map.Set("field1", "new", newTime, "node2");

        // Assert
        map.Get<string>("field1").Should().Be("new");
    }

    [Fact]
    public void Set_ExistingFieldOlderTimestamp_ShouldNotUpdate()
    {
        // Arrange
        var map = new LWWMap();
        var newTime = DateTime.UtcNow;
        var oldTime = newTime.AddSeconds(-1);
        map.Set("field1", "current", newTime, "node1");

        // Act
        map.Set("field1", "old", oldTime, "node2");

        // Assert
        map.Get<string>("field1").Should().Be("current");
    }

    [Fact]
    public void Set_MultipleFields_ShouldStoreSeparately()
    {
        // Arrange
        var map = new LWWMap();
        var timestamp = DateTime.UtcNow;

        // Act
        map.Set("field1", "value1", timestamp, "node1");
        map.Set("field2", "value2", timestamp, "node1");
        map.Set("field3", "value3", timestamp, "node1");

        // Assert
        map.Fields.Should().HaveCount(3);
        map.Get<string>("field1").Should().Be("value1");
        map.Get<string>("field2").Should().Be("value2");
        map.Get<string>("field3").Should().Be("value3");
    }

    #endregion

    #region Get Tests

    [Fact]
    public void Get_NonExistingField_ShouldReturnDefault()
    {
        // Arrange
        var map = new LWWMap();

        // Act
        var result = map.Get<string>("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Get_WrongType_ShouldReturnDefault()
    {
        // Arrange
        var map = new LWWMap();
        map.Set("field1", "string value", DateTime.UtcNow, "node1");

        // Act
        var result = map.Get<int>("field1");

        // Assert
        result.Should().Be(0); // default(int)
    }

    [Fact]
    public void Get_CorrectType_ShouldReturnValue()
    {
        // Arrange
        var map = new LWWMap();
        map.Set("field1", 42, DateTime.UtcNow, "node1");

        // Act
        var result = map.Get<int>("field1");

        // Assert
        result.Should().Be(42);
    }

    #endregion

    #region Merge Tests

    [Fact]
    public void Merge_DisjointFields_ShouldIncludeAll()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var map1 = new LWWMap();
        map1.Set("field1", "value1", timestamp, "node1");

        var map2 = new LWWMap();
        map2.Set("field2", "value2", timestamp, "node2");

        // Act
        var merged = map1.Merge(map2);

        // Assert
        merged.Fields.Should().HaveCount(2);
        merged.Get<string>("field1").Should().Be("value1");
        merged.Get<string>("field2").Should().Be("value2");
    }

    [Fact]
    public void Merge_OverlappingFields_ShouldTakeNewer()
    {
        // Arrange
        var oldTime = DateTime.UtcNow;
        var newTime = oldTime.AddSeconds(1);

        var map1 = new LWWMap();
        map1.Set("field1", "old", oldTime, "node1");

        var map2 = new LWWMap();
        map2.Set("field1", "new", newTime, "node2");

        // Act
        var merged = map1.Merge(map2);

        // Assert
        merged.Get<string>("field1").Should().Be("new");
    }

    [Fact]
    public void Merge_ShouldReturnNewInstance()
    {
        // Arrange
        var map1 = new LWWMap();
        var map2 = new LWWMap();

        // Act
        var merged = map1.Merge(map2);

        // Assert
        merged.Should().NotBeSameAs(map1);
        merged.Should().NotBeSameAs(map2);
    }

    [Fact]
    public void Merge_ShouldBeCommutative()
    {
        // Arrange
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(1);

        var map1 = new LWWMap();
        map1.Set("field1", "value1", timestamp1, "node1");

        var map2 = new LWWMap();
        map2.Set("field1", "value2", timestamp2, "node2");

        // Act
        var merged1 = map1.Merge(map2);
        var merged2 = map2.Merge(map1);

        // Assert
        merged1.Get<string>("field1").Should().Be(merged2.Get<string>("field1"));
    }

    [Fact]
    public void Merge_MixedFieldTimestamps_ShouldMergeFieldByField()
    {
        // Arrange
        var time1 = DateTime.UtcNow;
        var time2 = time1.AddSeconds(1);

        var map1 = new LWWMap();
        map1.Set("field1", "map1-newer", time2, "node1"); // newer
        map1.Set("field2", "map1-older", time1, "node1"); // older

        var map2 = new LWWMap();
        map2.Set("field1", "map2-older", time1, "node2"); // older
        map2.Set("field2", "map2-newer", time2, "node2"); // newer

        // Act
        var merged = map1.Merge(map2);

        // Assert - each field independently uses newest value
        merged.Get<string>("field1").Should().Be("map1-newer");
        merged.Get<string>("field2").Should().Be("map2-newer");
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void Scenario_ConcurrentEditsToSameDocument()
    {
        // Two users editing different fields of the same document
        var baseTime = DateTime.UtcNow;

        // User A edits title
        var mapA = new LWWMap();
        mapA.Set("title", "User A's Title", baseTime.AddSeconds(1), "userA");
        mapA.Set("content", "Original content", baseTime, "system");

        // User B edits content
        var mapB = new LWWMap();
        mapB.Set("title", "Original Title", baseTime, "system");
        mapB.Set("content", "User B's Content", baseTime.AddSeconds(2), "userB");

        // Merge should take the newer version of each field
        var merged = mapA.Merge(mapB);

        merged.Get<string>("title").Should().Be("User A's Title");
        merged.Get<string>("content").Should().Be("User B's Content");
    }

    #endregion
}
