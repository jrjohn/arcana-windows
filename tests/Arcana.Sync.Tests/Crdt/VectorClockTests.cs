using Arcana.Sync.Crdt;
using FluentAssertions;
using Xunit;

namespace Arcana.Sync.Tests.Crdt;

public class VectorClockTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_ShouldCreateEmptyClock()
    {
        // Act
        var clock = new VectorClock();

        // Assert
        clock.Clock.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithDictionary_ShouldCopyValues()
    {
        // Arrange
        var initial = new Dictionary<string, long>
        {
            { "node1", 5 },
            { "node2", 3 }
        };

        // Act
        var clock = new VectorClock(initial);

        // Assert
        clock.Clock.Should().HaveCount(2);
        clock.GetValue("node1").Should().Be(5);
        clock.GetValue("node2").Should().Be(3);
    }

    [Fact]
    public void Constructor_WithDictionary_ShouldNotShareReference()
    {
        // Arrange
        var initial = new Dictionary<string, long> { { "node1", 5 } };
        var clock = new VectorClock(initial);

        // Act
        initial["node1"] = 10;

        // Assert - original clock should not be affected
        clock.GetValue("node1").Should().Be(5);
    }

    #endregion

    #region Increment Tests

    [Fact]
    public void Increment_NewNode_ShouldSetValueToOne()
    {
        // Arrange
        var clock = new VectorClock();

        // Act
        var newClock = clock.Increment("node1");

        // Assert
        newClock.GetValue("node1").Should().Be(1);
        clock.GetValue("node1").Should().Be(0); // Original unchanged
    }

    [Fact]
    public void Increment_ExistingNode_ShouldIncrementValue()
    {
        // Arrange
        var clock = new VectorClock(new Dictionary<string, long> { { "node1", 5 } });

        // Act
        var newClock = clock.Increment("node1");

        // Assert
        newClock.GetValue("node1").Should().Be(6);
        clock.GetValue("node1").Should().Be(5); // Original unchanged
    }

    [Fact]
    public void Increment_ShouldPreserveOtherNodes()
    {
        // Arrange
        var clock = new VectorClock(new Dictionary<string, long>
        {
            { "node1", 5 },
            { "node2", 3 }
        });

        // Act
        var newClock = clock.Increment("node1");

        // Assert
        newClock.GetValue("node1").Should().Be(6);
        newClock.GetValue("node2").Should().Be(3);
    }

    [Fact]
    public void Increment_ShouldReturnNewInstance()
    {
        // Arrange
        var clock = new VectorClock();

        // Act
        var newClock = clock.Increment("node1");

        // Assert
        newClock.Should().NotBeSameAs(clock);
    }

    #endregion

    #region GetValue Tests

    [Fact]
    public void GetValue_ExistingNode_ShouldReturnValue()
    {
        // Arrange
        var clock = new VectorClock(new Dictionary<string, long> { { "node1", 42 } });

        // Act
        var value = clock.GetValue("node1");

        // Assert
        value.Should().Be(42);
    }

    [Fact]
    public void GetValue_NonExistingNode_ShouldReturnZero()
    {
        // Arrange
        var clock = new VectorClock();

        // Act
        var value = clock.GetValue("nonexistent");

        // Assert
        value.Should().Be(0);
    }

    #endregion

    #region Merge Tests

    [Fact]
    public void Merge_EmptyClocks_ShouldReturnEmptyClock()
    {
        // Arrange
        var clock1 = new VectorClock();
        var clock2 = new VectorClock();

        // Act
        var merged = clock1.Merge(clock2);

        // Assert
        merged.Clock.Should().BeEmpty();
    }

    [Fact]
    public void Merge_OneEmptyClock_ShouldReturnOtherClock()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long> { { "node1", 5 } });
        var clock2 = new VectorClock();

        // Act
        var merged = clock1.Merge(clock2);

        // Assert
        merged.GetValue("node1").Should().Be(5);
    }

    [Fact]
    public void Merge_DisjointNodes_ShouldIncludeAll()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long> { { "node1", 5 } });
        var clock2 = new VectorClock(new Dictionary<string, long> { { "node2", 3 } });

        // Act
        var merged = clock1.Merge(clock2);

        // Assert
        merged.GetValue("node1").Should().Be(5);
        merged.GetValue("node2").Should().Be(3);
    }

    [Fact]
    public void Merge_OverlappingNodes_ShouldTakeMaximum()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long>
        {
            { "node1", 5 },
            { "node2", 3 }
        });
        var clock2 = new VectorClock(new Dictionary<string, long>
        {
            { "node1", 3 },
            { "node2", 7 }
        });

        // Act
        var merged = clock1.Merge(clock2);

        // Assert
        merged.GetValue("node1").Should().Be(5);
        merged.GetValue("node2").Should().Be(7);
    }

    [Fact]
    public void Merge_ShouldBeCommutative()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long> { { "node1", 5 } });
        var clock2 = new VectorClock(new Dictionary<string, long> { { "node2", 3 } });

        // Act
        var merged1 = clock1.Merge(clock2);
        var merged2 = clock2.Merge(clock1);

        // Assert
        merged1.Should().Be(merged2);
    }

    [Fact]
    public void Merge_ShouldBeAssociative()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long> { { "node1", 1 } });
        var clock2 = new VectorClock(new Dictionary<string, long> { { "node2", 2 } });
        var clock3 = new VectorClock(new Dictionary<string, long> { { "node3", 3 } });

        // Act
        var merged1 = clock1.Merge(clock2).Merge(clock3);
        var merged2 = clock1.Merge(clock2.Merge(clock3));

        // Assert
        merged1.Should().Be(merged2);
    }

    [Fact]
    public void Merge_ShouldBeIdempotent()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long> { { "node1", 5 } });

        // Act
        var merged = clock1.Merge(clock1);

        // Assert
        merged.Should().Be(clock1);
    }

    #endregion

    #region CompareTo Tests

    [Fact]
    public void CompareTo_EqualClocks_ShouldReturnEqual()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long> { { "node1", 5 } });
        var clock2 = new VectorClock(new Dictionary<string, long> { { "node1", 5 } });

        // Act
        var result = clock1.CompareTo(clock2);

        // Assert
        result.Should().Be(CausalRelation.Equal);
    }

    [Fact]
    public void CompareTo_EmptyClocks_ShouldReturnEqual()
    {
        // Arrange
        var clock1 = new VectorClock();
        var clock2 = new VectorClock();

        // Act
        var result = clock1.CompareTo(clock2);

        // Assert
        result.Should().Be(CausalRelation.Equal);
    }

    [Fact]
    public void CompareTo_ThisHappenedAfter_ShouldReturnHappenedAfter()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long>
        {
            { "node1", 5 },
            { "node2", 3 }
        });
        var clock2 = new VectorClock(new Dictionary<string, long>
        {
            { "node1", 3 },
            { "node2", 2 }
        });

        // Act
        var result = clock1.CompareTo(clock2);

        // Assert
        result.Should().Be(CausalRelation.HappenedAfter);
    }

    [Fact]
    public void CompareTo_ThisHappenedBefore_ShouldReturnHappenedBefore()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long>
        {
            { "node1", 3 },
            { "node2", 2 }
        });
        var clock2 = new VectorClock(new Dictionary<string, long>
        {
            { "node1", 5 },
            { "node2", 3 }
        });

        // Act
        var result = clock1.CompareTo(clock2);

        // Assert
        result.Should().Be(CausalRelation.HappenedBefore);
    }

    [Fact]
    public void CompareTo_Concurrent_ShouldReturnConcurrent()
    {
        // Arrange - clock1 has higher node1, clock2 has higher node2
        var clock1 = new VectorClock(new Dictionary<string, long>
        {
            { "node1", 5 },
            { "node2", 2 }
        });
        var clock2 = new VectorClock(new Dictionary<string, long>
        {
            { "node1", 3 },
            { "node2", 4 }
        });

        // Act
        var result = clock1.CompareTo(clock2);

        // Assert
        result.Should().Be(CausalRelation.Concurrent);
    }

    [Fact]
    public void CompareTo_DifferentNodeSets_ShouldHandleCorrectly()
    {
        // Arrange - clock1 has node that clock2 doesn't
        var clock1 = new VectorClock(new Dictionary<string, long>
        {
            { "node1", 5 },
            { "node2", 3 }
        });
        var clock2 = new VectorClock(new Dictionary<string, long>
        {
            { "node1", 5 }
        });

        // Act
        var result = clock1.CompareTo(clock2);

        // Assert - clock1 happened after because node2:3 > node2:0
        result.Should().Be(CausalRelation.HappenedAfter);
    }

    [Fact]
    public void IComparable_CompareTo_Null_ShouldReturnPositive()
    {
        // Arrange
        var clock = new VectorClock();
        IComparable<VectorClock> comparable = clock;

        // Act
        var result = comparable.CompareTo(null);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void IComparable_CompareTo_ShouldMapHappenedAfterToPositive()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long> { { "node1", 5 } });
        var clock2 = new VectorClock(new Dictionary<string, long> { { "node1", 3 } });
        IComparable<VectorClock> comparable = clock1;

        // Act
        var result = comparable.CompareTo(clock2);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void IComparable_CompareTo_ShouldMapHappenedBeforeToNegative()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long> { { "node1", 3 } });
        var clock2 = new VectorClock(new Dictionary<string, long> { { "node1", 5 } });
        IComparable<VectorClock> comparable = clock1;

        // Act
        var result = comparable.CompareTo(clock2);

        // Assert
        result.Should().Be(-1);
    }

    [Fact]
    public void IComparable_CompareTo_ShouldMapConcurrentToZero()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long> { { "node1", 5 }, { "node2", 2 } });
        var clock2 = new VectorClock(new Dictionary<string, long> { { "node1", 3 }, { "node2", 4 } });
        IComparable<VectorClock> comparable = clock1;

        // Act
        var result = comparable.CompareTo(clock2);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameClock_ShouldReturnTrue()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long> { { "node1", 5 } });
        var clock2 = new VectorClock(new Dictionary<string, long> { { "node1", 5 } });

        // Act & Assert
        clock1.Equals(clock2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentClock_ShouldReturnFalse()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long> { { "node1", 5 } });
        var clock2 = new VectorClock(new Dictionary<string, long> { { "node1", 3 } });

        // Act & Assert
        clock1.Equals(clock2).Should().BeFalse();
    }

    [Fact]
    public void Equals_Null_ShouldReturnFalse()
    {
        // Arrange
        var clock = new VectorClock();

        // Act & Assert
        clock.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_Object_ShouldWorkCorrectly()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long> { { "node1", 5 } });
        var clock2 = new VectorClock(new Dictionary<string, long> { { "node1", 5 } });
        object obj = clock2;

        // Act & Assert
        clock1.Equals(obj).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_EqualClocks_ShouldBeEqual()
    {
        // Arrange
        var clock1 = new VectorClock(new Dictionary<string, long> { { "node1", 5 }, { "node2", 3 } });
        var clock2 = new VectorClock(new Dictionary<string, long> { { "node1", 5 }, { "node2", 3 } });

        // Act & Assert
        clock1.GetHashCode().Should().Be(clock2.GetHashCode());
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void Serialize_ShouldProduceValidJson()
    {
        // Arrange
        var clock = new VectorClock(new Dictionary<string, long>
        {
            { "node1", 5 },
            { "node2", 3 }
        });

        // Act
        var json = clock.Serialize();

        // Assert
        json.Should().Contain("node1");
        json.Should().Contain("5");
        json.Should().Contain("node2");
        json.Should().Contain("3");
    }

    [Fact]
    public void Deserialize_ValidJson_ShouldReconstructClock()
    {
        // Arrange
        var original = new VectorClock(new Dictionary<string, long>
        {
            { "node1", 5 },
            { "node2", 3 }
        });
        var json = original.Serialize();

        // Act
        var deserialized = VectorClock.Deserialize(json);

        // Assert
        deserialized.Should().Be(original);
    }

    [Fact]
    public void Deserialize_EmptyObject_ShouldReturnEmptyClock()
    {
        // Act
        var clock = VectorClock.Deserialize("{}");

        // Assert
        clock.Clock.Should().BeEmpty();
    }

    [Fact]
    public void RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var original = new VectorClock(new Dictionary<string, long>
        {
            { "node1", 5 },
            { "node2", 3 },
            { "node3", 10 }
        });

        // Act
        var json = original.Serialize();
        var restored = VectorClock.Deserialize(json);

        // Assert
        restored.GetValue("node1").Should().Be(5);
        restored.GetValue("node2").Should().Be(3);
        restored.GetValue("node3").Should().Be(10);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_EmptyClock_ShouldReturnEmptyFormat()
    {
        // Arrange
        var clock = new VectorClock();

        // Act
        var str = clock.ToString();

        // Assert
        str.Should().Be("VectorClock()");
    }

    [Fact]
    public void ToString_WithValues_ShouldIncludeAllPairs()
    {
        // Arrange
        var clock = new VectorClock(new Dictionary<string, long> { { "node1", 5 } });

        // Act
        var str = clock.ToString();

        // Assert
        str.Should().Contain("node1:5");
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void Scenario_TwoNodesEditingConcurrently_ShouldDetectConcurrency()
    {
        // Simulate two nodes starting from the same state
        var initialClock = new VectorClock();

        // Node A makes an edit
        var clockA = initialClock.Increment("nodeA");

        // Node B makes an edit (from the same initial state)
        var clockB = initialClock.Increment("nodeB");

        // These edits are concurrent
        clockA.CompareTo(clockB).Should().Be(CausalRelation.Concurrent);
        clockB.CompareTo(clockA).Should().Be(CausalRelation.Concurrent);
    }

    [Fact]
    public void Scenario_SequentialEdits_ShouldDetectCausality()
    {
        // Node A makes an edit
        var clockA1 = new VectorClock().Increment("nodeA");

        // Node B receives A's edit and makes another edit
        var clockB1 = clockA1.Merge(new VectorClock()).Increment("nodeB");

        // B's edit happened after A's
        clockB1.CompareTo(clockA1).Should().Be(CausalRelation.HappenedAfter);
        clockA1.CompareTo(clockB1).Should().Be(CausalRelation.HappenedBefore);
    }

    [Fact]
    public void Scenario_MergeAfterConflict_ShouldHaveCorrectClock()
    {
        // Both nodes start from the same state
        var initial = new VectorClock();

        // Both nodes make concurrent edits
        var clockA = initial.Increment("nodeA");
        var clockB = initial.Increment("nodeB");

        // After merging, the merged clock dominates both
        var merged = clockA.Merge(clockB);

        merged.CompareTo(clockA).Should().Be(CausalRelation.HappenedAfter);
        merged.CompareTo(clockB).Should().Be(CausalRelation.HappenedAfter);
    }

    #endregion
}
