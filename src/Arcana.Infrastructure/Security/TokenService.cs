using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Arcana.Core.Security;

namespace Arcana.Infrastructure.Security;

/// <summary>
/// Simple token service using HMAC-SHA256 for local desktop app.
/// </summary>
public class TokenService : ITokenService
{
    private readonly byte[] _secretKey;
    private readonly TimeSpan _accessTokenLifetime;
    private readonly TimeSpan _refreshTokenLifetime;

    public TokenService(TokenServiceOptions? options = null)
    {
        options ??= new TokenServiceOptions();

        // Generate or use provided secret key
        if (options.SecretKey != null)
        {
            _secretKey = Convert.FromBase64String(options.SecretKey);
        }
        else
        {
            // Generate a machine-specific key based on machine ID
            var machineId = Environment.MachineName + Environment.UserName;
            _secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(machineId));
        }

        _accessTokenLifetime = options.AccessTokenLifetime;
        _refreshTokenLifetime = options.RefreshTokenLifetime;
    }

    public TokenResult GenerateAccessToken(AuthenticatedUser user)
    {
        var expiresAt = DateTime.UtcNow.Add(_accessTokenLifetime);

        var payload = new TokenPayload
        {
            UserId = user.Id,
            Username = user.Username,
            ExpiresAt = expiresAt.Ticks,
            IssuedAt = DateTime.UtcNow.Ticks
        };

        var token = CreateSignedToken(payload);

        return new TokenResult
        {
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    public TokenResult GenerateRefreshToken()
    {
        var expiresAt = DateTime.UtcNow.Add(_refreshTokenLifetime);

        // Generate random bytes for refresh token
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(randomBytes);

        return new TokenResult
        {
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    public TokenValidationResult ValidateAccessToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return TokenValidationResult.Invalid("Token is empty");

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2)
                return TokenValidationResult.Invalid("Invalid token format");

            var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
            var providedSignature = parts[1];

            // Verify signature
            var expectedSignature = ComputeSignature(parts[0]);
            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedSignature),
                Encoding.UTF8.GetBytes(expectedSignature)))
            {
                return TokenValidationResult.Invalid("Invalid signature");
            }

            var payload = JsonSerializer.Deserialize<TokenPayload>(payloadJson);
            if (payload == null)
                return TokenValidationResult.Invalid("Invalid payload");

            // Check expiration
            var expiresAt = new DateTime(payload.ExpiresAt, DateTimeKind.Utc);
            if (DateTime.UtcNow > expiresAt)
                return TokenValidationResult.Invalid("Token expired", isExpired: true);

            return TokenValidationResult.Valid(payload.UserId, payload.Username);
        }
        catch (Exception ex)
        {
            return TokenValidationResult.Invalid($"Token validation failed: {ex.Message}");
        }
    }

    public bool ValidateRefreshToken(string token, string storedToken)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(storedToken))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(token),
            Encoding.UTF8.GetBytes(storedToken));
    }

    private string CreateSignedToken(TokenPayload payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));
        var signature = ComputeSignature(payloadBase64);

        return $"{payloadBase64}.{signature}";
    }

    private string ComputeSignature(string data)
    {
        using var hmac = new HMACSHA256(_secretKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    private record TokenPayload
    {
        public int UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public long ExpiresAt { get; init; }
        public long IssuedAt { get; init; }
    }
}

/// <summary>
/// Options for token service.
/// </summary>
public class TokenServiceOptions
{
    /// <summary>
    /// Base64-encoded secret key. If null, a machine-specific key is generated.
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Access token lifetime. Default: 1 hour.
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Refresh token lifetime. Default: 7 days.
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);
}
