using Arcana.Core.Common;

namespace Arcana.Core.Security;

/// <summary>
/// Authentication service interface.
/// 身份驗證服務介面
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticate user with username and password.
    /// </summary>
    Task<Result<AuthResult>> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate and refresh an existing token.
    /// </summary>
    Task<Result<AuthResult>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout the current user and invalidate tokens.
    /// </summary>
    Task<Result<bool>> LogoutAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change user password.
    /// </summary>
    Task<Result<bool>> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset user password (admin action).
    /// </summary>
    Task<Result<string>> ResetPasswordAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate if a token is still valid.
    /// </summary>
    Task<Result<AuthenticatedUser>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lock a user account.
    /// </summary>
    Task<Result<bool>> LockAccountAsync(int userId, TimeSpan? duration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlock a user account.
    /// </summary>
    Task<Result<bool>> UnlockAccountAsync(int userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of authentication operation.
/// </summary>
public record AuthResult
{
    /// <summary>
    /// The authenticated user information.
    /// </summary>
    public required AuthenticatedUser User { get; init; }

    /// <summary>
    /// Access token for API calls.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Refresh token for obtaining new access tokens.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// When the access token expires.
    /// </summary>
    public required DateTime AccessTokenExpiresAt { get; init; }

    /// <summary>
    /// When the refresh token expires.
    /// </summary>
    public required DateTime RefreshTokenExpiresAt { get; init; }

    /// <summary>
    /// Whether the user must change their password.
    /// </summary>
    public bool MustChangePassword { get; init; }
}

/// <summary>
/// Represents an authenticated user.
/// </summary>
public record AuthenticatedUser
{
    /// <summary>
    /// User ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Display name.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// User's roles.
    /// </summary>
    public required IReadOnlyList<string> Roles { get; init; }

    /// <summary>
    /// User's effective permissions (from roles and direct assignments).
    /// </summary>
    public required IReadOnlySet<string> Permissions { get; init; }
}
