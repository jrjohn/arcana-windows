namespace Arcana.Core.Security;

/// <summary>
/// Token generation and validation service.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate an access token for a user.
    /// </summary>
    TokenResult GenerateAccessToken(AuthenticatedUser user);

    /// <summary>
    /// Generate a refresh token.
    /// </summary>
    TokenResult GenerateRefreshToken();

    /// <summary>
    /// Validate an access token and extract user information.
    /// </summary>
    TokenValidationResult ValidateAccessToken(string token);

    /// <summary>
    /// Validate a refresh token.
    /// </summary>
    bool ValidateRefreshToken(string token, string storedToken);
}

/// <summary>
/// Result of token generation.
/// </summary>
public record TokenResult
{
    /// <summary>
    /// The generated token.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// When the token expires.
    /// </summary>
    public required DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Result of token validation.
/// </summary>
public record TokenValidationResult
{
    /// <summary>
    /// Whether the token is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// The user ID from the token (if valid).
    /// </summary>
    public int? UserId { get; init; }

    /// <summary>
    /// The username from the token (if valid).
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Reason for validation failure (if invalid).
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Whether the token is expired.
    /// </summary>
    public bool IsExpired { get; init; }

    public static TokenValidationResult Valid(int userId, string username) => new()
    {
        IsValid = true,
        UserId = userId,
        Username = username
    };

    public static TokenValidationResult Invalid(string reason, bool isExpired = false) => new()
    {
        IsValid = false,
        FailureReason = reason,
        IsExpired = isExpired
    };
}
