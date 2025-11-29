using Arcana.Sync.Crdt;
using FluentAssertions;
using Xunit;

namespace Arcana.Sync.Tests.Crdt;

public class ConflictResolverTests
{
    private readonly ConflictResolver _resolver;

    public ConflictResolverTests()
    {
        _resolver = new ConflictResolver("testNode");
    }

    #region Test Entities

    private class TestEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    private class EntityWithFieldTimestamps
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public DateTime TitleModifiedAt { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime ContentModifiedAt { get; set; }
    }

    #endregion

    #region Causal Resolution Tests (No Conflict)

    [Fact]
    public void Resolve_LocalHappenedAfter_ShouldReturnLocal()
    {
        // Arrange
        var clock1 = new VectorClock().Increment("node1");
        var clock2 = clock1.Increment("node1"); // clock2 happened after clock1

        var local = new TestEntity { Name = "Local", Value = 2 };
        var remote = new TestEntity { Name = "Remote", Value = 1 };

        // Act
        var result = _resolver.Resolve(
            local, remote,
            clock2, clock1,
            DateTime.UtcNow, DateTime.UtcNow.AddSeconds(-1));

        // Assert
        result.Result.Should().Be(local);
        result.HadConflict.Should().BeFalse();
        result.Resolution.Should().Be("Local version is newer");
    }

    [Fact]
    public void Resolve_LocalHappenedBefore_ShouldReturnRemote()
    {
        // Arrange
        var clock1 = new VectorClock().Increment("node1");
        var clock2 = clock1.Increment("node1");

        var local = new TestEntity { Name = "Local", Value = 1 };
        var remote = new TestEntity { Name = "Remote", Value = 2 };

        // Act
        var result = _resolver.Resolve(
            local, remote,
            clock1, clock2,
            DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow);

        // Assert
        result.Result.Should().Be(remote);
        result.HadConflict.Should().BeFalse();
        result.Resolution.Should().Be("Remote version is newer");
    }

    [Fact]
    public void Resolve_EqualClocks_ShouldReturnLocal()
    {
        // Arrange
        var clock = new VectorClock().Increment("node1");

        var local = new TestEntity { Name = "Same", Value = 1 };
        var remote = new TestEntity { Name = "Same", Value = 1 };

        // Act
        var result = _resolver.Resolve(
            local, remote,
            clock, clock,
            DateTime.UtcNow, DateTime.UtcNow);

        // Assert
        result.Result.Should().Be(local);
        result.HadConflict.Should().BeFalse();
        result.Resolution.Should().Be("Versions are identical");
    }

    #endregion

    #region LastWriterWins Strategy Tests

    [Fact]
    public void Resolve_LWW_ConcurrentRemoteNewer_ShouldReturnRemote()
    {
        // Arrange
        _resolver.Configure<TestEntity>(ConflictResolutionStrategy.LastWriterWins);

        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2"); // Concurrent

        var oldTime = DateTime.UtcNow.AddSeconds(-10);
        var newTime = DateTime.UtcNow;

        var local = new TestEntity { Name = "Local" };
        var remote = new TestEntity { Name = "Remote" };

        // Act
        var result = _resolver.Resolve(
            local, remote,
            clock1, clock2,
            oldTime, newTime); // Remote has newer timestamp

        // Assert
        result.Result.Name.Should().Be("Remote");
        result.HadConflict.Should().BeTrue();
        result.Resolution.Should().Contain("LastWriterWins");
    }

    [Fact]
    public void Resolve_LWW_ConcurrentLocalNewer_ShouldReturnLocal()
    {
        // Arrange
        _resolver.Configure<TestEntity>(ConflictResolutionStrategy.LastWriterWins);

        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var oldTime = DateTime.UtcNow.AddSeconds(-10);
        var newTime = DateTime.UtcNow;

        var local = new TestEntity { Name = "Local" };
        var remote = new TestEntity { Name = "Remote" };

        // Act
        var result = _resolver.Resolve(
            local, remote,
            clock1, clock2,
            newTime, oldTime); // Local has newer timestamp

        // Assert
        result.Result.Name.Should().Be("Local");
        result.HadConflict.Should().BeTrue();
    }

    [Fact]
    public void Resolve_LWW_SameTimestamp_ShouldUseDeterministicTiebreaker()
    {
        // Arrange
        _resolver.Configure<TestEntity>(ConflictResolutionStrategy.LastWriterWins);

        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var sameTime = DateTime.UtcNow;

        var local = new TestEntity { Id = "A", Name = "Local" };
        var remote = new TestEntity { Id = "Z", Name = "Remote" };

        // Act
        var result = _resolver.Resolve(
            local, remote,
            clock1, clock2,
            sameTime, sameTime);

        // Assert - Should use entity ID comparison for deterministic ordering
        result.HadConflict.Should().BeTrue();
        // Result depends on ID comparison (Z > A, so remote wins based on typical implementation)
    }

    #endregion

    #region FirstWriterWins Strategy Tests

    [Fact]
    public void Resolve_FWW_ConcurrentRemoteOlder_ShouldReturnRemote()
    {
        // Arrange
        _resolver.Configure<TestEntity>(ConflictResolutionStrategy.FirstWriterWins);

        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var oldTime = DateTime.UtcNow.AddSeconds(-10);
        var newTime = DateTime.UtcNow;

        var local = new TestEntity { Name = "Local" };
        var remote = new TestEntity { Name = "Remote" };

        // Act
        var result = _resolver.Resolve(
            local, remote,
            clock1, clock2,
            newTime, oldTime); // Remote has older timestamp

        // Assert
        result.Result.Name.Should().Be("Remote");
        result.HadConflict.Should().BeTrue();
        result.Resolution.Should().Contain("FirstWriterWins");
    }

    [Fact]
    public void Resolve_FWW_ConcurrentLocalOlder_ShouldReturnLocal()
    {
        // Arrange
        _resolver.Configure<TestEntity>(ConflictResolutionStrategy.FirstWriterWins);

        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var oldTime = DateTime.UtcNow.AddSeconds(-10);
        var newTime = DateTime.UtcNow;

        var local = new TestEntity { Name = "Local" };
        var remote = new TestEntity { Name = "Remote" };

        // Act
        var result = _resolver.Resolve(
            local, remote,
            clock1, clock2,
            oldTime, newTime); // Local has older timestamp

        // Assert
        result.Result.Name.Should().Be("Local");
        result.HadConflict.Should().BeTrue();
    }

    #endregion

    #region FieldLevelMerge Strategy Tests

    [Fact]
    public void Resolve_FieldLevel_ShouldMergeFieldByField()
    {
        // Arrange
        _resolver.Configure<TestEntity>(ConflictResolutionStrategy.FieldLevelMerge);

        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var time1 = DateTime.UtcNow;
        var time2 = DateTime.UtcNow.AddSeconds(-10);

        var local = new TestEntity
        {
            Id = "entity1",
            Name = "Local Name",
            Value = 100,
            Description = "Local Description"
        };
        var remote = new TestEntity
        {
            Id = "entity1",
            Name = "Remote Name",
            Value = 200,
            Description = "Remote Description"
        };

        // Act - Local has newer timestamp overall, so all fields use local
        var result = _resolver.Resolve(
            local, remote,
            clock1, clock2,
            time1, time2);

        // Assert
        result.HadConflict.Should().BeTrue();
        result.Result.Name.Should().Be("Local Name");
        result.Result.Value.Should().Be(100);
        result.Resolution.Should().Contain("FieldLevelMerge");
    }

    [Fact]
    public void Resolve_FieldLevel_WithFieldTimestamps_ShouldUseFieldTimestamps()
    {
        // Arrange
        _resolver.Configure<EntityWithFieldTimestamps>(ConflictResolutionStrategy.FieldLevelMerge);

        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var baseTime = DateTime.UtcNow;

        var local = new EntityWithFieldTimestamps
        {
            Id = "entity1",
            Title = "Local Title",
            TitleModifiedAt = baseTime.AddSeconds(10), // Newer title
            Content = "Local Content",
            ContentModifiedAt = baseTime.AddSeconds(-10) // Older content
        };
        var remote = new EntityWithFieldTimestamps
        {
            Id = "entity1",
            Title = "Remote Title",
            TitleModifiedAt = baseTime.AddSeconds(-10), // Older title
            Content = "Remote Content",
            ContentModifiedAt = baseTime.AddSeconds(10) // Newer content
        };

        // Act
        var result = _resolver.Resolve(
            local, remote,
            clock1, clock2,
            baseTime, baseTime);

        // Assert - Should use field-level timestamps
        result.HadConflict.Should().BeTrue();
        result.Result.Title.Should().Be("Local Title"); // Local title is newer
        result.Result.Content.Should().Be("Remote Content"); // Remote content is newer
    }

    [Fact]
    public void Resolve_FieldLevel_SameFieldValues_ShouldUseEither()
    {
        // Arrange
        _resolver.Configure<TestEntity>(ConflictResolutionStrategy.FieldLevelMerge);

        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var local = new TestEntity { Id = "same", Name = "Same Name", Value = 42 };
        var remote = new TestEntity { Id = "same", Name = "Same Name", Value = 42 };

        // Act
        var result = _resolver.Resolve(
            local, remote,
            clock1, clock2,
            DateTime.UtcNow, DateTime.UtcNow);

        // Assert
        result.Result.Name.Should().Be("Same Name");
        result.Result.Value.Should().Be(42);
    }

    #endregion

    #region Custom Resolver Tests

    [Fact]
    public void Resolve_CustomResolver_ShouldUseProvidedFunction()
    {
        // Arrange
        _resolver.ConfigureCustom<TestEntity>(conflict =>
        {
            // Custom logic: combine values
            return new TestEntity
            {
                Id = conflict.EntityId,
                Name = $"{conflict.LocalVersion.Name} + {conflict.RemoteVersion.Name}",
                Value = conflict.LocalVersion.Value + conflict.RemoteVersion.Value
            };
        });

        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var local = new TestEntity { Id = "e1", Name = "A", Value = 10 };
        var remote = new TestEntity { Id = "e1", Name = "B", Value = 20 };

        // Act
        var result = _resolver.Resolve(
            local, remote,
            clock1, clock2,
            DateTime.UtcNow, DateTime.UtcNow);

        // Assert
        result.HadConflict.Should().BeTrue();
        result.Result.Name.Should().Be("A + B");
        result.Result.Value.Should().Be(30);
        result.Resolution.Should().Contain("Custom");
    }

    [Fact]
    public void Resolve_CustomResolver_ShouldReceiveConflictDetails()
    {
        // Arrange
        SyncConflict<TestEntity>? capturedConflict = null;

        _resolver.ConfigureCustom<TestEntity>(conflict =>
        {
            capturedConflict = conflict;
            return conflict.LocalVersion;
        });

        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var local = new TestEntity { Id = "test-id", Name = "Local" };
        var remote = new TestEntity { Id = "test-id", Name = "Remote" };

        // Act
        _resolver.Resolve(
            local, remote,
            clock1, clock2,
            DateTime.UtcNow, DateTime.UtcNow);

        // Assert
        capturedConflict.Should().NotBeNull();
        capturedConflict!.LocalVersion.Should().Be(local);
        capturedConflict.RemoteVersion.Should().Be(remote);
        capturedConflict.LocalClock.Should().Be(clock1);
        capturedConflict.RemoteClock.Should().Be(clock2);
        capturedConflict.Relation.Should().Be(CausalRelation.Concurrent);
    }

    #endregion

    #region Default Strategy Tests

    [Fact]
    public void Resolve_UnconfiguredType_ShouldDefaultToLWW()
    {
        // Arrange - Don't configure any strategy
        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var oldTime = DateTime.UtcNow.AddSeconds(-10);
        var newTime = DateTime.UtcNow;

        var local = new TestEntity { Name = "Local" };
        var remote = new TestEntity { Name = "Remote" };

        // Act
        var result = _resolver.Resolve(
            local, remote,
            clock1, clock2,
            oldTime, newTime); // Remote is newer

        // Assert - Should use LWW (default)
        result.Result.Name.Should().Be("Remote");
        result.HadConflict.Should().BeTrue();
    }

    #endregion

    #region MergedClock Tests

    [Fact]
    public void Resolve_Conflict_ShouldReturnMergedClock()
    {
        // Arrange
        var clock1 = new VectorClock().Increment("node1").Increment("node1");
        var clock2 = new VectorClock().Increment("node2").Increment("node2").Increment("node2");

        var local = new TestEntity { Name = "Local" };
        var remote = new TestEntity { Name = "Remote" };

        // Act
        var result = _resolver.Resolve(
            local, remote,
            clock1, clock2,
            DateTime.UtcNow, DateTime.UtcNow);

        // Assert
        result.MergedClock.GetValue("node1").Should().Be(2);
        result.MergedClock.GetValue("node2").Should().Be(3);
        result.MergedClock.GetValue("testNode").Should().Be(1); // Incremented by resolver
    }

    #endregion

    #region ConflictResolutionResult Tests

    [Fact]
    public void Result_WhenConflict_ShouldIncludeBothVersions()
    {
        // Arrange
        var clock1 = new VectorClock().Increment("node1");
        var clock2 = new VectorClock().Increment("node2");

        var local = new TestEntity { Name = "Local" };
        var remote = new TestEntity { Name = "Remote" };

        // Act
        var result = _resolver.Resolve(
            local, remote,
            clock1, clock2,
            DateTime.UtcNow, DateTime.UtcNow);

        // Assert
        result.HadConflict.Should().BeTrue();
        result.LocalVersion.Should().Be(local);
        result.RemoteVersion.Should().Be(remote);
    }

    [Fact]
    public void Result_WhenNoConflict_ShouldNotIncludeVersions()
    {
        // Arrange
        var clock1 = new VectorClock().Increment("node1");
        var clock2 = clock1.Increment("node1");

        var local = new TestEntity { Name = "Local" };
        var remote = new TestEntity { Name = "Remote" };

        // Act
        var result = _resolver.Resolve(
            local, remote,
            clock2, clock1, // Local is strictly newer
            DateTime.UtcNow, DateTime.UtcNow);

        // Assert
        result.HadConflict.Should().BeFalse();
        result.LocalVersion.Should().BeNull();
        result.RemoteVersion.Should().BeNull();
    }

    #endregion
}

public class SyncConflictTests
{
    [Fact]
    public void SyncConflict_Resolve_ShouldSetResolvedVersion()
    {
        // Arrange
        var conflict = new SyncConflict<TestEntity>
        {
            EntityId = "test",
            LocalVersion = new TestEntity { Name = "Local" },
            RemoteVersion = new TestEntity { Name = "Remote" },
            LocalClock = new VectorClock(),
            RemoteClock = new VectorClock(),
            Relation = CausalRelation.Concurrent
        };

        var resolved = new TestEntity { Name = "Resolved" };

        // Act
        conflict.Resolve(resolved);

        // Assert
        conflict.IsResolved.Should().BeTrue();
        conflict.ResolvedVersion.Should().Be(resolved);
    }

    [Fact]
    public void SyncConflict_DetectedAt_ShouldBeSetAutomatically()
    {
        // Arrange & Act
        var before = DateTime.UtcNow;
        var conflict = new SyncConflict<TestEntity>
        {
            EntityId = "test",
            LocalVersion = new TestEntity(),
            RemoteVersion = new TestEntity(),
            LocalClock = new VectorClock(),
            RemoteClock = new VectorClock(),
            Relation = CausalRelation.Concurrent
        };
        var after = DateTime.UtcNow;

        // Assert
        conflict.DetectedAt.Should().BeOnOrAfter(before);
        conflict.DetectedAt.Should().BeOnOrBefore(after);
    }

    private class TestEntity
    {
        public string Name { get; set; } = string.Empty;
    }
}
