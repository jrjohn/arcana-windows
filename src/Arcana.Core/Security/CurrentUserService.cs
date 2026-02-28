namespace Arcana.Core.Security;

/// <summary>
/// Service to access the current authenticated user.
/// </summary>
public interface CurrentUserService
{
    /// <summary>
    /// Whether a user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// The current authenticated user (null if not authenticated).
    /// </summary>
    AuthenticatedUser? CurrentUser { get; }

    /// <summary>
    /// The current user's ID (null if not authenticated).
    /// </summary>
    int? UserId { get; }

    /// <summary>
    /// The current user's username (null if not authenticated).
    /// </summary>
    string? Username { get; }

    /// <summary>
    /// Set the current authenticated user.
    /// </summary>
    void SetCurrentUser(AuthenticatedUser? user);

    /// <summary>
    /// Clear the current user (logout).
    /// </summary>
    void ClearCurrentUser();

    /// <summary>
    /// Check if current user has a specific permission.
    /// </summary>
    bool HasPermission(string permission);

    /// <summary>
    /// Check if current user has any of the specified permissions.
    /// </summary>
    bool HasAnyPermission(params string[] permissions);

    /// <summary>
    /// Check if current user has all of the specified permissions.
    /// </summary>
    bool HasAllPermissions(params string[] permissions);

    /// <summary>
    /// Check if current user is in a specific role.
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Check if current user is in any of the specified roles.
    /// </summary>
    bool IsInAnyRole(params string[] roles);

    /// <summary>
    /// Event raised when the current user changes.
    /// </summary>
    event EventHandler<CurrentUserChangedEventArgs>? CurrentUserChanged;
}

/// <summary>
/// Event args for current user change.
/// </summary>
public class CurrentUserChangedEventArgs : EventArgs
{
    public AuthenticatedUser? PreviousUser { get; init; }
    public AuthenticatedUser? NewUser { get; init; }
}
