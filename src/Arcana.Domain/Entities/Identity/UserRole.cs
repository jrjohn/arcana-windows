namespace Arcana.Domain.Entities.Identity;

/// <summary>
/// Junction entity for User-Role many-to-many relationship.
/// </summary>
public class UserRole : BaseEntity
{
    /// <summary>
    /// User ID.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// User navigation property.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Role ID.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Role navigation property.
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// When this role assignment was granted.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who assigned this role.
    /// </summary>
    public string? AssignedBy { get; set; }

    /// <summary>
    /// Optional expiration for temporary role assignments.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
