namespace Arcana.Domain.Entities.Identity;

/// <summary>
/// Direct user permission assignment (in addition to role-based permissions).
/// </summary>
public class UserPermission : BaseEntity
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
    /// Permission ID.
    /// </summary>
    public int PermissionId { get; set; }

    /// <summary>
    /// Permission navigation property.
    /// </summary>
    public AppPermission Permission { get; set; } = null!;

    /// <summary>
    /// Whether this is a grant (true) or deny (false) permission.
    /// </summary>
    public bool IsGranted { get; set; } = true;

    /// <summary>
    /// When this permission was assigned.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who assigned this permission.
    /// </summary>
    public string? AssignedBy { get; set; }

    /// <summary>
    /// Optional expiration for temporary permission assignments.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
