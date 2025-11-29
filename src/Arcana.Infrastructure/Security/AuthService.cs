using System.Security.Cryptography;
using Arcana.Core.Common;
using Arcana.Core.Security;
using Arcana.Data.Local;
using Arcana.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Arcana.Infrastructure.Security;

/// <summary>
/// Authentication service implementation.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthService> _logger;

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public AuthService(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ICurrentUserService currentUserService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<AuthResult>> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Result<AuthResult>.Failure(new AppError.Validation(ErrorCode.ValidationFailed, "Username and password are required", []));
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login failed: user {Username} not found", username);
            await LogAuditAsync(null, username, AuditEventType.LoginFailed, "User not found", false, cancellationToken);
            return Result<AuthResult>.Failure(new AppError.Auth(ErrorCode.InvalidCredentials, "Invalid username or password"));
        }

        // Check if account is locked
        if (user.IsLocked)
        {
            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
            {
                _logger.LogWarning("Login failed: user {Username} is locked until {LockedUntil}", username, user.LockedUntil);
                return Result<AuthResult>.Failure(new AppError.Auth(ErrorCode.AccountLocked, $"Account is locked until {user.LockedUntil:g}"));
            }
            else
            {
                // Lock expired, unlock the account
                user.IsLocked = false;
                user.LockedUntil = null;
                user.FailedLoginAttempts = 0;
            }
        }

        // Check if account is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: user {Username} is inactive", username);
            return Result<AuthResult>.Failure(new AppError.Auth(ErrorCode.AuthenticationFailed, "Account is inactive"));
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.IsLocked = true;
                user.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
                _logger.LogWarning("User {Username} locked after {Attempts} failed attempts", username, user.FailedLoginAttempts);
                await LogAuditAsync(user.Id, username, AuditEventType.AccountLocked, $"Locked after {user.FailedLoginAttempts} failed attempts", true, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await LogAuditAsync(user.Id, username, AuditEventType.LoginFailed, "Invalid password", false, cancellationToken);

            return Result<AuthResult>.Failure(new AppError.Auth(ErrorCode.InvalidCredentials, "Invalid username or password"));
        }

        // Check if password needs rehashing
        if (_passwordHasher.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = _passwordHasher.HashPassword(password);
        }

        // Build authenticated user
        var authenticatedUser = BuildAuthenticatedUser(user);

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(authenticatedUser);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Update user
        user.LastLoginAt = DateTime.UtcNow;
        user.FailedLoginAttempts = 0;
        user.RefreshToken = refreshToken.Token;
        user.RefreshTokenExpiresAt = refreshToken.ExpiresAt;

        await _context.SaveChangesAsync(cancellationToken);

        // Set current user
        _currentUserService.SetCurrentUser(authenticatedUser);

        await LogAuditAsync(user.Id, username, AuditEventType.LoginSuccess, null, true, cancellationToken);

        _logger.LogInformation("User {Username} logged in successfully", username);

        return Result<AuthResult>.Success(new AuthResult
        {
            User = authenticatedUser,
            AccessToken = accessToken.Token,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            MustChangePassword = user.MustChangePassword
        });
    }

    public async Task<Result<AuthResult>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<AuthResult>.Failure(new AppError.Auth(ErrorCode.AuthenticationFailed, "Refresh token is required"));
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u =>
                u.RefreshToken == refreshToken &&
                !u.IsDeleted &&
                u.IsActive,
                cancellationToken);

        if (user == null)
        {
            return Result<AuthResult>.Failure(new AppError.Auth(ErrorCode.TokenExpired, "Invalid refresh token"));
        }

        if (user.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            return Result<AuthResult>.Failure(new AppError.Auth(ErrorCode.TokenExpired, "Refresh token expired"));
        }

        // Build authenticated user
        var authenticatedUser = BuildAuthenticatedUser(user);

        // Generate new tokens
        var accessToken = _tokenService.GenerateAccessToken(authenticatedUser);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        // Update user
        user.RefreshToken = newRefreshToken.Token;
        user.RefreshTokenExpiresAt = newRefreshToken.ExpiresAt;

        await _context.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(user.Id, user.Username, AuditEventType.TokenRefreshed, null, true, cancellationToken);

        // Update current user
        _currentUserService.SetCurrentUser(authenticatedUser);

        return Result<AuthResult>.Success(new AuthResult
        {
            User = authenticatedUser,
            AccessToken = accessToken.Token,
            RefreshToken = newRefreshToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshTokenExpiresAt = newRefreshToken.ExpiresAt,
            MustChangePassword = user.MustChangePassword
        });
    }

    public async Task<Result<bool>> LogoutAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            return Result<bool>.Failure(new AppError.Data(ErrorCode.NotFound, "User not found"));
        }

        // Invalidate refresh token
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;

        await _context.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(userId, user.Username, AuditEventType.Logout, null, true, cancellationToken);

        // Clear current user if it's the same user
        if (_currentUserService.UserId == userId)
        {
            _currentUserService.ClearCurrentUser();
        }

        _logger.LogInformation("User {Username} logged out", user.Username);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ChangePasswordAsync(
        int userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            return Result<bool>.Failure(new AppError.Validation(ErrorCode.ValidationFailed, "Current and new passwords are required", []));
        }

        if (newPassword.Length < 8)
        {
            return Result<bool>.Failure(new AppError.Validation(ErrorCode.ValidationFailed, "Password must be at least 8 characters", []));
        }

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            return Result<bool>.Failure(new AppError.Data(ErrorCode.NotFound, "User not found"));
        }

        if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
        {
            await LogAuditAsync(userId, user.Username, AuditEventType.PasswordChanged, "Invalid current password", false, cancellationToken);
            return Result<bool>.Failure(new AppError.Auth(ErrorCode.InvalidCredentials, "Current password is incorrect"));
        }

        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.MustChangePassword = false;

        // Invalidate refresh token to force re-login
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;

        await _context.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(userId, user.Username, AuditEventType.PasswordChanged, null, true, cancellationToken);

        _logger.LogInformation("Password changed for user {Username}", user.Username);

        return Result<bool>.Success(true);
    }

    public async Task<Result<string>> ResetPasswordAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            return Result<string>.Failure(new AppError.Data(ErrorCode.NotFound, "User not found"));
        }

        // Generate temporary password
        var tempPassword = GenerateTemporaryPassword();

        user.PasswordHash = _passwordHasher.HashPassword(tempPassword);
        user.MustChangePassword = true;
        user.PasswordChangedAt = DateTime.UtcNow;

        // Invalidate refresh token
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;

        await _context.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(userId, user.Username, AuditEventType.PasswordResetCompleted, $"Reset by {_currentUserService.Username}", true, cancellationToken);

        _logger.LogInformation("Password reset for user {Username}", user.Username);

        return Result<string>.Success(tempPassword);
    }

    public async Task<Result<AuthenticatedUser>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var validation = _tokenService.ValidateAccessToken(token);

        if (!validation.IsValid)
        {
            return Result<AuthenticatedUser>.Failure(new AppError.Auth(ErrorCode.TokenExpired, validation.FailureReason ?? "Invalid token"));
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == validation.UserId && !u.IsDeleted && u.IsActive, cancellationToken);

        if (user == null)
        {
            return Result<AuthenticatedUser>.Failure(new AppError.Auth(ErrorCode.AuthenticationFailed, "User not found or inactive"));
        }

        return Result<AuthenticatedUser>.Success(BuildAuthenticatedUser(user));
    }

    public async Task<Result<bool>> LockAccountAsync(int userId, TimeSpan? duration = null, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            return Result<bool>.Failure(new AppError.Data(ErrorCode.NotFound, "User not found"));
        }

        user.IsLocked = true;
        user.LockedUntil = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : null;
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;

        await _context.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(userId, user.Username, AuditEventType.AccountLocked, $"Locked by {_currentUserService.Username}", true, cancellationToken);

        _logger.LogInformation("User {Username} account locked", user.Username);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> UnlockAccountAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            return Result<bool>.Failure(new AppError.Data(ErrorCode.NotFound, "User not found"));
        }

        user.IsLocked = false;
        user.LockedUntil = null;
        user.FailedLoginAttempts = 0;

        await _context.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(userId, user.Username, AuditEventType.AccountUnlocked, $"Unlocked by {_currentUserService.Username}", true, cancellationToken);

        _logger.LogInformation("User {Username} account unlocked", user.Username);

        return Result<bool>.Success(true);
    }

    private static AuthenticatedUser BuildAuthenticatedUser(User user)
    {
        var roles = user.UserRoles
            .Where(ur => !ur.IsDeleted && (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow))
            .Select(ur => ur.Role.Name)
            .ToList();

        var permissions = new HashSet<string>();

        // Add permissions from roles
        foreach (var userRole in user.UserRoles.Where(ur => !ur.IsDeleted))
        {
            foreach (var rp in userRole.Role.RolePermissions.Where(rp => !rp.IsDeleted))
            {
                permissions.Add(rp.Permission.Code);
            }
        }

        // Add/remove direct user permissions
        foreach (var up in user.UserPermissions.Where(up => !up.IsDeleted && (up.ExpiresAt == null || up.ExpiresAt > DateTime.UtcNow)))
        {
            if (up.IsGranted)
                permissions.Add(up.Permission.Code);
            else
                permissions.Remove(up.Permission.Code);
        }

        return new AuthenticatedUser
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Roles = roles,
            Permissions = permissions
        };
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%";
        var random = new byte[12];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(random);

        return new string(random.Select(b => chars[b % chars.Length]).ToArray());
    }

    private async Task LogAuditAsync(
        int? userId,
        string? username,
        AuditEventType eventType,
        string? details,
        bool isSuccess,
        CancellationToken cancellationToken)
    {
        var audit = new AuditLog
        {
            EventType = eventType,
            UserId = userId,
            Username = username,
            Action = eventType.ToString(),
            Details = details,
            IsSuccess = isSuccess,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(audit);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
