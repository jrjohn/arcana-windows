namespace Arcana.Domain.Entities.Identity;

/// <summary>
/// User entity for authentication and authorization.
/// 使用者實體用於身份驗證和授權
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Unique username for login.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User's email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Hashed password (BCrypt or similar).
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Salt used for password hashing (if applicable).
    /// </summary>
    public string? PasswordSalt { get; set; }

    /// <summary>
    /// Whether the user account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the user account is locked due to failed attempts.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// When the account was locked.
    /// </summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>
    /// Number of consecutive failed login attempts.
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// Last successful login timestamp.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Last password change timestamp.
    /// </summary>
    public DateTime? PasswordChangedAt { get; set; }

    /// <summary>
    /// Whether password must be changed on next login.
    /// </summary>
    public bool MustChangePassword { get; set; }

    /// <summary>
    /// Refresh token for session management.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Refresh token expiration time.
    /// </summary>
    public DateTime? RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// User's roles.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// User's direct permissions (in addition to role permissions).
    /// </summary>
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
