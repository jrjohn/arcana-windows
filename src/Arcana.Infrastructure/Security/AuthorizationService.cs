using Arcana.Core.Common;
using Arcana.Core.Security;
using Arcana.Data.Local;
using Arcana.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Arcana.Infrastructure.Security;

/// <summary>
/// Authorization service implementation.
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(AppDbContext context, ILogger<AuthorizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(int userId, string permission, CancellationToken cancellationToken = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return permissions.Contains(permission);
    }

    public async Task<bool> HasAnyPermissionAsync(int userId, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        var userPermissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return permissions.Any(p => userPermissions.Contains(p));
    }

    public async Task<bool> HasAllPermissionsAsync(int userId, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        var userPermissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return permissions.All(p => userPermissions.Contains(p));
    }

    public async Task<bool> IsInRoleAsync(int userId, string role, CancellationToken cancellationToken = default)
    {
        var roles = await GetUserRolesAsync(userId, cancellationToken);
        return roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> IsInAnyRoleAsync(int userId, IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        var userRoles = await GetUserRolesAsync(userId, cancellationToken);
        return roles.Any(r => userRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlySet<string>> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            return new HashSet<string>();
        }

        var permissions = new HashSet<string>();

        // Add permissions from roles
        foreach (var userRole in user.UserRoles.Where(ur => !ur.IsDeleted && (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow)))
        {
            foreach (var rp in userRole.Role.RolePermissions.Where(rp => !rp.IsDeleted))
            {
                permissions.Add(rp.Permission.Code);
            }
        }

        // Add/remove direct user permissions
        foreach (var up in user.UserPermissions.Where(up => !up.IsDeleted && (up.ExpiresAt == null || up.ExpiresAt > DateTime.UtcNow)))
        {
            if (up.IsGranted)
                permissions.Add(up.Permission.Code);
            else
                permissions.Remove(up.Permission.Code);
        }

        return permissions;
    }

    public async Task<IReadOnlyList<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == userId &&
                         !ur.IsDeleted &&
                         (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow))
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync(cancellationToken);

        return roles;
    }

    public async Task<Result<bool>> AuthorizeAsync(int userId, string permission, string? resource = null, CancellationToken cancellationToken = default)
    {
        var hasPermission = await HasPermissionAsync(userId, permission, cancellationToken);

        if (!hasPermission)
        {
            _logger.LogWarning("Authorization denied for user {UserId} on permission {Permission}, resource: {Resource}",
                userId, permission, resource);

            // Log access denied
            var audit = new AuditLog
            {
                EventType = AuditEventType.AccessDenied,
                UserId = userId,
                Resource = resource,
                Action = permission,
                IsSuccess = false,
                Timestamp = DateTime.UtcNow
            };
            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync(cancellationToken);

            return Result<bool>.Failure(new AppError.Auth(ErrorCode.Forbidden, $"Access denied: {permission}"));
        }

        return Result<bool>.Success(true);
    }
}
