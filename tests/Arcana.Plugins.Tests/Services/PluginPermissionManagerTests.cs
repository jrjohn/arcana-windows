using Arcana.Plugins.Contracts;
using Arcana.Plugins.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Plugins.Tests.Services;

public class PluginPermissionManagerTests : IDisposable
{
    private readonly Mock<ILogger<PluginPermissionManager>> _loggerMock;
    private readonly string _tempDataPath;
    private readonly PluginPermissionManager _manager;

    public PluginPermissionManagerTests()
    {
        _loggerMock = new Mock<ILogger<PluginPermissionManager>>();
        _tempDataPath = Path.Combine(Path.GetTempPath(), $"plugin_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDataPath);
        _manager = new PluginPermissionManager(_loggerMock.Object, _tempDataPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDataPath))
        {
            Directory.Delete(_tempDataPath, recursive: true);
        }
    }

    #region HasPermission Tests

    [Fact]
    public void HasPermission_PluginWithNoPermissions_ShouldReturnFalse()
    {
        // Act
        var result = _manager.HasPermission("unknown-plugin", PluginPermission.NetworkAccess);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermission_GrantedPermission_ShouldReturnTrue()
    {
        // Arrange
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.NetworkAccess);

        // Act
        var result = _manager.HasPermission("test-plugin", PluginPermission.NetworkAccess);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermission_PartialPermission_ShouldReturnFalse()
    {
        // Arrange - Grant only NetworkAccess
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.NetworkAccess);

        // Act - Check for NetworkAccess | WebSocketAccess
        var combined = PluginPermission.NetworkAccess | PluginPermission.WebSocketAccess;
        var result = _manager.HasPermission("test-plugin", combined);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermission_MultiplePermissions_ShouldReturnTrue()
    {
        // Arrange
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.NetworkAccess);
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.WebSocketAccess);

        // Act
        var combined = PluginPermission.NetworkAccess | PluginPermission.WebSocketAccess;
        var result = _manager.HasPermission("test-plugin", combined);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region CheckPermission Tests

    [Fact]
    public async Task CheckPermission_Granted_ShouldReturnAllow()
    {
        // Arrange
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.ReadLocalData);

        var request = new PermissionRequest
        {
            PluginId = "test-plugin",
            Permission = PluginPermission.ReadLocalData,
            Resource = "/data/file.txt"
        };

        // Act
        var result = _manager.CheckPermission(request);

        // Assert
        result.Granted.Should().BeTrue();
        result.DeniedPermissions.Should().Be(PluginPermission.None);
    }

    [Fact]
    public void CheckPermission_Denied_ShouldReturnDeny()
    {
        // Arrange
        var request = new PermissionRequest
        {
            PluginId = "test-plugin",
            Permission = PluginPermission.NetworkAccess,
            Resource = "https://example.com"
        };

        // Act
        var result = _manager.CheckPermission(request);

        // Assert
        result.Granted.Should().BeFalse();
        result.DeniedPermissions.Should().Be(PluginPermission.NetworkAccess);
        result.Message.Should().Contain("Permission denied");
    }

    [Fact]
    public void CheckPermission_Denied_ShouldRaiseEvent()
    {
        // Arrange
        PermissionDeniedEventArgs? capturedArgs = null;
        _manager.PermissionDenied += (sender, args) => capturedArgs = args;

        var request = new PermissionRequest
        {
            PluginId = "test-plugin",
            Permission = PluginPermission.ExecuteProcess,
            Resource = "cmd.exe"
        };

        // Act
        _manager.CheckPermission(request);

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.PluginId.Should().Be("test-plugin");
        capturedArgs.RequestedPermission.Should().Be(PluginPermission.ExecuteProcess);
        capturedArgs.Resource.Should().Be("cmd.exe");
    }

    [Fact]
    public async Task CheckPermission_PartiallyDenied_ShouldReturnOnlyDeniedPermissions()
    {
        // Arrange
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.NetworkAccess);

        var request = new PermissionRequest
        {
            PluginId = "test-plugin",
            Permission = PluginPermission.NetworkAccess | PluginPermission.ExecuteProcess
        };

        // Act
        var result = _manager.CheckPermission(request);

        // Assert
        result.Granted.Should().BeFalse();
        result.DeniedPermissions.Should().Be(PluginPermission.ExecuteProcess);
    }

    #endregion

    #region GrantPermissionAsync Tests

    [Fact]
    public async Task GrantPermissionAsync_ShouldAddPermission()
    {
        // Act
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.NetworkAccess);

        // Assert
        _manager.GetPermissions("test-plugin").Should().Be(PluginPermission.NetworkAccess);
    }

    [Fact]
    public async Task GrantPermissionAsync_Multiple_ShouldCombine()
    {
        // Act
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.NetworkAccess);
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.WebSocketAccess);

        // Assert
        var permissions = _manager.GetPermissions("test-plugin");
        permissions.Should().Be(PluginPermission.NetworkAccess | PluginPermission.WebSocketAccess);
    }

    [Fact]
    public async Task GrantPermissionAsync_SamePermissionTwice_ShouldNotDuplicate()
    {
        // Act
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.NetworkAccess);
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.NetworkAccess);

        // Assert
        _manager.GetPermissions("test-plugin").Should().Be(PluginPermission.NetworkAccess);
    }

    [Fact]
    public async Task GrantPermissionAsync_ShouldPersistToFile()
    {
        // Act
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.NetworkAccess);

        // Assert
        var permissionsFile = Path.Combine(_tempDataPath, "plugin_permissions.json");
        File.Exists(permissionsFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(permissionsFile);
        content.Should().Contain("test-plugin");
    }

    #endregion

    #region RevokePermissionAsync Tests

    [Fact]
    public async Task RevokePermissionAsync_ShouldRemovePermission()
    {
        // Arrange
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.NetworkAccess | PluginPermission.WebSocketAccess);

        // Act
        await _manager.RevokePermissionAsync("test-plugin", PluginPermission.NetworkAccess);

        // Assert
        var permissions = _manager.GetPermissions("test-plugin");
        permissions.Should().Be(PluginPermission.WebSocketAccess);
    }

    [Fact]
    public async Task RevokePermissionAsync_NonExistingPermission_ShouldNotThrow()
    {
        // Arrange
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.NetworkAccess);

        // Act & Assert - Should not throw
        await _manager.RevokePermissionAsync("test-plugin", PluginPermission.ExecuteProcess);

        _manager.GetPermissions("test-plugin").Should().Be(PluginPermission.NetworkAccess);
    }

    [Fact]
    public async Task RevokePermissionAsync_AllPermissions_ShouldResultInNone()
    {
        // Arrange
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.NetworkAccess);

        // Act
        await _manager.RevokePermissionAsync("test-plugin", PluginPermission.NetworkAccess);

        // Assert
        _manager.GetPermissions("test-plugin").Should().Be(PluginPermission.None);
    }

    #endregion

    #region SetPermissionsAsync Tests

    [Fact]
    public async Task SetPermissionsAsync_ShouldReplaceAllPermissions()
    {
        // Arrange
        await _manager.GrantPermissionAsync("test-plugin", PluginPermission.NetworkAccess);

        // Act
        await _manager.SetPermissionsAsync("test-plugin", PluginPermission.ReadDatabase);

        // Assert
        var permissions = _manager.GetPermissions("test-plugin");
        permissions.Should().Be(PluginPermission.ReadDatabase);
        permissions.HasFlag(PluginPermission.NetworkAccess).Should().BeFalse();
    }

    [Fact]
    public async Task SetPermissionsAsync_BasicPlugin_ShouldSetMultiple()
    {
        // Act
        await _manager.SetPermissionsAsync("test-plugin", PluginPermission.BasicPlugin);

        // Assert
        var permissions = _manager.GetPermissions("test-plugin");
        permissions.HasFlag(PluginPermission.ReadLocalData).Should().BeTrue();
        permissions.HasFlag(PluginPermission.WriteLocalData).Should().BeTrue();
        permissions.HasFlag(PluginPermission.CreateDialogs).Should().BeTrue();
    }

    #endregion

    #region Manifest Tests

    [Fact]
    public void GetManifest_Unregistered_ShouldReturnNull()
    {
        // Act
        var manifest = _manager.GetManifest("unknown-plugin");

        // Assert
        manifest.Should().BeNull();
    }

    [Fact]
    public void RegisterManifest_ShouldStoreManifest()
    {
        // Arrange
        var manifest = new PluginPermissionManifest
        {
            RequiredPermissions = PluginPermission.NetworkAccess,
            OptionalPermissions = PluginPermission.WebSocketAccess,
            PermissionReasons = new Dictionary<PluginPermission, string>
            {
                { PluginPermission.NetworkAccess, "Required for API calls" }
            }
        };

        // Act
        _manager.RegisterManifest("test-plugin", manifest);

        // Assert
        var retrieved = _manager.GetManifest("test-plugin");
        retrieved.Should().NotBeNull();
        retrieved!.RequiredPermissions.Should().Be(PluginPermission.NetworkAccess);
        retrieved.OptionalPermissions.Should().Be(PluginPermission.WebSocketAccess);
    }

    [Fact]
    public void RegisterManifestFromType_ShouldExtractAttributes()
    {
        // Act
        _manager.RegisterManifestFromType("test-plugin", typeof(TestPluginWithAttributes));

        // Assert
        var manifest = _manager.GetManifest("test-plugin");
        manifest.Should().NotBeNull();
        manifest!.RequiredPermissions.HasFlag(PluginPermission.NetworkAccess).Should().BeTrue();
        manifest.RequiredPermissions.HasFlag(PluginPermission.ReadDatabase).Should().BeTrue();
        manifest.OptionalPermissions.HasFlag(PluginPermission.WebSocketAccess).Should().BeTrue();
    }

    #endregion

    #region ValidateRequiredPermissions Tests

    [Fact]
    public void ValidateRequiredPermissions_NoManifest_ShouldReturnAllow()
    {
        // Act
        var result = _manager.ValidateRequiredPermissions("unregistered-plugin");

        // Assert
        result.Granted.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRequiredPermissions_AllRequired_ShouldReturnAllow()
    {
        // Arrange
        var manifest = new PluginPermissionManifest
        {
            RequiredPermissions = PluginPermission.NetworkAccess | PluginPermission.ReadDatabase
        };
        _manager.RegisterManifest("test-plugin", manifest);
        await _manager.SetPermissionsAsync("test-plugin", PluginPermission.NetworkAccess | PluginPermission.ReadDatabase);

        // Act
        var result = _manager.ValidateRequiredPermissions("test-plugin");

        // Assert
        result.Granted.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRequiredPermissions_MissingRequired_ShouldReturnDeny()
    {
        // Arrange
        var manifest = new PluginPermissionManifest
        {
            RequiredPermissions = PluginPermission.NetworkAccess | PluginPermission.ReadDatabase
        };
        _manager.RegisterManifest("test-plugin", manifest);
        await _manager.SetPermissionsAsync("test-plugin", PluginPermission.NetworkAccess); // Missing ReadDatabase

        // Act
        var result = _manager.ValidateRequiredPermissions("test-plugin");

        // Assert
        result.Granted.Should().BeFalse();
        result.DeniedPermissions.Should().Be(PluginPermission.ReadDatabase);
        result.Message.Should().Contain("Missing required permissions");
    }

    #endregion

    #region GrantBuiltInPermissionsAsync Tests

    [Fact]
    public async Task GrantBuiltInPermissionsAsync_ShouldGrantFullAccess()
    {
        // Act
        await _manager.GrantBuiltInPermissionsAsync("builtin-plugin");

        // Assert
        var permissions = _manager.GetPermissions("builtin-plugin");
        permissions.Should().Be(PluginPermission.FullAccess);
    }

    #endregion

    #region GrantDefaultPermissionsAsync Tests

    [Fact]
    public async Task GrantDefaultPermissionsAsync_WithManifest_ShouldGrantRequired()
    {
        // Arrange
        var manifest = new PluginPermissionManifest
        {
            RequiredPermissions = PluginPermission.NetworkAccess | PluginPermission.ReadDatabase,
            OptionalPermissions = PluginPermission.WebSocketAccess
        };
        _manager.RegisterManifest("test-plugin", manifest);

        // Act
        await _manager.GrantDefaultPermissionsAsync("test-plugin");

        // Assert
        var permissions = _manager.GetPermissions("test-plugin");
        permissions.Should().Be(PluginPermission.NetworkAccess | PluginPermission.ReadDatabase);
        permissions.HasFlag(PluginPermission.WebSocketAccess).Should().BeFalse();
    }

    [Fact]
    public async Task GrantDefaultPermissionsAsync_NoManifest_ShouldGrantBasic()
    {
        // Act
        await _manager.GrantDefaultPermissionsAsync("test-plugin");

        // Assert
        var permissions = _manager.GetPermissions("test-plugin");
        permissions.Should().Be(PluginPermission.BasicPlugin);
    }

    #endregion

    #region Persistence Tests

    [Fact]
    public async Task LoadPermissions_ShouldRestoreFromFile()
    {
        // Arrange - Create a manager, grant permissions, then create new manager
        await _manager.GrantPermissionAsync("plugin1", PluginPermission.NetworkAccess);
        await _manager.GrantPermissionAsync("plugin2", PluginPermission.ReadDatabase | PluginPermission.WriteDatabase);

        // Act - Create new manager instance with same path
        var newManager = new PluginPermissionManager(_loggerMock.Object, _tempDataPath);

        // Assert
        newManager.GetPermissions("plugin1").Should().Be(PluginPermission.NetworkAccess);
        newManager.GetPermissions("plugin2").Should().Be(PluginPermission.ReadDatabase | PluginPermission.WriteDatabase);
    }

    #endregion

    #region Test Helper Classes

    [RequiresPermission(PluginPermission.NetworkAccess, "Required for API calls")]
    [RequiresPermission(PluginPermission.ReadDatabase, "Required for data access")]
    [OptionalPermission(PluginPermission.WebSocketAccess, "Enables real-time updates")]
    private class TestPluginWithAttributes
    {
    }

    #endregion
}

public class PluginSandboxTests
{
    private readonly Mock<IPluginPermissionManager> _permissionManagerMock;
    private readonly PluginSandbox _sandbox;

    public PluginSandboxTests()
    {
        _permissionManagerMock = new Mock<IPluginPermissionManager>();
        _sandbox = new PluginSandbox("test-plugin", _permissionManagerMock.Object);
    }

    #region RequirePermission Tests

    [Fact]
    public void RequirePermission_Granted_ShouldNotThrow()
    {
        // Arrange
        _permissionManagerMock
            .Setup(m => m.CheckPermission(It.IsAny<PermissionRequest>()))
            .Returns(PermissionCheckResult.Allow());

        // Act & Assert - Should not throw
        _sandbox.RequirePermission(PluginPermission.NetworkAccess);
    }

    [Fact]
    public void RequirePermission_Denied_ShouldThrowPluginPermissionException()
    {
        // Arrange
        _permissionManagerMock
            .Setup(m => m.CheckPermission(It.IsAny<PermissionRequest>()))
            .Returns(PermissionCheckResult.Deny(PluginPermission.NetworkAccess, "Permission denied"));

        // Act & Assert
        var exception = Assert.Throws<PluginPermissionException>(() =>
            _sandbox.RequirePermission(PluginPermission.NetworkAccess));

        exception.PluginId.Should().Be("test-plugin");
        exception.DeniedPermissions.Should().Be(PluginPermission.NetworkAccess);
    }

    [Fact]
    public void RequirePermission_WithResource_ShouldPassResourceToManager()
    {
        // Arrange
        PermissionRequest? capturedRequest = null;
        _permissionManagerMock
            .Setup(m => m.CheckPermission(It.IsAny<PermissionRequest>()))
            .Callback<PermissionRequest>(r => capturedRequest = r)
            .Returns(PermissionCheckResult.Allow());

        // Act
        _sandbox.RequirePermission(PluginPermission.ReadFileSystem, "/path/to/file");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Resource.Should().Be("/path/to/file");
    }

    #endregion

    #region ExecuteWithPermission Tests

    [Fact]
    public void ExecuteWithPermission_Granted_ShouldExecuteAction()
    {
        // Arrange
        _permissionManagerMock
            .Setup(m => m.CheckPermission(It.IsAny<PermissionRequest>()))
            .Returns(PermissionCheckResult.Allow());

        var executed = false;

        // Act
        _sandbox.ExecuteWithPermission(PluginPermission.NetworkAccess, () =>
        {
            executed = true;
            return 42;
        });

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void ExecuteWithPermission_Granted_ShouldReturnResult()
    {
        // Arrange
        _permissionManagerMock
            .Setup(m => m.CheckPermission(It.IsAny<PermissionRequest>()))
            .Returns(PermissionCheckResult.Allow());

        // Act
        var result = _sandbox.ExecuteWithPermission(PluginPermission.NetworkAccess, () => 42);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void ExecuteWithPermission_Denied_ShouldNotExecuteAction()
    {
        // Arrange
        _permissionManagerMock
            .Setup(m => m.CheckPermission(It.IsAny<PermissionRequest>()))
            .Returns(PermissionCheckResult.Deny(PluginPermission.NetworkAccess));

        var executed = false;

        // Act & Assert
        Assert.Throws<PluginPermissionException>(() =>
            _sandbox.ExecuteWithPermission(PluginPermission.NetworkAccess, () =>
            {
                executed = true;
                return 42;
            }));

        executed.Should().BeFalse();
    }

    #endregion

    #region ExecuteWithPermissionAsync Tests

    [Fact]
    public async Task ExecuteWithPermissionAsync_Granted_ShouldExecuteAction()
    {
        // Arrange
        _permissionManagerMock
            .Setup(m => m.CheckPermission(It.IsAny<PermissionRequest>()))
            .Returns(PermissionCheckResult.Allow());

        // Act
        var result = await _sandbox.ExecuteWithPermissionAsync(
            PluginPermission.NetworkAccess,
            async () =>
            {
                await Task.Delay(1);
                return "result";
            });

        // Assert
        result.Should().Be("result");
    }

    [Fact]
    public async Task ExecuteWithPermissionAsync_Denied_ShouldThrow()
    {
        // Arrange
        _permissionManagerMock
            .Setup(m => m.CheckPermission(It.IsAny<PermissionRequest>()))
            .Returns(PermissionCheckResult.Deny(PluginPermission.NetworkAccess));

        // Act & Assert
        await Assert.ThrowsAsync<PluginPermissionException>(async () =>
            await _sandbox.ExecuteWithPermissionAsync(
                PluginPermission.NetworkAccess,
                async () =>
                {
                    await Task.Delay(1);
                    return "result";
                }));
    }

    #endregion
}

public class PluginPermissionExceptionTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        // Act
        var exception = new PluginPermissionException(
            "test-plugin",
            PluginPermission.NetworkAccess | PluginPermission.ExecuteProcess,
            "Custom message");

        // Assert
        exception.PluginId.Should().Be("test-plugin");
        exception.DeniedPermissions.Should().Be(PluginPermission.NetworkAccess | PluginPermission.ExecuteProcess);
        exception.Message.Should().Be("Custom message");
    }

    [Fact]
    public void Constructor_WithoutMessage_ShouldGenerateDefaultMessage()
    {
        // Act
        var exception = new PluginPermissionException(
            "test-plugin",
            PluginPermission.NetworkAccess);

        // Assert
        exception.Message.Should().Contain("test-plugin");
        exception.Message.Should().Contain("NetworkAccess");
    }
}

public class PermissionCheckResultTests
{
    [Fact]
    public void Allow_ShouldReturnGrantedResult()
    {
        // Act
        var result = PermissionCheckResult.Allow();

        // Assert
        result.Granted.Should().BeTrue();
        result.DeniedPermissions.Should().Be(PluginPermission.None);
        result.Message.Should().BeNull();
    }

    [Fact]
    public void Deny_ShouldReturnDeniedResult()
    {
        // Act
        var result = PermissionCheckResult.Deny(PluginPermission.NetworkAccess, "Access denied");

        // Assert
        result.Granted.Should().BeFalse();
        result.DeniedPermissions.Should().Be(PluginPermission.NetworkAccess);
        result.Message.Should().Be("Access denied");
    }
}

public class PluginPermissionFlagsTests
{
    [Fact]
    public void BasicPlugin_ShouldIncludeExpectedPermissions()
    {
        // Assert
        PluginPermission.BasicPlugin.HasFlag(PluginPermission.ReadLocalData).Should().BeTrue();
        PluginPermission.BasicPlugin.HasFlag(PluginPermission.WriteLocalData).Should().BeTrue();
        PluginPermission.BasicPlugin.HasFlag(PluginPermission.CreateDialogs).Should().BeTrue();
        PluginPermission.BasicPlugin.HasFlag(PluginPermission.InterPluginComm).Should().BeTrue();
        PluginPermission.BasicPlugin.HasFlag(PluginPermission.RegisterMenus).Should().BeTrue();
        PluginPermission.BasicPlugin.HasFlag(PluginPermission.RegisterViews).Should().BeTrue();
    }

    [Fact]
    public void BasicPlugin_ShouldNotIncludeSensitivePermissions()
    {
        // Assert
        PluginPermission.BasicPlugin.HasFlag(PluginPermission.NetworkAccess).Should().BeFalse();
        PluginPermission.BasicPlugin.HasFlag(PluginPermission.ExecuteProcess).Should().BeFalse();
        PluginPermission.BasicPlugin.HasFlag(PluginPermission.AccessCredentials).Should().BeFalse();
    }

    [Fact]
    public void NetworkPlugin_ShouldIncludeBasicPlusNetwork()
    {
        // Assert
        PluginPermission.NetworkPlugin.HasFlag(PluginPermission.ReadLocalData).Should().BeTrue();
        PluginPermission.NetworkPlugin.HasFlag(PluginPermission.NetworkAccess).Should().BeTrue();
    }

    [Fact]
    public void FullAccess_ShouldIncludeAllPermissions()
    {
        // Assert
        PluginPermission.FullAccess.HasFlag(PluginPermission.ReadLocalData).Should().BeTrue();
        PluginPermission.FullAccess.HasFlag(PluginPermission.NetworkAccess).Should().BeTrue();
        PluginPermission.FullAccess.HasFlag(PluginPermission.ExecuteProcess).Should().BeTrue();
        PluginPermission.FullAccess.HasFlag(PluginPermission.AccessCredentials).Should().BeTrue();
        PluginPermission.FullAccess.HasFlag(PluginPermission.AccessHardware).Should().BeTrue();
    }
}
