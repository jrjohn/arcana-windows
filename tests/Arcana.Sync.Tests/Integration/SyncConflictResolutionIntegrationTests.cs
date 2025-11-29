using Arcana.Sync.Crdt;
using FluentAssertions;
using Xunit;

namespace Arcana.Sync.Tests.Integration;

/// <summary>
/// Integration tests for the complete sync and conflict resolution workflow.
/// Simulates real-world scenarios with multiple clients and concurrent edits.
/// </summary>
public class SyncConflictResolutionIntegrationTests
{
    #region Multi-Client Sync Scenarios

    [Fact]
    public void TwoClients_SequentialEdits_ShouldMergeCorrectly()
    {
        // Simulate two clients editing sequentially
        var clientA_Id = "client-A";
        var clientB_Id = "client-B";

        // Client A creates document
        var clockA = new VectorClock().Increment(clientA_Id);
        var docA = new Document { Id = "doc-1", Title = "Original", Content = "Initial content" };

        // Client A syncs to Client B
        var clockB = clockA; // B receives A's clock

        // Client B edits (after receiving A's version)
        clockB = clockB.Increment(clientB_Id);
        var docB = docA with { Content = "B's edit" };

        // No conflict - B happened after A
        clockB.CompareTo(clockA).Should().Be(CausalRelation.HappenedAfter);
    }

    [Fact]
    public void TwoClients_ConcurrentEdits_ShouldDetectConflict()
    {
        // Simulate two clients editing concurrently
        var clientA_Id = "client-A";
        var clientB_Id = "client-B";

        // Both clients start from the same state
        var initialClock = new VectorClock();

        // Client A edits offline
        var clockA = initialClock.Increment(clientA_Id);
        var docA = new Document { Id = "doc-1", Title = "A's Title", Content = "A's content" };

        // Client B also edits offline (from same initial state)
        var clockB = initialClock.Increment(clientB_Id);
        var docB = new Document { Id = "doc-1", Title = "B's Title", Content = "B's content" };

        // When syncing - should detect conflict
        clockA.CompareTo(clockB).Should().Be(CausalRelation.Concurrent);
        clockB.CompareTo(clockA).Should().Be(CausalRelation.Concurrent);
    }

    [Fact]
    public void TwoClients_ConcurrentEdits_WithLWWResolution()
    {
        // Setup resolver
        var resolver = new ConflictResolver("server");
        resolver.Configure<Document>(ConflictResolutionStrategy.LastWriterWins);

        var clientA_Id = "client-A";
        var clientB_Id = "client-B";

        // Concurrent edits
        var clockA = new VectorClock().Increment(clientA_Id);
        var clockB = new VectorClock().Increment(clientB_Id);

        var timestampA = DateTime.UtcNow.AddSeconds(-10);
        var timestampB = DateTime.UtcNow; // B is newer

        var docA = new Document { Id = "doc-1", Title = "A's Title", Content = "A's content" };
        var docB = new Document { Id = "doc-1", Title = "B's Title", Content = "B's content" };

        // Act
        var result = resolver.Resolve(docA, docB, clockA, clockB, timestampA, timestampB);

        // Assert - B wins (newer timestamp)
        result.HadConflict.Should().BeTrue();
        result.Result.Title.Should().Be("B's Title");
        result.Result.Content.Should().Be("B's content");
    }

    [Fact]
    public void TwoClients_ConcurrentEdits_WithFieldLevelMerge()
    {
        // Setup resolver with field-level merge
        var resolver = new ConflictResolver("server");
        resolver.Configure<DocumentWithTimestamps>(ConflictResolutionStrategy.FieldLevelMerge);

        var clientA_Id = "client-A";
        var clientB_Id = "client-B";

        var clockA = new VectorClock().Increment(clientA_Id);
        var clockB = new VectorClock().Increment(clientB_Id);

        var baseTime = DateTime.UtcNow;

        // A edits title more recently, B edits content more recently
        var docA = new DocumentWithTimestamps
        {
            Id = "doc-1",
            Title = "A's Title",
            TitleModifiedAt = baseTime.AddSeconds(10), // A's title is newer
            Content = "A's content",
            ContentModifiedAt = baseTime.AddSeconds(-10) // A's content is older
        };

        var docB = new DocumentWithTimestamps
        {
            Id = "doc-1",
            Title = "B's Title",
            TitleModifiedAt = baseTime.AddSeconds(-10), // B's title is older
            Content = "B's content",
            ContentModifiedAt = baseTime.AddSeconds(10) // B's content is newer
        };

        // Act
        var result = resolver.Resolve(docA, docB, clockA, clockB, baseTime, baseTime);

        // Assert - Should merge best of both
        result.HadConflict.Should().BeTrue();
        result.Result.Title.Should().Be("A's Title"); // A's title wins
        result.Result.Content.Should().Be("B's content"); // B's content wins
    }

    #endregion

    #region Three-Way Merge Scenarios

    [Fact]
    public void ThreeClients_ConcurrentEdits_ShouldCreateThreeWayConflict()
    {
        // Three clients all edit from the same base version
        var clientA_Id = "client-A";
        var clientB_Id = "client-B";
        var clientC_Id = "client-C";

        var baseClock = new VectorClock();

        var clockA = baseClock.Increment(clientA_Id);
        var clockB = baseClock.Increment(clientB_Id);
        var clockC = baseClock.Increment(clientC_Id);

        // All are concurrent with each other
        clockA.CompareTo(clockB).Should().Be(CausalRelation.Concurrent);
        clockA.CompareTo(clockC).Should().Be(CausalRelation.Concurrent);
        clockB.CompareTo(clockC).Should().Be(CausalRelation.Concurrent);
    }

    [Fact]
    public void ThreeClients_WithMVRegister_ShouldPreserveAllVersions()
    {
        var register = new MVRegister<string>();

        var clockA = new VectorClock().Increment("client-A");
        var clockB = new VectorClock().Increment("client-B");
        var clockC = new VectorClock().Increment("client-C");

        // All three clients set values concurrently
        register.Set("Version A", clockA);
        register.Set("Version B", clockB);
        register.Set("Version C", clockC);

        // Assert - All three versions preserved
        register.HasConflict.Should().BeTrue();
        register.Values.Should().HaveCount(3);
        register.Values.Select(v => v.Value).Should().Contain("Version A");
        register.Values.Select(v => v.Value).Should().Contain("Version B");
        register.Values.Select(v => v.Value).Should().Contain("Version C");

        // Resolve by merging
        var mergedClock = clockA.Merge(clockB).Merge(clockC).Increment("resolver");
        register.Resolve("Merged by user", mergedClock);

        register.HasConflict.Should().BeFalse();
        register.SingleValue.Should().Be("Merged by user");
    }

    #endregion

    #region Offline Sync Scenarios

    [Fact]
    public void OfflineClient_MultipleEdits_ThenSync()
    {
        var onlineClient = "online";
        var offlineClient = "offline";

        // Initial state
        var baseClock = new VectorClock().Increment(onlineClient);

        // Online client makes edits
        var onlineClock = baseClock.Increment(onlineClient).Increment(onlineClient);

        // Offline client makes multiple edits from base state
        var offlineClock = baseClock
            .Increment(offlineClient)
            .Increment(offlineClient)
            .Increment(offlineClient);

        // They're concurrent
        onlineClock.CompareTo(offlineClock).Should().Be(CausalRelation.Concurrent);

        // After merge, the merged clock dominates both
        var mergedClock = onlineClock.Merge(offlineClock);
        mergedClock.CompareTo(onlineClock).Should().Be(CausalRelation.HappenedAfter);
        mergedClock.CompareTo(offlineClock).Should().Be(CausalRelation.HappenedAfter);
    }

    [Fact]
    public void OfflineClient_SyncsWithServer_ThenContinuesEditing()
    {
        var server = "server";
        var client = "client";

        // Client starts with server state
        var serverClock = new VectorClock().Increment(server);
        var clientClock = serverClock; // Client syncs and gets server clock

        // Client edits offline
        clientClock = clientClock.Increment(client);

        // Server has new edits
        serverClock = serverClock.Increment(server);

        // Conflict when syncing
        clientClock.CompareTo(serverClock).Should().Be(CausalRelation.Concurrent);

        // After sync (merge and resolve)
        var afterSync = serverClock.Merge(clientClock).Increment(client);

        // Now client is ahead of both previous versions
        afterSync.CompareTo(serverClock).Should().Be(CausalRelation.HappenedAfter);
        afterSync.CompareTo(clientClock).Should().Be(CausalRelation.HappenedAfter);
    }

    #endregion

    #region LWW Map Scenarios

    [Fact]
    public void LWWMap_MultipleFieldsEditedByConcurrentClients()
    {
        var baseTime = DateTime.UtcNow;

        // Client A edits some fields
        var mapA = new LWWMap();
        mapA.Set("title", "A's Title", baseTime.AddSeconds(10), "client-A");
        mapA.Set("author", "A's Author", baseTime.AddSeconds(5), "client-A");
        mapA.Set("content", "A's Content", baseTime.AddSeconds(-10), "client-A");

        // Client B edits some fields (different times)
        var mapB = new LWWMap();
        mapB.Set("title", "B's Title", baseTime.AddSeconds(-10), "client-B");
        mapB.Set("author", "B's Author", baseTime.AddSeconds(20), "client-B");
        mapB.Set("content", "B's Content", baseTime.AddSeconds(10), "client-B");

        // Merge
        var merged = mapA.Merge(mapB);

        // Assert - Each field has its own winner
        merged.Get<string>("title").Should().Be("A's Title"); // A newer
        merged.Get<string>("author").Should().Be("B's Author"); // B newer
        merged.Get<string>("content").Should().Be("B's Content"); // B newer
    }

    [Fact]
    public void LWWMap_ContinuousMerging_ShouldConverge()
    {
        var baseTime = DateTime.UtcNow;

        // Simulate continuous syncing between 3 clients
        var map1 = new LWWMap();
        var map2 = new LWWMap();
        var map3 = new LWWMap();

        // Round 1: Each client makes edits
        map1.Set("field", "value1", baseTime, "client1");
        map2.Set("field", "value2", baseTime.AddSeconds(1), "client2");
        map3.Set("field", "value3", baseTime.AddSeconds(2), "client3"); // Newest

        // Round 2: Merge all maps
        var merged12 = map1.Merge(map2);
        var merged = merged12.Merge(map3);

        // All should converge to the newest value
        merged.Get<string>("field").Should().Be("value3");

        // Commutative - order of merge shouldn't matter
        var merged23 = map2.Merge(map3);
        var mergedAlt = merged23.Merge(map1);
        mergedAlt.Get<string>("field").Should().Be("value3");
    }

    #endregion

    #region Custom Resolution Scenarios

    [Fact]
    public void CustomResolver_MergeStrategies()
    {
        var resolver = new ConflictResolver("resolver");

        // Configure custom resolver that combines values
        resolver.ConfigureCustom<Document>(conflict =>
        {
            // Custom merge: concatenate content, use longest title
            var title = conflict.LocalVersion.Title.Length >= conflict.RemoteVersion.Title.Length
                ? conflict.LocalVersion.Title
                : conflict.RemoteVersion.Title;

            return new Document
            {
                Id = conflict.EntityId,
                Title = title,
                Content = $"{conflict.LocalVersion.Content}\n---\n{conflict.RemoteVersion.Content}"
            };
        });

        var clockA = new VectorClock().Increment("A");
        var clockB = new VectorClock().Increment("B");

        var docA = new Document { Id = "doc-1", Title = "Short", Content = "Content A" };
        var docB = new Document { Id = "doc-1", Title = "Much Longer Title", Content = "Content B" };

        // Act
        var result = resolver.Resolve(docA, docB, clockA, clockB, DateTime.UtcNow, DateTime.UtcNow);

        // Assert
        result.HadConflict.Should().BeTrue();
        result.Result.Title.Should().Be("Much Longer Title"); // Longer title wins
        result.Result.Content.Should().Contain("Content A");
        result.Result.Content.Should().Contain("Content B");
    }

    #endregion

    #region Vector Clock Edge Cases

    [Fact]
    public void VectorClock_ChainOfCausality()
    {
        // A → B → C (chain of causality)
        var clockA = new VectorClock().Increment("A");

        // B receives A's message and responds
        var clockB = clockA.Merge(new VectorClock()).Increment("B");

        // C receives B's message and responds
        var clockC = clockB.Merge(new VectorClock()).Increment("C");

        // Verify causality chain
        clockA.CompareTo(clockB).Should().Be(CausalRelation.HappenedBefore);
        clockB.CompareTo(clockC).Should().Be(CausalRelation.HappenedBefore);
        clockA.CompareTo(clockC).Should().Be(CausalRelation.HappenedBefore);

        // Transitivity: C happened after A
        clockC.CompareTo(clockA).Should().Be(CausalRelation.HappenedAfter);
    }

    [Fact]
    public void VectorClock_PartialOrder()
    {
        // A and B start independent work
        var clockA1 = new VectorClock().Increment("A");
        var clockB1 = new VectorClock().Increment("B");

        // They're concurrent
        clockA1.CompareTo(clockB1).Should().Be(CausalRelation.Concurrent);

        // A sends message to C (C knows about A)
        var clockC1 = clockA1.Merge(new VectorClock()).Increment("C");

        // C knows about A but not B
        clockC1.CompareTo(clockA1).Should().Be(CausalRelation.HappenedAfter);
        clockC1.CompareTo(clockB1).Should().Be(CausalRelation.Concurrent);

        // B sends message to D
        var clockD1 = clockB1.Merge(new VectorClock()).Increment("D");

        // D knows about B but not A or C
        clockD1.CompareTo(clockB1).Should().Be(CausalRelation.HappenedAfter);
        clockD1.CompareTo(clockA1).Should().Be(CausalRelation.Concurrent);
        clockD1.CompareTo(clockC1).Should().Be(CausalRelation.Concurrent);
    }

    #endregion

    #region Test Entities

    private record Document
    {
        public string Id { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
    }

    private class DocumentWithTimestamps
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime TitleModifiedAt { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime ContentModifiedAt { get; set; }
    }

    #endregion
}
