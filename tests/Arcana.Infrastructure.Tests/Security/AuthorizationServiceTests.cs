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

public class AuthorizationServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AuthorizationServiceImpl _service;
    private readonly Mock<ILogger<AuthorizationServiceImpl>> _loggerMock;

    public AuthorizationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<AuthorizationServiceImpl>>();
        _service = new AuthorizationServiceImpl(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<(User user, Role role, AppPermission permission)> SetupUserWithRolePermissionAsync(
        string username = "testuser",
        string roleName = "Admin",
        string permissionCode = "orders.view")
    {
        var permission = new AppPermission
        {
            Code = permissionCode,
            DisplayName = permissionCode,
            Category = "Test"
        };
        _context.Permissions.Add(permission);

        var role = new Role
        {
            Name = roleName,
            DisplayName = roleName,
            RolePermissions = new List<RolePermission>()
        };
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        var rolePermission = new RolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id
        };
        _context.RolePermissions.Add(rolePermission);

        var user = new User
        {
            Username = username,
            DisplayName = username,
            PasswordHash = "hash",
            IsActive = true,
            UserRoles = new List<UserRole>(),
            UserPermissions = new List<UserPermission>()
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        };
        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();

        return (user, role, permission);
    }

    [Fact]
    public async Task HasPermissionAsync_UserWithPermission_ShouldReturnTrue()
    {
        var (user, _, _) = await SetupUserWithRolePermissionAsync();
        var result = await _service.HasPermissionAsync(user.Id, "orders.view");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_UserWithoutPermission_ShouldReturnFalse()
    {
        var (user, _, _) = await SetupUserWithRolePermissionAsync();
        var result = await _service.HasPermissionAsync(user.Id, "admin.delete");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_NonExistentUser_ShouldReturnFalse()
    {
        var result = await _service.HasPermissionAsync(9999, "orders.view");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAnyPermissionAsync_UserHasOneOfRequested_ShouldReturnTrue()
    {
        var (user, _, _) = await SetupUserWithRolePermissionAsync();
        var result = await _service.HasAnyPermissionAsync(user.Id, new[] { "orders.view", "admin.delete" });
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAnyPermissionAsync_UserHasNoneOfRequested_ShouldReturnFalse()
    {
        var (user, _, _) = await SetupUserWithRolePermissionAsync();
        var result = await _service.HasAnyPermissionAsync(user.Id, new[] { "admin.delete", "admin.create" });
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAllPermissionsAsync_UserHasAll_ShouldReturnTrue()
    {
        var (user, _, _) = await SetupUserWithRolePermissionAsync();

        // Add another permission to the same role
        var perm2 = new AppPermission { Code = "orders.create", DisplayName = "Create", Category = "Test" };
        _context.Permissions.Add(perm2);
        await _context.SaveChangesAsync();

        var role = await _context.Roles.FirstAsync();
        var rp = new RolePermission { RoleId = role.Id, PermissionId = perm2.Id };
        _context.RolePermissions.Add(rp);
        await _context.SaveChangesAsync();

        var result = await _service.HasAllPermissionsAsync(user.Id, new[] { "orders.view", "orders.create" });
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAllPermissionsAsync_UserMissingOne_ShouldReturnFalse()
    {
        var (user, _, _) = await SetupUserWithRolePermissionAsync();
        var result = await _service.HasAllPermissionsAsync(user.Id, new[] { "orders.view", "admin.delete" });
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsInRoleAsync_UserInRole_ShouldReturnTrue()
    {
        var (user, _, _) = await SetupUserWithRolePermissionAsync(roleName: "Admin");
        var result = await _service.IsInRoleAsync(user.Id, "Admin");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsInRoleAsync_UserNotInRole_ShouldReturnFalse()
    {
        var (user, _, _) = await SetupUserWithRolePermissionAsync(roleName: "Admin");
        var result = await _service.IsInRoleAsync(user.Id, "Manager");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsInAnyRoleAsync_UserInAtLeastOne_ShouldReturnTrue()
    {
        var (user, _, _) = await SetupUserWithRolePermissionAsync(roleName: "Admin");
        var result = await _service.IsInAnyRoleAsync(user.Id, new[] { "Admin", "Manager" });
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsInAnyRoleAsync_UserInNone_ShouldReturnFalse()
    {
        var (user, _, _) = await SetupUserWithRolePermissionAsync(roleName: "Admin");
        var result = await _service.IsInAnyRoleAsync(user.Id, new[] { "Manager", "Guest" });
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ReturnsCorrectSet()
    {
        var (user, _, permission) = await SetupUserWithRolePermissionAsync();
        var permissions = await _service.GetUserPermissionsAsync(user.Id);

        permissions.Should().Contain(permission.Code);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_NonExistentUser_ReturnsEmpty()
    {
        var permissions = await _service.GetUserPermissionsAsync(9999);
        permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserRolesAsync_ReturnsCorrectRoles()
    {
        var (user, role, _) = await SetupUserWithRolePermissionAsync(roleName: "Admin");
        var roles = await _service.GetUserRolesAsync(user.Id);

        roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task GetUserRolesAsync_NonExistentUser_ReturnsEmpty()
    {
        var roles = await _service.GetUserRolesAsync(9999);
        roles.Should().BeEmpty();
    }

    [Fact]
    public async Task AuthorizeAsync_UserHasPermission_ShouldReturnSuccess()
    {
        var (user, _, _) = await SetupUserWithRolePermissionAsync();
        var result = await _service.AuthorizeAsync(user.Id, "orders.view");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_UserLacksPermission_ShouldReturnFailure()
    {
        var (user, _, _) = await SetupUserWithRolePermissionAsync();
        var result = await _service.AuthorizeAsync(user.Id, "admin.delete", "resource/123");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserPermissionsAsync_DirectUserPermission_Granted_IsIncluded()
    {
        var (user, _, _) = await SetupUserWithRolePermissionAsync();

        var directPerm = new AppPermission { Code = "special.access", DisplayName = "Special", Category = "Test" };
        _context.Permissions.Add(directPerm);
        await _context.SaveChangesAsync();

        var up = new UserPermission
        {
            UserId = user.Id,
            PermissionId = directPerm.Id,
            IsGranted = true
        };
        _context.UserPermissions.Add(up);
        await _context.SaveChangesAsync();

        var permissions = await _service.GetUserPermissionsAsync(user.Id);
        permissions.Should().Contain("special.access");
    }

    [Fact]
    public async Task GetUserPermissionsAsync_DirectUserPermission_Revoked_IsExcluded()
    {
        var (user, role, permission) = await SetupUserWithRolePermissionAsync();

        // Create a "revoke" user permission for the same permission
        var revokeUp = new UserPermission
        {
            UserId = user.Id,
            PermissionId = permission.Id,
            IsGranted = false
        };
        _context.UserPermissions.Add(revokeUp);
        await _context.SaveChangesAsync();

        var permissions = await _service.GetUserPermissionsAsync(user.Id);
        permissions.Should().NotContain(permission.Code);
    }
}
