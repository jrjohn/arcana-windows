using System.Reflection;
using System.Text.Json;

namespace Arcana.Sync.Crdt;

/// <summary>
/// Conflict resolution strategy.
/// </summary>
public enum ConflictResolutionStrategy
{
    /// <summary>Last writer wins based on timestamp.</summary>
    LastWriterWins,
    /// <summary>First writer wins based on timestamp.</summary>
    FirstWriterWins,
    /// <summary>Merge field by field using LWW for each field.</summary>
    FieldLevelMerge,
    /// <summary>Keep both versions for manual resolution.</summary>
    KeepBoth,
    /// <summary>Use custom resolver function.</summary>
    Custom
}

/// <summary>
/// Represents a conflict between two versions of an entity.
/// </summary>
public class SyncConflict<T> where T : class
{
    public required string EntityId { get; init; }
    public required T LocalVersion { get; init; }
    public required T RemoteVersion { get; init; }
    public required VectorClock LocalClock { get; init; }
    public required VectorClock RemoteClock { get; init; }
    public required CausalRelation Relation { get; init; }
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
    public bool IsResolved { get; private set; }
    public T? ResolvedVersion { get; private set; }

    public void Resolve(T resolved)
    {
        ResolvedVersion = resolved;
        IsResolved = true;
    }
}

/// <summary>
/// Conflict resolution service for sync operations.
/// </summary>
public class ConflictResolver
{
    private readonly string _nodeId;
    private readonly Dictionary<Type, ConflictResolutionStrategy> _strategies = new();
    private readonly Dictionary<Type, Delegate> _customResolvers = new();

    public ConflictResolver(string nodeId)
    {
        _nodeId = nodeId;
    }

    /// <summary>
    /// Configures the resolution strategy for a specific entity type.
    /// </summary>
    public void Configure<T>(ConflictResolutionStrategy strategy) where T : class
    {
        _strategies[typeof(T)] = strategy;
    }

    /// <summary>
    /// Configures a custom resolver for a specific entity type.
    /// </summary>
    public void ConfigureCustom<T>(Func<SyncConflict<T>, T> resolver) where T : class
    {
        _strategies[typeof(T)] = ConflictResolutionStrategy.Custom;
        _customResolvers[typeof(T)] = resolver;
    }

    /// <summary>
    /// Resolves a conflict between local and remote versions.
    /// </summary>
    public ConflictResolutionResult<T> Resolve<T>(
        T local,
        T remote,
        VectorClock localClock,
        VectorClock remoteClock,
        DateTime localTimestamp,
        DateTime remoteTimestamp) where T : class
    {
        var relation = localClock.CompareTo(remoteClock);

        // If causally ordered, no conflict
        if (relation == CausalRelation.HappenedAfter)
        {
            return new ConflictResolutionResult<T>
            {
                Result = local,
                MergedClock = localClock,
                HadConflict = false,
                Resolution = "Local version is newer"
            };
        }

        if (relation == CausalRelation.HappenedBefore)
        {
            return new ConflictResolutionResult<T>
            {
                Result = remote,
                MergedClock = remoteClock,
                HadConflict = false,
                Resolution = "Remote version is newer"
            };
        }

        if (relation == CausalRelation.Equal)
        {
            return new ConflictResolutionResult<T>
            {
                Result = local,
                MergedClock = localClock,
                HadConflict = false,
                Resolution = "Versions are identical"
            };
        }

        // Concurrent - actual conflict
        var strategy = _strategies.GetValueOrDefault(typeof(T), ConflictResolutionStrategy.LastWriterWins);
        var mergedClock = localClock.Merge(remoteClock).Increment(_nodeId);

        var conflict = new SyncConflict<T>
        {
            EntityId = GetEntityId(local),
            LocalVersion = local,
            RemoteVersion = remote,
            LocalClock = localClock,
            RemoteClock = remoteClock,
            Relation = relation
        };

        T resolved = strategy switch
        {
            ConflictResolutionStrategy.LastWriterWins =>
                ResolveLastWriterWins(local, remote, localTimestamp, remoteTimestamp),

            ConflictResolutionStrategy.FirstWriterWins =>
                ResolveFirstWriterWins(local, remote, localTimestamp, remoteTimestamp),

            ConflictResolutionStrategy.FieldLevelMerge =>
                ResolveFieldLevel(local, remote, localTimestamp, remoteTimestamp),

            ConflictResolutionStrategy.Custom when _customResolvers.TryGetValue(typeof(T), out var resolver) =>
                ((Func<SyncConflict<T>, T>)resolver)(conflict),

            _ => ResolveLastWriterWins(local, remote, localTimestamp, remoteTimestamp)
        };

        return new ConflictResolutionResult<T>
        {
            Result = resolved,
            MergedClock = mergedClock,
            HadConflict = true,
            Resolution = $"Resolved using {strategy}",
            LocalVersion = local,
            RemoteVersion = remote
        };
    }

    private T ResolveLastWriterWins<T>(T local, T remote, DateTime localTimestamp, DateTime remoteTimestamp) where T : class
    {
        if (remoteTimestamp > localTimestamp)
            return remote;
        if (localTimestamp > remoteTimestamp)
            return local;
        // Same timestamp - use node ID for deterministic ordering
        return string.Compare(GetEntityId(local), GetEntityId(remote), StringComparison.Ordinal) > 0 ? local : remote;
    }

    private T ResolveFirstWriterWins<T>(T local, T remote, DateTime localTimestamp, DateTime remoteTimestamp) where T : class
    {
        if (remoteTimestamp < localTimestamp)
            return remote;
        if (localTimestamp < remoteTimestamp)
            return local;
        return string.Compare(GetEntityId(local), GetEntityId(remote), StringComparison.Ordinal) < 0 ? local : remote;
    }

    private T ResolveFieldLevel<T>(T local, T remote, DateTime localTimestamp, DateTime remoteTimestamp) where T : class
    {
        var type = typeof(T);
        var result = Activator.CreateInstance<T>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite) continue;

            var localValue = prop.GetValue(local);
            var remoteValue = prop.GetValue(remote);

            // If values are the same, use either
            if (Equals(localValue, remoteValue))
            {
                prop.SetValue(result, localValue);
                continue;
            }

            // Check for field-level timestamp attribute
            var fieldTimestamp = GetFieldTimestamp(local, remote, prop.Name, localTimestamp, remoteTimestamp);

            // Use LWW for this field
            var value = fieldTimestamp.localTime >= fieldTimestamp.remoteTime ? localValue : remoteValue;
            prop.SetValue(result, value);
        }

        return result;
    }

    private (DateTime localTime, DateTime remoteTime) GetFieldTimestamp<T>(
        T local, T remote, string fieldName,
        DateTime localTimestamp, DateTime remoteTimestamp) where T : class
    {
        // Try to find field-level timestamps (e.g., {FieldName}ModifiedAt property)
        var type = typeof(T);
        var timestampProp = type.GetProperty($"{fieldName}ModifiedAt");

        if (timestampProp != null && timestampProp.PropertyType == typeof(DateTime))
        {
            var localFieldTime = (DateTime?)timestampProp.GetValue(local) ?? localTimestamp;
            var remoteFieldTime = (DateTime?)timestampProp.GetValue(remote) ?? remoteTimestamp;
            return (localFieldTime, remoteFieldTime);
        }

        return (localTimestamp, remoteTimestamp);
    }

    private string GetEntityId<T>(T entity) where T : class
    {
        var idProp = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty("EntityId");
        return idProp?.GetValue(entity)?.ToString() ?? Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Result of conflict resolution.
/// </summary>
public class ConflictResolutionResult<T> where T : class
{
    public required T Result { get; init; }
    public required VectorClock MergedClock { get; init; }
    public required bool HadConflict { get; init; }
    public required string Resolution { get; init; }
    public T? LocalVersion { get; init; }
    public T? RemoteVersion { get; init; }
}

/// <summary>
/// Attribute to mark fields that should use specific merge strategies.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MergeStrategyAttribute : Attribute
{
    public ConflictResolutionStrategy Strategy { get; }

    public MergeStrategyAttribute(ConflictResolutionStrategy strategy)
    {
        Strategy = strategy;
    }
}

/// <summary>
/// Attribute to mark fields that should always take the local value.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class LocalOnlyAttribute : Attribute { }

/// <summary>
/// Attribute to mark fields that should always take the remote value.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RemoteOnlyAttribute : Attribute { }
