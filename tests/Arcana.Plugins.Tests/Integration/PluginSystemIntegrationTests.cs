using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;
using Arcana.Plugins.Data;
using Arcana.Plugins.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Plugins.Tests.Integration;

/// <summary>
/// Integration tests for the complete plugin system workflow.
/// Tests the interaction between multiple components.
/// </summary>
public class PluginSystemIntegrationTests : IDisposable
{
    private readonly string _testDataPath;
    private readonly ServiceProvider _serviceProvider;
    private readonly PluginDbContext _dbContext;

    public PluginSystemIntegrationTests()
    {
        _testDataPath = Path.Combine(Path.GetTempPath(), $"plugin_integration_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataPath);

        var services = new ServiceCollection();

        // Register DbContext
        services.AddDbContext<PluginDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        // Register logging
        services.AddLogging(builder => builder.AddDebug());

        // Register plugin services
        services.AddSingleton<IPluginPermissionManager>(sp =>
            new PluginPermissionManager(
                sp.GetRequiredService<ILogger<PluginPermissionManager>>(),
                _testDataPath));

        services.AddScoped<IPluginVersionRepository, PluginVersionRepository>();
        services.AddSingleton<PluginLoadContextManager>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<PluginDbContext>();
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, recursive: true);
        }
    }

    #region Permission and Version Repository Integration

    [Fact]
    public async Task PluginInstallation_ShouldSetPermissionsAndRecordVersion()
    {
        // Arrange
        var permissionManager = _serviceProvider.GetRequiredService<IPluginPermissionManager>();
        var versionRepository = _serviceProvider.GetRequiredService<IPluginVersionRepository>();

        var pluginId = "test-plugin";
        var manifest = new PluginPermissionManifest
        {
            RequiredPermissions = PluginPermission.NetworkAccess | PluginPermission.ReadDatabase,
            OptionalPermissions = PluginPermission.WebSocketAccess
        };

        // Act - Simulate plugin installation
        permissionManager.RegisterManifest(pluginId, manifest);
        await ((PluginPermissionManager)permissionManager).GrantDefaultPermissionsAsync(pluginId);

        var versionInfo = new PluginVersionInfo
        {
            PluginId = pluginId,
            Version = Version.Parse("1.0.0"),
            InstalledAt = DateTime.UtcNow,
            BackupPath = Path.Combine(_testDataPath, "backups", pluginId, "1.0.0"),
            IsCurrent = true
        };
        await versionRepository.AddVersionAsync(versionInfo);

        // Assert
        var permissions = permissionManager.GetPermissions(pluginId);
        permissions.Should().Be(PluginPermission.NetworkAccess | PluginPermission.ReadDatabase);

        var currentVersion = await versionRepository.GetCurrentVersionAsync(pluginId);
        currentVersion.Should().NotBeNull();
        currentVersion!.Version.Should().Be(Version.Parse("1.0.0"));
    }

    [Fact]
    public async Task PluginUpgrade_ShouldUpdateVersionAndPreservePermissions()
    {
        // Arrange
        var permissionManager = _serviceProvider.GetRequiredService<IPluginPermissionManager>();
        var versionRepository = _serviceProvider.GetRequiredService<IPluginVersionRepository>();
        var pluginId = "test-plugin";

        // Install v1.0.0
        await ((PluginPermissionManager)permissionManager).SetPermissionsAsync(
            pluginId,
            PluginPermission.NetworkAccess | PluginPermission.ReadDatabase);

        var v1 = new PluginVersionInfo
        {
            PluginId = pluginId,
            Version = Version.Parse("1.0.0"),
            InstalledAt = DateTime.UtcNow.AddDays(-1),
            BackupPath = Path.Combine(_testDataPath, "backups", pluginId, "1.0.0"),
            IsCurrent = true
        };
        await versionRepository.AddVersionAsync(v1);

        // Act - Upgrade to v2.0.0
        var v2 = new PluginVersionInfo
        {
            PluginId = pluginId,
            Version = Version.Parse("2.0.0"),
            InstalledAt = DateTime.UtcNow,
            BackupPath = Path.Combine(_testDataPath, "backups", pluginId, "2.0.0"),
            IsCurrent = true
        };
        await versionRepository.AddVersionAsync(v2);

        // Assert - Permissions should be preserved
        var permissions = permissionManager.GetPermissions(pluginId);
        permissions.Should().Be(PluginPermission.NetworkAccess | PluginPermission.ReadDatabase);

        // Version should be updated
        var currentVersion = await versionRepository.GetCurrentVersionAsync(pluginId);
        currentVersion!.Version.Should().Be(Version.Parse("2.0.0"));

        // Old version should be available for rollback
        var versions = await versionRepository.GetVersionsAsync(pluginId);
        versions.Should().HaveCount(2);
    }

    [Fact]
    public async Task PluginRollback_ShouldRestorePreviousVersion()
    {
        // Arrange
        var versionRepository = _serviceProvider.GetRequiredService<IPluginVersionRepository>();
        var pluginId = "test-plugin";

        // Create version history
        var v1 = new PluginVersionInfo
        {
            PluginId = pluginId,
            Version = Version.Parse("1.0.0"),
            InstalledAt = DateTime.UtcNow.AddDays(-2),
            BackupPath = Path.Combine(_testDataPath, "backups", pluginId, "1.0.0"),
            IsCurrent = false
        };
        var v2 = new PluginVersionInfo
        {
            PluginId = pluginId,
            Version = Version.Parse("2.0.0"),
            InstalledAt = DateTime.UtcNow,
            BackupPath = Path.Combine(_testDataPath, "backups", pluginId, "2.0.0"),
            IsCurrent = true
        };

        await versionRepository.AddVersionAsync(v1);
        await versionRepository.AddVersionAsync(v2);

        // Act - Rollback to v1.0.0
        await versionRepository.SetCurrentVersionAsync(pluginId, "1.0.0");

        // Assert
        var currentVersion = await versionRepository.GetCurrentVersionAsync(pluginId);
        currentVersion!.Version.Should().Be(Version.Parse("1.0.0"));

        var v2After = await versionRepository.GetVersionAsync(pluginId, "2.0.0");
        v2After!.IsCurrent.Should().BeFalse();
    }

    #endregion

    #region Dependency Resolution Integration

    [Fact]
    public void DependencyResolver_ShouldResolveComplexDependencyGraph()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DependencyResolver>>();
        var resolver = new DependencyResolver(loggerMock.Object);

        // Create a dependency graph:
        // App depends on UI and Core
        // UI depends on Core and Themes
        // Core depends on nothing
        // Themes depends on Core

        resolver.Register("core", SemanticVersion.Parse("1.0.0"));
        resolver.Register("themes", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("core@^1.0.0")
        });
        resolver.Register("ui", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("core@^1.0.0"),
            PluginDependency.Parse("themes@^1.0.0")
        });
        resolver.Register("app", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("ui@^1.0.0"),
            PluginDependency.Parse("core@^1.0.0")
        });

        // Act
        var result = resolver.Resolve();

        // Assert
        result.Success.Should().BeTrue();
        var order = result.ResolvedOrder.ToList();

        // Core should be first (no dependencies)
        order.IndexOf("core").Should().Be(0);

        // Themes should come after Core
        order.IndexOf("themes").Should().BeGreaterThan(order.IndexOf("core"));

        // UI should come after both Core and Themes
        order.IndexOf("ui").Should().BeGreaterThan(order.IndexOf("core"));
        order.IndexOf("ui").Should().BeGreaterThan(order.IndexOf("themes"));

        // App should be last
        order.IndexOf("app").Should().Be(3);
    }

    [Fact]
    public void DependencyResolver_ShouldDetectVersionConflicts()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DependencyResolver>>();
        var resolver = new DependencyResolver(loggerMock.Object);

        // Plugin A requires Core ^1.0.0
        // Plugin B requires Core ^2.0.0
        // Core is at 1.5.0 - satisfies A but not B

        resolver.Register("core", SemanticVersion.Parse("1.5.0"));
        resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("core@^1.0.0")
        });
        resolver.Register("plugin-b", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("core@^2.0.0")
        });

        // Act
        var result = resolver.Resolve();

        // Assert
        result.Success.Should().BeFalse();
        result.Conflicts.Should().NotBeEmpty();
    }

    #endregion

    #region Load Context Integration

    [Fact]
    public void PluginLoadContextManager_ShouldManageMultipleContexts()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<PluginLoadContextManager>();
        var plugin1Path = Path.Combine(_testDataPath, "plugins", "plugin1", "plugin1.dll");
        var plugin2Path = Path.Combine(_testDataPath, "plugins", "plugin2", "plugin2.dll");

        Directory.CreateDirectory(Path.GetDirectoryName(plugin1Path)!);
        Directory.CreateDirectory(Path.GetDirectoryName(plugin2Path)!);

        // Act
        var context1 = manager.CreateContext("plugin1", plugin1Path);
        var context2 = manager.CreateContext("plugin2", plugin2Path);

        // Assert
        context1.Should().NotBeSameAs(context2);
        manager.GetContext("plugin1").Should().BeSameAs(context1);
        manager.GetContext("plugin2").Should().BeSameAs(context2);

        var status = manager.GetUnloadStatus();
        status.Should().HaveCount(2);
    }

    [Fact]
    public void PluginLoadContextManager_ShouldReplaceExistingContext()
    {
        // Arrange
        var manager = _serviceProvider.GetRequiredService<PluginLoadContextManager>();
        var pluginPath = Path.Combine(_testDataPath, "plugins", "plugin1", "plugin1.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(pluginPath)!);

        var context1 = manager.CreateContext("plugin1", pluginPath);
        var context1Unloading = false;
        context1.Unloading += (s, e) => context1Unloading = true;

        // Act - Create another context for the same plugin
        var context2 = manager.CreateContext("plugin1", pluginPath);

        // Assert
        context2.Should().NotBeSameAs(context1);
        context1Unloading.Should().BeTrue();
        manager.GetContext("plugin1").Should().BeSameAs(context2);
    }

    #endregion

    #region Permission Security Integration

    [Fact]
    public async Task PluginSandbox_ShouldEnforcePermissions()
    {
        // Arrange
        var permissionManager = _serviceProvider.GetRequiredService<IPluginPermissionManager>();
        var pluginId = "sandboxed-plugin";

        // Grant only basic permissions
        await ((PluginPermissionManager)permissionManager).SetPermissionsAsync(
            pluginId,
            PluginPermission.BasicPlugin);

        var sandbox = new PluginSandbox(pluginId, permissionManager);

        // Act & Assert - Should allow basic operations
        sandbox.RequirePermission(PluginPermission.ReadLocalData); // Should not throw

        // Act & Assert - Should deny network access
        Assert.Throws<PluginPermissionException>(() =>
            sandbox.RequirePermission(PluginPermission.NetworkAccess));
    }

    [Fact]
    public async Task PluginPermission_ShouldTrackDeniedAccess()
    {
        // Arrange
        var permissionManager = _serviceProvider.GetRequiredService<IPluginPermissionManager>();
        var pluginId = "tracked-plugin";
        var deniedEvents = new List<PermissionDeniedEventArgs>();

        permissionManager.PermissionDenied += (sender, args) => deniedEvents.Add(args);

        // Act - Try to check permission without granting
        permissionManager.CheckPermission(new PermissionRequest
        {
            PluginId = pluginId,
            Permission = PluginPermission.ExecuteProcess,
            Resource = "cmd.exe"
        });

        // Assert
        deniedEvents.Should().HaveCount(1);
        deniedEvents[0].PluginId.Should().Be(pluginId);
        deniedEvents[0].RequestedPermission.Should().Be(PluginPermission.ExecuteProcess);
        deniedEvents[0].Resource.Should().Be("cmd.exe");
    }

    #endregion

    #region Health Monitoring Integration

    [Fact]
    public async Task HealthHistory_ShouldTrackOverTime()
    {
        // Arrange
        var versionRepository = _serviceProvider.GetRequiredService<IPluginVersionRepository>();
        var pluginId = "monitored-plugin";

        // Record health snapshots over time
        var baseTime = DateTime.UtcNow;
        var states = new[] { HealthState.Healthy, HealthState.Healthy, HealthState.Degraded, HealthState.Unhealthy, HealthState.Healthy };

        for (int i = 0; i < states.Length; i++)
        {
            var status = new PluginHealthStatus
            {
                PluginId = pluginId,
                PluginName = "Monitored Plugin",
                State = states[i],
                CheckedAt = baseTime.AddMinutes(i),
                ResponseTime = TimeSpan.FromMilliseconds(100 + i * 50),
                MemoryUsageBytes = 1024 * 1024 * (1 + i)
            };
            await versionRepository.RecordHealthSnapshotAsync(pluginId, status);
        }

        // Act
        var history = await versionRepository.GetHealthHistoryAsync(
            pluginId,
            baseTime.AddMinutes(-1),
            baseTime.AddMinutes(10));

        // Assert
        history.Should().HaveCount(5);

        // Should be ordered by newest first
        history[0].State.Should().Be(HealthState.Healthy);
        history[1].State.Should().Be(HealthState.Unhealthy);
        history[2].State.Should().Be(HealthState.Degraded);
    }

    #endregion

    #region Complete Plugin Lifecycle Integration

    [Fact]
    public async Task CompletePluginLifecycle_InstallActivateDeactivateUninstall()
    {
        // Arrange
        var permissionManager = _serviceProvider.GetRequiredService<IPluginPermissionManager>();
        var versionRepository = _serviceProvider.GetRequiredService<IPluginVersionRepository>();
        var loadContextManager = _serviceProvider.GetRequiredService<PluginLoadContextManager>();
        var pluginId = "lifecycle-plugin";

        // Step 1: Install
        var manifest = new PluginPermissionManifest
        {
            RequiredPermissions = PluginPermission.BasicPlugin,
            OptionalPermissions = PluginPermission.NetworkAccess
        };
        permissionManager.RegisterManifest(pluginId, manifest);
        await ((PluginPermissionManager)permissionManager).GrantDefaultPermissionsAsync(pluginId);

        var versionInfo = new PluginVersionInfo
        {
            PluginId = pluginId,
            Version = Version.Parse("1.0.0"),
            InstalledAt = DateTime.UtcNow,
            BackupPath = Path.Combine(_testDataPath, "backups", pluginId, "1.0.0"),
            IsCurrent = true
        };
        await versionRepository.AddVersionAsync(versionInfo);

        // Verify install
        var currentVersion = await versionRepository.GetCurrentVersionAsync(pluginId);
        currentVersion.Should().NotBeNull();
        permissionManager.GetPermissions(pluginId).Should().Be(PluginPermission.BasicPlugin);

        // Step 2: Activate (create load context)
        var pluginPath = Path.Combine(_testDataPath, "plugins", pluginId, "plugin.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(pluginPath)!);
        var context = loadContextManager.CreateContext(pluginId, pluginPath);
        context.Should().NotBeNull();

        // Record healthy state
        await versionRepository.RecordHealthSnapshotAsync(pluginId, new PluginHealthStatus
        {
            PluginId = pluginId,
            PluginName = "Lifecycle Plugin",
            State = HealthState.Healthy,
            CheckedAt = DateTime.UtcNow
        });

        // Step 3: Deactivate (unload context)
        loadContextManager.UnloadContext(pluginId);

        // Step 4: Uninstall
        await versionRepository.DeleteAllVersionsAsync(pluginId);
        await ((PluginPermissionManager)permissionManager).SetPermissionsAsync(pluginId, PluginPermission.None);

        // Verify uninstall
        var versionsAfterUninstall = await versionRepository.GetVersionsAsync(pluginId);
        versionsAfterUninstall.Should().BeEmpty();
        permissionManager.GetPermissions(pluginId).Should().Be(PluginPermission.None);
    }

    #endregion
}
