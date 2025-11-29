using Arcana.Core.Security;

namespace Arcana.Infrastructure.Security;

/// <summary>
/// Thread-safe current user service for desktop application.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly object _lock = new();
    private AuthenticatedUser? _currentUser;

    public bool IsAuthenticated => _currentUser != null;

    public AuthenticatedUser? CurrentUser
    {
        get
        {
            lock (_lock)
            {
                return _currentUser;
            }
        }
    }

    public int? UserId => CurrentUser?.Id;

    public string? Username => CurrentUser?.Username;

    public event EventHandler<CurrentUserChangedEventArgs>? CurrentUserChanged;

    public void SetCurrentUser(AuthenticatedUser? user)
    {
        AuthenticatedUser? previousUser;

        lock (_lock)
        {
            previousUser = _currentUser;
            _currentUser = user;
        }

        CurrentUserChanged?.Invoke(this, new CurrentUserChangedEventArgs
        {
            PreviousUser = previousUser,
            NewUser = user
        });
    }

    public void ClearCurrentUser()
    {
        SetCurrentUser(null);
    }

    public bool HasPermission(string permission)
    {
        var user = CurrentUser;
        if (user == null)
            return false;

        return user.Permissions.Contains(permission);
    }

    public bool HasAnyPermission(params string[] permissions)
    {
        var user = CurrentUser;
        if (user == null)
            return false;

        return permissions.Any(p => user.Permissions.Contains(p));
    }

    public bool HasAllPermissions(params string[] permissions)
    {
        var user = CurrentUser;
        if (user == null)
            return false;

        return permissions.All(p => user.Permissions.Contains(p));
    }

    public bool IsInRole(string role)
    {
        var user = CurrentUser;
        if (user == null)
            return false;

        return user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsInAnyRole(params string[] roles)
    {
        var user = CurrentUser;
        if (user == null)
            return false;

        return roles.Any(r => user.Roles.Contains(r, StringComparer.OrdinalIgnoreCase));
    }
}
