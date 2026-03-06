using Arcana.Core.Security;
using Arcana.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace Arcana.Infrastructure.Tests.Security;

public class TokenServiceTests
{
    private readonly TokenServiceImpl _service;
    private readonly AuthenticatedUser _testUser;

    public TokenServiceTests()
    {
        var options = new TokenServiceOptions
        {
            AccessTokenLifetime = TimeSpan.FromHours(1),
            RefreshTokenLifetime = TimeSpan.FromDays(7)
        };
        _service = new TokenServiceImpl(options);

        _testUser = new AuthenticatedUser
        {
            Id = 42,
            Username = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            Roles = new List<string> { "Admin" },
            Permissions = new HashSet<string> { "read", "write" }
        };
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ShouldReturnToken()
    {
        var result = _service.GenerateAccessToken(_testUser);

        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateAccessToken_ShouldExpireInApproximatelyOneHour()
    {
        var before = DateTime.UtcNow;
        var result = _service.GenerateAccessToken(_testUser);
        var after = DateTime.UtcNow;

        result.ExpiresAt.Should().BeCloseTo(before.AddHours(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateAccessToken_TokenHasTwoParts()
    {
        var result = _service.GenerateAccessToken(_testUser);
        var parts = result.Token.Split('.');
        parts.Should().HaveCount(2);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnRandomToken()
    {
        var result1 = _service.GenerateRefreshToken();
        var result2 = _service.GenerateRefreshToken();

        result1.Token.Should().NotBeNullOrWhiteSpace();
        result2.Token.Should().NotBeNullOrWhiteSpace();
        result1.Token.Should().NotBe(result2.Token);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldExpireInSevenDays()
    {
        var before = DateTime.UtcNow;
        var result = _service.GenerateRefreshToken();

        result.ExpiresAt.Should().BeCloseTo(before.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ValidateAccessToken_ValidToken_ShouldReturnValidResult()
    {
        var tokenResult = _service.GenerateAccessToken(_testUser);

        var validation = _service.ValidateAccessToken(tokenResult.Token);

        validation.IsValid.Should().BeTrue();
        validation.UserId.Should().Be(_testUser.Id);
        validation.Username.Should().Be(_testUser.Username);
    }

    [Fact]
    public void ValidateAccessToken_EmptyToken_ShouldReturnInvalid()
    {
        var validation = _service.ValidateAccessToken("");
        validation.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateAccessToken_WhitespaceToken_ShouldReturnInvalid()
    {
        var validation = _service.ValidateAccessToken("   ");
        validation.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateAccessToken_MalformedToken_ShouldReturnInvalid()
    {
        var validation = _service.ValidateAccessToken("not.a.valid.token");
        validation.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateAccessToken_TamperedSignature_ShouldReturnInvalid()
    {
        var tokenResult = _service.GenerateAccessToken(_testUser);
        var parts = tokenResult.Token.Split('.');
        var tamperedToken = $"{parts[0]}.INVALIDSIGNATURE";

        var validation = _service.ValidateAccessToken(tamperedToken);
        validation.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateAccessToken_TamperedPayload_ShouldReturnInvalid()
    {
        var tokenResult = _service.GenerateAccessToken(_testUser);
        var parts = tokenResult.Token.Split('.');
        var fakePayload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"userId\":999,\"username\":\"hacker\",\"expiresAt\":9999999999999,\"issuedAt\":0}"));
        var tamperedToken = $"{fakePayload}.{parts[1]}";

        var validation = _service.ValidateAccessToken(tamperedToken);
        validation.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateAccessToken_ExpiredToken_ShouldReturnInvalid()
    {
        var shortLivedOptions = new TokenServiceOptions
        {
            AccessTokenLifetime = TimeSpan.FromMilliseconds(1)
        };
        var shortLivedService = new TokenServiceImpl(shortLivedOptions);

        var token = shortLivedService.GenerateAccessToken(_testUser);
        System.Threading.Thread.Sleep(50); // wait for expiry

        var validation = shortLivedService.ValidateAccessToken(token.Token);
        validation.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateRefreshToken_MatchingTokens_ShouldReturnTrue()
    {
        var refreshToken = _service.GenerateRefreshToken();
        _service.ValidateRefreshToken(refreshToken.Token, refreshToken.Token).Should().BeTrue();
    }

    [Fact]
    public void ValidateRefreshToken_DifferentTokens_ShouldReturnFalse()
    {
        var token1 = _service.GenerateRefreshToken();
        var token2 = _service.GenerateRefreshToken();
        _service.ValidateRefreshToken(token1.Token, token2.Token).Should().BeFalse();
    }

    [Fact]
    public void ValidateRefreshToken_EmptyToken_ShouldReturnFalse()
    {
        _service.ValidateRefreshToken("", "sometoken").Should().BeFalse();
        _service.ValidateRefreshToken("sometoken", "").Should().BeFalse();
    }

    [Fact]
    public void TokenService_DefaultOptions_ShouldWork()
    {
        // Default constructor uses machine-specific key
        var defaultService = new TokenServiceImpl();
        var token = defaultService.GenerateAccessToken(_testUser);
        var validation = defaultService.ValidateAccessToken(token.Token);
        validation.IsValid.Should().BeTrue();
    }

    [Fact]
    public void TokenService_WithSecretKey_ShouldWork()
    {
        // Generate a 32-byte secret key (256 bits)
        var secretKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var options = new TokenServiceOptions { SecretKey = secretKey };
        var serviceWithKey = new TokenServiceImpl(options);

        var token = serviceWithKey.GenerateAccessToken(_testUser);
        var validation = serviceWithKey.ValidateAccessToken(token.Token);
        validation.IsValid.Should().BeTrue();
    }
}
