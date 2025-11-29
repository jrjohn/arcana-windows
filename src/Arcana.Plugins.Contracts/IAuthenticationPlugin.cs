namespace Arcana.Plugins.Contracts;

/// <summary>
/// Interface for authentication provider plugins.
/// 身份驗證提供者插件介面
/// </summary>
/// <remarks>
/// Allows plugins to provide custom authentication mechanisms such as:
/// - OAuth2/OIDC (Google, Microsoft, etc.)
/// - LDAP/Active Directory
/// - SAML
/// - Custom SSO providers
/// - Multi-factor authentication
/// </remarks>
public interface IAuthenticationPlugin : IPlugin
{
    /// <summary>
    /// Name of the authentication provider (e.g., "Google", "Microsoft", "LDAP").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Display name for UI.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Icon resource for the provider.
    /// </summary>
    string? IconResource { get; }

    /// <summary>
    /// Whether this provider supports the authentication flow.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Authenticate using this provider.
    /// </summary>
    /// <param name="context">Authentication context with provider-specific parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with external user info</returns>
    Task<ExternalAuthResult> AuthenticateAsync(
        ExternalAuthContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate an external token (e.g., for session refresh).
    /// </summary>
    Task<ExternalTokenValidationResult> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout from the external provider.
    /// </summary>
    Task<bool> LogoutAsync(
        string externalUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the login UI for this provider (if custom UI is needed).
    /// </summary>
    /// <returns>View definition for login UI, or null to use default</returns>
    ViewDefinition? GetLoginView();
}

/// <summary>
/// Context for external authentication.
/// </summary>
public record ExternalAuthContext
{
    /// <summary>
    /// Provider-specific parameters (e.g., OAuth code, LDAP credentials).
    /// </summary>
    public Dictionary<string, string> Parameters { get; init; } = new();

    /// <summary>
    /// Redirect URI for OAuth flows.
    /// </summary>
    public string? RedirectUri { get; init; }

    /// <summary>
    /// State parameter for OAuth flows.
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// Scope for OAuth flows.
    /// </summary>
    public string? Scope { get; init; }
}

/// <summary>
/// Result of external authentication.
/// </summary>
public record ExternalAuthResult
{
    /// <summary>
    /// Whether authentication was successful.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Error message if authentication failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// External user information.
    /// </summary>
    public ExternalUserInfo? User { get; init; }

    /// <summary>
    /// External access token (if applicable).
    /// </summary>
    public string? ExternalAccessToken { get; init; }

    /// <summary>
    /// External refresh token (if applicable).
    /// </summary>
    public string? ExternalRefreshToken { get; init; }

    /// <summary>
    /// When the external token expires.
    /// </summary>
    public DateTime? ExternalTokenExpiresAt { get; init; }

    public static ExternalAuthResult Success(ExternalUserInfo user, string? accessToken = null, string? refreshToken = null) => new()
    {
        IsSuccess = true,
        User = user,
        ExternalAccessToken = accessToken,
        ExternalRefreshToken = refreshToken
    };

    public static ExternalAuthResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// External user information.
/// </summary>
public record ExternalUserInfo
{
    /// <summary>
    /// External provider user ID.
    /// </summary>
    public required string ExternalId { get; init; }

    /// <summary>
    /// Provider name.
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// Username or login name.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Display name.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// First name.
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Last name.
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Profile picture URL.
    /// </summary>
    public string? PictureUrl { get; init; }

    /// <summary>
    /// Additional claims from the provider.
    /// </summary>
    public Dictionary<string, string> Claims { get; init; } = new();

    /// <summary>
    /// Suggested roles based on external groups/claims.
    /// </summary>
    public IReadOnlyList<string> SuggestedRoles { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Result of external token validation.
/// </summary>
public record ExternalTokenValidationResult
{
    /// <summary>
    /// Whether the token is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// External user ID (if valid).
    /// </summary>
    public string? ExternalUserId { get; init; }

    /// <summary>
    /// Reason for failure (if invalid).
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Whether the token is expired.
    /// </summary>
    public bool IsExpired { get; init; }

    public static ExternalTokenValidationResult Valid(string externalUserId) => new()
    {
        IsValid = true,
        ExternalUserId = externalUserId
    };

    public static ExternalTokenValidationResult Invalid(string reason, bool isExpired = false) => new()
    {
        IsValid = false,
        FailureReason = reason,
        IsExpired = isExpired
    };
}

/// <summary>
/// Interface for MFA (Multi-Factor Authentication) plugins.
/// 多因素驗證插件介面
/// </summary>
public interface IMfaPlugin : IPlugin
{
    /// <summary>
    /// MFA method name (e.g., "TOTP", "SMS", "Email").
    /// </summary>
    string MethodName { get; }

    /// <summary>
    /// Display name for UI.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Whether this MFA method is available.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Enroll a user in this MFA method.
    /// </summary>
    Task<MfaEnrollmentResult> EnrollAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send MFA challenge to user.
    /// </summary>
    Task<MfaChallengeResult> SendChallengeAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify MFA code.
    /// </summary>
    Task<MfaVerificationResult> VerifyAsync(int userId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unenroll a user from this MFA method.
    /// </summary>
    Task<bool> UnenrollAsync(int userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// MFA enrollment result.
/// </summary>
public record MfaEnrollmentResult
{
    public required bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? SetupData { get; init; } // e.g., TOTP secret, QR code URL
    public IReadOnlyList<string>? BackupCodes { get; init; }
}

/// <summary>
/// MFA challenge result.
/// </summary>
public record MfaChallengeResult
{
    public required bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// MFA verification result.
/// </summary>
public record MfaVerificationResult
{
    public required bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsBackupCode { get; init; }
}
