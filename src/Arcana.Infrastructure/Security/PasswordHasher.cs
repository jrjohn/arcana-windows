using System.Security.Cryptography;
using Arcana.Core.Security;

namespace Arcana.Infrastructure.Security;

/// <summary>
/// PBKDF2-based password hasher.
/// </summary>
public class PasswordHasher : PasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 100000; // OWASP recommendation
    private const char Delimiter = ':';
    private const int CurrentVersion = 1;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        // Generate salt
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Hash password
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        // Format: version:iterations:salt:hash
        return $"{CurrentVersion}{Delimiter}{Iterations}{Delimiter}{Convert.ToBase64String(salt)}{Delimiter}{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedPassword);

        var parts = hashedPassword.Split(Delimiter);
        if (parts.Length != 4)
            return false;

        if (!int.TryParse(parts[0], out var version) || version != CurrentVersion)
            return false;

        if (!int.TryParse(parts[1], out var iterations))
            return false;

        byte[] salt;
        byte[] storedHash;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            storedHash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        // Compute hash with same parameters
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            storedHash.Length);

        // Constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
    }

    public bool NeedsRehash(string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            return true;

        var parts = hashedPassword.Split(Delimiter);
        if (parts.Length != 4)
            return true;

        // Check version
        if (!int.TryParse(parts[0], out var version) || version < CurrentVersion)
            return true;

        // Check iterations (may have increased over time)
        if (!int.TryParse(parts[1], out var iterations) || iterations < Iterations)
            return true;

        return false;
    }
}
