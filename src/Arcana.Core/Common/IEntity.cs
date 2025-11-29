namespace Arcana.Core.Common;

/// <summary>
/// Base interface for all entities with a unique identifier.
/// </summary>
/// <typeparam name="TKey">The type of the entity's identifier</typeparam>
public interface IEntity<TKey> where TKey : notnull
{
    /// <summary>
    /// The unique identifier of the entity
    /// </summary>
    TKey Id { get; }
}

/// <summary>
/// Base interface for entities with auditing information.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// When the entity was created
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Who created the entity
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// When the entity was last modified
    /// </summary>
    DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Who last modified the entity
    /// </summary>
    string? ModifiedBy { get; set; }
}

/// <summary>
/// Base interface for soft-deletable entities.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Whether the entity has been deleted
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// When the entity was deleted
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Who deleted the entity
    /// </summary>
    string? DeletedBy { get; set; }
}

/// <summary>
/// Base interface for entities that support optimistic concurrency.
/// </summary>
public interface IConcurrencyAware
{
    /// <summary>
    /// Row version for optimistic concurrency
    /// </summary>
    byte[] RowVersion { get; set; }
}

/// <summary>
/// Base interface for syncable entities.
/// </summary>
public interface ISyncable
{
    /// <summary>
    /// Global unique identifier for sync
    /// </summary>
    Guid SyncId { get; set; }

    /// <summary>
    /// Last sync timestamp
    /// </summary>
    DateTime? LastSyncAt { get; set; }

    /// <summary>
    /// Whether changes are pending sync
    /// </summary>
    bool IsPendingSync { get; set; }
}

/// <summary>
/// Combined base entity interface with all common features.
/// </summary>
public interface IFullEntity<TKey> : IEntity<TKey>, IAuditableEntity, ISoftDeletable, IConcurrencyAware, ISyncable
    where TKey : notnull
{
}
