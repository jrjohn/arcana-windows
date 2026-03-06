using Arcana.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace Arcana.Infrastructure.Tests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasherImpl _hasher;

    public PasswordHasherTests()
    {
        _hasher = new PasswordHasherImpl();
    }

    [Fact]
    public void HashPassword_ValidPassword_ShouldReturnNonEmptyHash()
    {
        var hash = _hasher.HashPassword("MySecureP@ssw0rd");
        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HashPassword_SamePasswordTwice_ShouldReturnDifferentHashes()
    {
        var hash1 = _hasher.HashPassword("password123");
        var hash2 = _hasher.HashPassword("password123");
        hash1.Should().NotBe(hash2, "each hash should use a different random salt");
    }

    [Fact]
    public void HashPassword_ReturnsFormatWithFourParts()
    {
        var hash = _hasher.HashPassword("password");
        var parts = hash.Split(':');
        parts.Should().HaveCount(4);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void HashPassword_EmptyOrWhitespace_ShouldThrow(string password)
    {
        var act = () => _hasher.HashPassword(password);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ShouldReturnTrue()
    {
        var password = "MySecret123!";
        var hash = _hasher.HashPassword(password);
        _hasher.VerifyPassword(password, hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ShouldReturnFalse()
    {
        var hash = _hasher.HashPassword("correctPassword");
        _hasher.VerifyPassword("wrongPassword", hash).Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_InvalidHashFormat_ShouldReturnFalse()
    {
        _hasher.VerifyPassword("password", "not-a-valid-hash").Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_InvalidBase64InHash_ShouldReturnFalse()
    {
        _hasher.VerifyPassword("password", "1:100000:!!!invalid!!!:!!!invalid!!!").Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WrongVersion_ShouldReturnFalse()
    {
        _hasher.VerifyPassword("password", "99:100000:abc:def").Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void VerifyPassword_EmptyPassword_ShouldThrow(string password)
    {
        var hash = _hasher.HashPassword("realpassword");
        var act = () => _hasher.VerifyPassword(password, hash);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void VerifyPassword_EmptyHash_ShouldThrow(string hash)
    {
        var act = () => _hasher.VerifyPassword("password", hash);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NeedsRehash_ValidCurrentHash_ShouldReturnFalse()
    {
        var hash = _hasher.HashPassword("password");
        _hasher.NeedsRehash(hash).Should().BeFalse();
    }

    [Fact]
    public void NeedsRehash_NullOrEmpty_ShouldReturnTrue()
    {
        _hasher.NeedsRehash("").Should().BeTrue();
        _hasher.NeedsRehash("   ").Should().BeTrue();
    }

    [Fact]
    public void NeedsRehash_InvalidFormat_ShouldReturnTrue()
    {
        _hasher.NeedsRehash("invalid-hash").Should().BeTrue();
    }

    [Fact]
    public void NeedsRehash_WrongVersion_ShouldReturnTrue()
    {
        _hasher.NeedsRehash("0:100000:abc:def").Should().BeTrue();
    }

    [Fact]
    public void NeedsRehash_LowIterations_ShouldReturnTrue()
    {
        // Iteration count lower than current minimum
        _hasher.NeedsRehash("1:1000:abc:def").Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_MultipleCorrectRoundTrips()
    {
        var passwords = new[] { "simple", "P@ssw0rd!123", "日本語パスワード", "very long password with spaces and special chars !@#$%^&*()" };
        foreach (var password in passwords)
        {
            var hash = _hasher.HashPassword(password);
            _hasher.VerifyPassword(password, hash).Should().BeTrue($"password '{password}' should verify");
        }
    }
}
