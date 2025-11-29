namespace Arcana.Sync.Crdt;

/// <summary>
/// Interface for entities that support CRDT-based synchronization.
/// 支援 CRDT 同步的實體介面
/// </summary>
public interface ISyncableEntity
{
    /// <summary>
    /// Gets or sets the entity ID.
    /// </summary>
    string SyncId { get; }

    /// <summary>
    /// Gets or sets the vector clock for this entity.
    /// </summary>
    string? VectorClockJson { get; set; }

    /// <summary>
    /// Gets or sets the last modified timestamp.
    /// </summary>
    DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the node ID that last modified this entity.
    /// </summary>
    string? ModifiedByNodeId { get; set; }

    /// <summary>
    /// Gets or sets whether this entity has unresolved conflicts.
    /// </summary>
    bool HasConflict { get; set; }

    /// <summary>
    /// Gets or sets the conflicting version as JSON (if any).
    /// </summary>
    string? ConflictingVersionJson { get; set; }
}

/// <summary>
/// Base implementation for syncable entities.
/// </summary>
public abstract class SyncableEntityBase : ISyncableEntity
{
    public abstract string SyncId { get; }

    public string? VectorClockJson { get; set; }

    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    public string? ModifiedByNodeId { get; set; }

    public bool HasConflict { get; set; }

    public string? ConflictingVersionJson { get; set; }

    /// <summary>
    /// Gets the vector clock for this entity.
    /// </summary>
    public VectorClock GetVectorClock()
    {
        if (string.IsNullOrEmpty(VectorClockJson))
            return new VectorClock();

        return VectorClock.Deserialize(VectorClockJson);
    }

    /// <summary>
    /// Sets the vector clock for this entity.
    /// </summary>
    public void SetVectorClock(VectorClock clock)
    {
        VectorClockJson = clock.Serialize();
    }

    /// <summary>
    /// Increments the vector clock for the given node.
    /// </summary>
    public void IncrementClock(string nodeId)
    {
        var clock = GetVectorClock().Increment(nodeId);
        SetVectorClock(clock);
        ModifiedByNodeId = nodeId;
        ModifiedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Sync metadata for tracking entity synchronization state.
/// </summary>
public class SyncMetadata
{
    public required string EntityType { get; init; }
    public required string EntityId { get; init; }
    public required string VectorClockJson { get; init; }
    public required DateTime ModifiedAt { get; init; }
    public required string ModifiedByNodeId { get; init; }
    public SyncOperationType Operation { get; init; }
    public bool IsSynced { get; set; }
    public DateTime? SyncedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Sync conflict record for manual resolution.
/// </summary>
public class SyncConflictRecord
{
    public int Id { get; set; }
    public required string EntityType { get; init; }
    public required string EntityId { get; init; }
    public required string LocalVersionJson { get; init; }
    public required string RemoteVersionJson { get; init; }
    public required string LocalClockJson { get; init; }
    public required string RemoteClockJson { get; init; }
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedByNodeId { get; set; }
    public string? ResolutionStrategy { get; set; }
}
