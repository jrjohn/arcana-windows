namespace Arcana.Domain.Entities.Identity;

/// <summary>
/// Junction entity for Role-Permission many-to-many relationship.
/// 角色權限關聯實體
/// </summary>
public class RolePermission : BaseEntity
{
    /// <summary>
    /// Role ID.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Role navigation property.
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Permission ID.
    /// </summary>
    public int PermissionId { get; set; }

    /// <summary>
    /// Permission navigation property.
    /// </summary>
    public AppPermission Permission { get; set; } = null!;

    /// <summary>
    /// When this permission was granted to the role.
    /// </summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who granted this permission.
    /// </summary>
    public string? GrantedBy { get; set; }
}
