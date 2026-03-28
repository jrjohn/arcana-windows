using Arcana.Core.Security;
using Arcana.Data.Local;
using Arcana.Domain.Entities.Identity;
using Arcana.Infrastructure.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Infrastructure.Tests.Security;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<PasswordHasher> _passwordHasherMock;
    private readonly Mock<TokenService> _tokenServiceMock;
    private readonly Mock<CurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<AuthServiceImpl>> _loggerMock;
    private readonly AuthServiceImpl _service;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AppDbContext(options);
        _passwordHasherMock = new Mock<PasswordHasher>();
        _tokenServiceMock = new Mock<TokenService>();
        _currentUserServiceMock = new Mock<CurrentUserService>();
        _loggerMock = new Mock<ILogger<AuthServiceImpl>>();

        _service = new AuthServiceImpl(
            _context,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);

        SetupDefaultTokenMocks();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SetupDefaultTokenMocks()
    {
        var testUser = new AuthenticatedUser
        {
            Id = 1,
            Username = "testuser",
            DisplayName = "Test User",
            Roles = new List<string>(),
            Permissions = new HashSet<string>()
        };

        _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<AuthenticatedUser>()))
            .Returns(new TokenResult { Token = "access-token-123", ExpiresAt = DateTime.UtcNow.AddHours(1) });

        _tokenServiceMock.Setup(t => t.GenerateRefreshToken())
            .Returns(new TokenResult { Token = "refresh-token-xyz", ExpiresAt = DateTime.UtcNow.AddDays(7) });

        _tokenServiceMock.Setup(t => t.ValidateAccessToken(It.IsAny<string>()))
            .Returns(TokenValidationResult.Valid(1, "testuser"));

        _currentUserServiceMock.Setup(c => c.Username).Returns("admin");
    }

    private async Task<User> CreateUserAsync(
        string username = "testuser",
        string passwordHash = "hashed-password",
        bool isActive = true,
        bool isLocked = false,
        IList<Role>? roles = null)
    {
        var user = new User
        {
            Username = username,
            DisplayName = "Test User",
            Email = "test@example.com",
            PasswordHash = passwordHash,
            IsActive = isActive,
            IsLocked = isLocked,
            UserRoles = new List<UserRole>(),
            UserPermissions = new List<UserPermission>()
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (roles != null)
        {
            foreach (var role in roles)
            {
                if (!_context.Roles.Any(r => r.Name == role.Name))
                {
                    _context.Roles.Add(role);
                    await _context.SaveChangesAsync();
                }
                var userRole = new UserRole { UserId = user.Id, RoleId = role.Id };
                _context.UserRoles.Add(userRole);
            }
            await _context.SaveChangesAsync();
        }

        return user;
    }

    #region AuthenticateAsync Tests

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ShouldReturnSuccess()
    {
        var user = await CreateUserAsync();
        _passwordHasherMock.Setup(p => p.VerifyPassword("password123", user.PasswordHash)).Returns(true);

        var result = await _service.AuthenticateAsync("testuser", "password123");

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access-token-123");
        result.Value.RefreshToken.Should().Be("refresh-token-xyz");
    }

    [Fact]
    public async Task AuthenticateAsync_UserNotFound_ShouldReturnFailure()
    {
        var result = await _service.AuthenticateAsync("nonexistent", "password");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_WrongPassword_ShouldReturnFailure()
    {
        var user = await CreateUserAsync();
        _passwordHasherMock.Setup(p => p.VerifyPassword("wrongpass", user.PasswordHash)).Returns(false);

        var result = await _service.AuthenticateAsync("testuser", "wrongpass");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_EmptyUsername_ShouldReturnFailure()
    {
        var result = await _service.AuthenticateAsync("", "password");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_EmptyPassword_ShouldReturnFailure()
    {
        var result = await _service.AuthenticateAsync("testuser", "");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_InactiveUser_ShouldReturnFailure()
    {
        var user = await CreateUserAsync(isActive: false);
        _passwordHasherMock.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var result = await _service.AuthenticateAsync("testuser", "password");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_LockedUser_ShouldReturnFailure()
    {
        var user = await CreateUserAsync(isLocked: true);
        user.LockedUntil = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

        var result = await _service.AuthenticateAsync("testuser", "password");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_ExpiredLock_ShouldSucceedWithCorrectPassword()
    {
        var user = await CreateUserAsync(isLocked: true);
        user.LockedUntil = DateTime.UtcNow.AddHours(-1); // lock expired
        await _context.SaveChangesAsync();

        _passwordHasherMock.Setup(p => p.VerifyPassword("password", user.PasswordHash)).Returns(true);

        var result = await _service.AuthenticateAsync("testuser", "password");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticateAsync_MultipleWrongPasswords_ShouldIncrementFailedAttempts()
    {
        var user = await CreateUserAsync();
        _passwordHasherMock.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        await _service.AuthenticateAsync("testuser", "wrong1");
        await _service.AuthenticateAsync("testuser", "wrong2");

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginAttempts.Should().Be(2);
    }

    [Fact]
    public async Task AuthenticateAsync_FiveWrongPasswords_ShouldLockAccount()
    {
        var user = await CreateUserAsync();
        _passwordHasherMock.Setup(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        for (int i = 0; i < 5; i++)
        {
            await _service.AuthenticateAsync("testuser", "wrong");
        }

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.IsLocked.Should().BeTrue();
        updatedUser.LockedUntil.Should().NotBeNull();
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_ExistingUser_ShouldClearRefreshToken()
    {
        var user = await CreateUserAsync();
        user.RefreshToken = "some-token";
        await _context.SaveChangesAsync();

        var result = await _service.LogoutAsync(user.Id);

        result.IsSuccess.Should().BeTrue();
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task LogoutAsync_NonExistentUser_ShouldReturnFailure()
    {
        var result = await _service.LogoutAsync(9999);
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region LockAccountAsync Tests

    [Fact]
    public async Task LockAccountAsync_ExistingUser_ShouldLockAccount()
    {
        var user = await CreateUserAsync();

        var result = await _service.LockAccountAsync(user.Id);

        result.IsSuccess.Should().BeTrue();
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task LockAccountAsync_WithDuration_ShouldSetLockedUntil()
    {
        var user = await CreateUserAsync();
        var duration = TimeSpan.FromHours(2);

        await _service.LockAccountAsync(user.Id, duration);

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.LockedUntil.Should().NotBeNull();
        updatedUser.LockedUntil!.Value.Should().BeCloseTo(DateTime.UtcNow.Add(duration), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LockAccountAsync_NonExistentUser_ShouldReturnFailure()
    {
        var result = await _service.LockAccountAsync(9999);
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region UnlockAccountAsync Tests

    [Fact]
    public async Task UnlockAccountAsync_LockedUser_ShouldUnlockAccount()
    {
        var user = await CreateUserAsync(isLocked: true);
        user.LockedUntil = DateTime.UtcNow.AddHours(1);
        user.FailedLoginAttempts = 5;
        await _context.SaveChangesAsync();

        var result = await _service.UnlockAccountAsync(user.Id);

        result.IsSuccess.Should().BeTrue();
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.IsLocked.Should().BeFalse();
        updatedUser.LockedUntil.Should().BeNull();
        updatedUser.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task UnlockAccountAsync_NonExistentUser_ShouldReturnFailure()
    {
        var result = await _service.UnlockAccountAsync(9999);
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region ChangePasswordAsync Tests

    [Fact]
    public async Task ChangePasswordAsync_CorrectCurrentPassword_ShouldSucceed()
    {
        var user = await CreateUserAsync();
        _passwordHasherMock.Setup(p => p.VerifyPassword("oldpass", user.PasswordHash)).Returns(true);
        _passwordHasherMock.Setup(p => p.HashPassword("newpass12")).Returns("new-hash");

        var result = await _service.ChangePasswordAsync(user.Id, "oldpass", "newpass12");

        result.IsSuccess.Should().BeTrue();
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.PasswordHash.Should().Be("new-hash");
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ShouldFail()
    {
        var user = await CreateUserAsync();
        _passwordHasherMock.Setup(p => p.VerifyPassword("wrongpass", user.PasswordHash)).Returns(false);

        var result = await _service.ChangePasswordAsync(user.Id, "wrongpass", "newpass");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_NonExistentUser_ShouldFail()
    {
        var result = await _service.ChangePasswordAsync(9999, "oldpass", "newpass");
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_ExistingUser_ShouldSucceed()
    {
        var user = await CreateUserAsync();
        _passwordHasherMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed-temp");

        var result = await _service.ResetPasswordAsync(user.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ResetPasswordAsync_NonExistentUser_ShouldFail()
    {
        var result = await _service.ResetPasswordAsync(9999);
        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region ValidateTokenAsync Tests

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ShouldReturnUser()
    {
        var user = await CreateUserAsync();
        user.RefreshToken = "some-refresh";
        await _context.SaveChangesAsync();

        _tokenServiceMock.Setup(t => t.ValidateAccessToken("valid-token"))
            .Returns(TokenValidationResult.Valid(user.Id, user.Username));

        var result = await _service.ValidateTokenAsync("valid-token");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Username.Should().Be(user.Username);
    }

    [Fact]
    public async Task ValidateTokenAsync_InvalidToken_ShouldReturnFailure()
    {
        _tokenServiceMock.Setup(t => t.ValidateAccessToken("bad-token"))
            .Returns(TokenValidationResult.Invalid("bad token"));

        var result = await _service.ValidateTokenAsync("bad-token");

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_ValidRefreshToken_ShouldSucceed()
    {
        var user = await CreateUserAsync();
        user.RefreshToken = "stored-refresh-token";
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        _tokenServiceMock.Setup(t => t.ValidateRefreshToken("stored-refresh-token", "stored-refresh-token")).Returns(true);

        var result = await _service.RefreshTokenAsync("stored-refresh-token");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredRefreshToken_ShouldFail()
    {
        var user = await CreateUserAsync();
        user.RefreshToken = "expired-token";
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(-1); // already expired
        await _context.SaveChangesAsync();

        var result = await _service.RefreshTokenAsync("expired-token");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_NoMatchingUser_ShouldFail()
    {
        var result = await _service.RefreshTokenAsync("unknown-token");
        result.IsSuccess.Should().BeFalse();
    }

    #endregion
}
