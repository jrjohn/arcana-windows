using Arcana.Core.Common;

namespace Arcana.Core.Security;

/// <summary>
/// Authorization service for checking permissions.
/// 授權服務用於檢查權限
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Check if a user has a specific permission.
    /// </summary>
    Task<bool> HasPermissionAsync(int userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user has any of the specified permissions.
    /// </summary>
    Task<bool> HasAnyPermissionAsync(int userId, IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user has all of the specified permissions.
    /// </summary>
    Task<bool> HasAllPermissionsAsync(int userId, IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user is in a specific role.
    /// </summary>
    Task<bool> IsInRoleAsync(int userId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user is in any of the specified roles.
    /// </summary>
    Task<bool> IsInAnyRoleAsync(int userId, IEnumerable<string> roles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all permissions for a user (from roles and direct assignments).
    /// </summary>
    Task<IReadOnlySet<string>> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all roles for a user.
    /// </summary>
    Task<IReadOnlyList<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authorize an action and return result.
    /// </summary>
    Task<Result<bool>> AuthorizeAsync(int userId, string permission, string? resource = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Attribute to mark methods/classes requiring specific permissions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequiresPermissionAttribute : Attribute
{
    public string Permission { get; }
    public string? Resource { get; }

    public RequiresPermissionAttribute(string permission, string? resource = null)
    {
        Permission = permission;
        Resource = resource;
    }
}

/// <summary>
/// Attribute to mark methods/classes requiring specific roles.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequiresRoleAttribute : Attribute
{
    public string Role { get; }

    public RequiresRoleAttribute(string role)
    {
        Role = role;
    }
}
