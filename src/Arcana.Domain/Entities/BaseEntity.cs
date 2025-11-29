using Arcana.Core.Common;

namespace Arcana.Domain.Entities;

/// <summary>
/// Base entity with common properties for all domain entities.
/// </summary>
public abstract class BaseEntity<TKey> : IEntity<TKey>, IAuditableEntity, ISoftDeletable, ISyncable
    where TKey : notnull
{
    public abstract TKey Id { get; set; }

    // IAuditableEntity
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // ISyncable
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public DateTime? LastSyncAt { get; set; }
    public bool IsPendingSync { get; set; } = true;
}

/// <summary>
/// Base entity with int primary key.
/// </summary>
public abstract class BaseEntity : BaseEntity<int>
{
    public override int Id { get; set; }
}

/// <summary>
/// Base entity with Guid primary key.
/// </summary>
public abstract class BaseGuidEntity : BaseEntity<Guid>
{
    public override Guid Id { get; set; } = Guid.NewGuid();
}
