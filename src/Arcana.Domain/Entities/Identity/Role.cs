namespace Arcana.Domain.Entities.Identity;

/// <summary>
/// Role entity for role-based access control.
/// 角色實體用於角色型存取控制
/// </summary>
public class Role : BaseEntity
{
    /// <summary>
    /// Unique role name (e.g., "Admin", "Manager", "User").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Role description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is a system role that cannot be deleted.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Role priority for permission resolution (higher = more privileged).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Users assigned to this role.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Permissions assigned to this role.
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Predefined system roles.
/// </summary>
public static class SystemRoles
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string User = "User";
    public const string Guest = "Guest";
}
