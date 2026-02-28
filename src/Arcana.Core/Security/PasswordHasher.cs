namespace Arcana.Core.Security;

/// <summary>
/// Password hashing service interface.
/// </summary>
public interface PasswordHasher
{
    /// <summary>
    /// Hash a password.
    /// </summary>
    /// <param name="password">The plain text password</param>
    /// <returns>The hashed password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verify a password against a hash.
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="hashedPassword">The stored hashed password</param>
    /// <returns>True if the password matches</returns>
    bool VerifyPassword(string password, string hashedPassword);

    /// <summary>
    /// Check if a hash needs to be rehashed (e.g., due to algorithm upgrade).
    /// </summary>
    /// <param name="hashedPassword">The stored hashed password</param>
    /// <returns>True if the hash should be regenerated</returns>
    bool NeedsRehash(string hashedPassword);
}
