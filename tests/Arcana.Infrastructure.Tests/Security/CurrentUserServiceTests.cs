using Arcana.Core.Security;
using Arcana.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace Arcana.Infrastructure.Tests.Security;

public class CurrentUserServiceTests
{
    private readonly CurrentUserServiceImpl _service;

    public CurrentUserServiceTests()
    {
        _service = new CurrentUserServiceImpl();
    }

    private static AuthenticatedUser CreateUser(int id = 1, string username = "testuser",
        IReadOnlyList<string>? roles = null,
        IReadOnlySet<string>? permissions = null) =>
        new AuthenticatedUser
        {
            Id = id,
            Username = username,
            DisplayName = "Test User",
            Email = "test@test.com",
            Roles = roles ?? new List<string> { "Admin", "User" },
            Permissions = permissions ?? new HashSet<string> { "orders.view", "orders.create" }
        };

    [Fact]
    public void IsAuthenticated_WhenNoUserSet_ShouldReturnFalse()
    {
        _service.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void CurrentUser_WhenNoUserSet_ShouldReturnNull()
    {
        _service.CurrentUser.Should().BeNull();
    }

    [Fact]
    public void UserId_WhenNoUserSet_ShouldReturnNull()
    {
        _service.UserId.Should().BeNull();
    }

    [Fact]
    public void Username_WhenNoUserSet_ShouldReturnNull()
    {
        _service.Username.Should().BeNull();
    }

    [Fact]
    public void SetCurrentUser_ValidUser_ShouldSetUser()
    {
        var user = CreateUser();
        _service.SetCurrentUser(user);

        _service.IsAuthenticated.Should().BeTrue();
        _service.CurrentUser.Should().Be(user);
        _service.UserId.Should().Be(user.Id);
        _service.Username.Should().Be(user.Username);
    }

    [Fact]
    public void SetCurrentUser_RaisesCurrentUserChangedEvent()
    {
        var user = CreateUser();
        CurrentUserChangedEventArgs? capturedArgs = null;
        _service.CurrentUserChanged += (_, args) => capturedArgs = args;

        _service.SetCurrentUser(user);

        capturedArgs.Should().NotBeNull();
        capturedArgs!.NewUser.Should().Be(user);
        capturedArgs.PreviousUser.Should().BeNull();
    }

    [Fact]
    public void SetCurrentUser_Replaces_ExistingUser_AndRaisesEvent()
    {
        var user1 = CreateUser(1, "user1");
        var user2 = CreateUser(2, "user2");
        _service.SetCurrentUser(user1);

        CurrentUserChangedEventArgs? capturedArgs = null;
        _service.CurrentUserChanged += (_, args) => capturedArgs = args;
        _service.SetCurrentUser(user2);

        capturedArgs!.PreviousUser.Should().Be(user1);
        capturedArgs.NewUser.Should().Be(user2);
        _service.CurrentUser.Should().Be(user2);
    }

    [Fact]
    public void ClearCurrentUser_ShouldRemoveUser()
    {
        _service.SetCurrentUser(CreateUser());
        _service.ClearCurrentUser();

        _service.IsAuthenticated.Should().BeFalse();
        _service.CurrentUser.Should().BeNull();
    }

    [Fact]
    public void ClearCurrentUser_RaisesEvent_WithNullNewUser()
    {
        _service.SetCurrentUser(CreateUser());
        CurrentUserChangedEventArgs? capturedArgs = null;
        _service.CurrentUserChanged += (_, args) => capturedArgs = args;

        _service.ClearCurrentUser();

        capturedArgs!.NewUser.Should().BeNull();
    }

    [Fact]
    public void HasPermission_WhenUserHasPermission_ShouldReturnTrue()
    {
        _service.SetCurrentUser(CreateUser());
        _service.HasPermission("orders.view").Should().BeTrue();
    }

    [Fact]
    public void HasPermission_WhenUserDoesNotHavePermission_ShouldReturnFalse()
    {
        _service.SetCurrentUser(CreateUser());
        _service.HasPermission("admin.delete").Should().BeFalse();
    }

    [Fact]
    public void HasPermission_WhenNoUser_ShouldReturnFalse()
    {
        _service.HasPermission("orders.view").Should().BeFalse();
    }

    [Fact]
    public void HasAnyPermission_WhenUserHasAtLeastOne_ShouldReturnTrue()
    {
        _service.SetCurrentUser(CreateUser());
        _service.HasAnyPermission("orders.view", "admin.delete").Should().BeTrue();
    }

    [Fact]
    public void HasAnyPermission_WhenUserHasNone_ShouldReturnFalse()
    {
        _service.SetCurrentUser(CreateUser());
        _service.HasAnyPermission("admin.delete", "admin.create").Should().BeFalse();
    }

    [Fact]
    public void HasAnyPermission_WhenNoUser_ShouldReturnFalse()
    {
        _service.HasAnyPermission("orders.view").Should().BeFalse();
    }

    [Fact]
    public void HasAllPermissions_WhenUserHasAll_ShouldReturnTrue()
    {
        _service.SetCurrentUser(CreateUser());
        _service.HasAllPermissions("orders.view", "orders.create").Should().BeTrue();
    }

    [Fact]
    public void HasAllPermissions_WhenUserMissingOne_ShouldReturnFalse()
    {
        _service.SetCurrentUser(CreateUser());
        _service.HasAllPermissions("orders.view", "admin.delete").Should().BeFalse();
    }

    [Fact]
    public void HasAllPermissions_WhenNoUser_ShouldReturnFalse()
    {
        _service.HasAllPermissions("orders.view").Should().BeFalse();
    }

    [Fact]
    public void IsInRole_WhenUserHasRole_ShouldReturnTrue()
    {
        _service.SetCurrentUser(CreateUser());
        _service.IsInRole("Admin").Should().BeTrue();
    }

    [Fact]
    public void IsInRole_CaseInsensitive_ShouldReturnTrue()
    {
        _service.SetCurrentUser(CreateUser());
        _service.IsInRole("admin").Should().BeTrue();
    }

    [Fact]
    public void IsInRole_WhenUserDoesNotHaveRole_ShouldReturnFalse()
    {
        _service.SetCurrentUser(CreateUser());
        _service.IsInRole("SuperAdmin").Should().BeFalse();
    }

    [Fact]
    public void IsInRole_WhenNoUser_ShouldReturnFalse()
    {
        _service.IsInRole("Admin").Should().BeFalse();
    }

    [Fact]
    public void IsInAnyRole_WhenUserHasAtLeastOne_ShouldReturnTrue()
    {
        _service.SetCurrentUser(CreateUser());
        _service.IsInAnyRole("Admin", "SuperAdmin").Should().BeTrue();
    }

    [Fact]
    public void IsInAnyRole_WhenUserHasNone_ShouldReturnFalse()
    {
        _service.SetCurrentUser(CreateUser());
        _service.IsInAnyRole("SuperAdmin", "Guest").Should().BeFalse();
    }

    [Fact]
    public void IsInAnyRole_WhenNoUser_ShouldReturnFalse()
    {
        _service.IsInAnyRole("Admin").Should().BeFalse();
    }

    [Fact]
    public void ThreadSafety_SetCurrentUser_ShouldNotThrow()
    {
        var tasks = Enumerable.Range(0, 20).Select(i => Task.Run(() =>
        {
            var user = CreateUser(i, $"user{i}");
            _service.SetCurrentUser(user);
            _ = _service.CurrentUser;
        }));

        var action = async () => await Task.WhenAll(tasks);
        action.Should().NotThrowAsync();
    }
}
