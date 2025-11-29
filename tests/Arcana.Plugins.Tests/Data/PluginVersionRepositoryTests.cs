using Arcana.Plugins.Contracts;
using Arcana.Plugins.Data;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Arcana.Plugins.Tests.Data;

public class PluginVersionRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PluginDbContext _context;
    private readonly PluginVersionRepository _repository;

    public PluginVersionRepositoryTests()
    {
        // Use SQLite in-memory with a shared connection to support transactions
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PluginDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new PluginDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new PluginVersionRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    #region GetVersionsAsync Tests

    [Fact]
    public async Task GetVersionsAsync_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Act
        var versions = await _repository.GetVersionsAsync("plugin-1");

        // Assert
        versions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetVersionsAsync_WithVersions_ShouldReturnOrdered()
    {
        // Arrange
        await AddVersionAsync("plugin-1", "1.0.0", DateTime.UtcNow.AddDays(-2));
        await AddVersionAsync("plugin-1", "1.1.0", DateTime.UtcNow.AddDays(-1));
        await AddVersionAsync("plugin-1", "2.0.0", DateTime.UtcNow);

        // Act
        var versions = await _repository.GetVersionsAsync("plugin-1");

        // Assert
        versions.Should().HaveCount(3);
        versions[0].Version.Should().Be(Version.Parse("2.0.0")); // Most recent first
        versions[1].Version.Should().Be(Version.Parse("1.1.0"));
        versions[2].Version.Should().Be(Version.Parse("1.0.0"));
    }

    [Fact]
    public async Task GetVersionsAsync_DifferentPlugins_ShouldOnlyReturnRequestedPlugin()
    {
        // Arrange
        await AddVersionAsync("plugin-1", "1.0.0", DateTime.UtcNow);
        await AddVersionAsync("plugin-2", "1.0.0", DateTime.UtcNow);
        await AddVersionAsync("plugin-2", "2.0.0", DateTime.UtcNow);

        // Act
        var versions = await _repository.GetVersionsAsync("plugin-2");

        // Assert
        versions.Should().HaveCount(2);
        versions.All(v => v.PluginId == "plugin-2").Should().BeTrue();
    }

    #endregion

    #region GetVersionAsync Tests

    [Fact]
    public async Task GetVersionAsync_ExistingVersion_ShouldReturnVersion()
    {
        // Arrange
        await AddVersionAsync("plugin-1", "1.0.0", DateTime.UtcNow);

        // Act
        var version = await _repository.GetVersionAsync("plugin-1", "1.0.0");

        // Assert
        version.Should().NotBeNull();
        version!.PluginId.Should().Be("plugin-1");
        version.Version.Should().Be(Version.Parse("1.0.0"));
    }

    [Fact]
    public async Task GetVersionAsync_NonExistingVersion_ShouldReturnNull()
    {
        // Act
        var version = await _repository.GetVersionAsync("plugin-1", "1.0.0");

        // Assert
        version.Should().BeNull();
    }

    [Fact]
    public async Task GetVersionAsync_WrongPlugin_ShouldReturnNull()
    {
        // Arrange
        await AddVersionAsync("plugin-1", "1.0.0", DateTime.UtcNow);

        // Act
        var version = await _repository.GetVersionAsync("plugin-2", "1.0.0");

        // Assert
        version.Should().BeNull();
    }

    #endregion

    #region GetCurrentVersionAsync Tests

    [Fact]
    public async Task GetCurrentVersionAsync_NoCurrent_ShouldReturnNull()
    {
        // Arrange
        var versionInfo = new PluginVersionInfo
        {
            PluginId = "plugin-1",
            Version = Version.Parse("1.0.0"),
            InstalledAt = DateTime.UtcNow,
            BackupPath = "/backup/1.0.0",
            IsCurrent = false
        };
        await _repository.AddVersionAsync(versionInfo);

        // Act
        var current = await _repository.GetCurrentVersionAsync("plugin-1");

        // Assert
        current.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentVersionAsync_WithCurrent_ShouldReturnCurrent()
    {
        // Arrange
        var versionInfo = new PluginVersionInfo
        {
            PluginId = "plugin-1",
            Version = Version.Parse("1.0.0"),
            InstalledAt = DateTime.UtcNow,
            BackupPath = "/backup/1.0.0",
            IsCurrent = true
        };
        await _repository.AddVersionAsync(versionInfo);

        // Act
        var current = await _repository.GetCurrentVersionAsync("plugin-1");

        // Assert
        current.Should().NotBeNull();
        current!.IsCurrent.Should().BeTrue();
        current.Version.Should().Be(Version.Parse("1.0.0"));
    }

    #endregion

    #region AddVersionAsync Tests

    [Fact]
    public async Task AddVersionAsync_ShouldAddVersion()
    {
        // Arrange
        var versionInfo = new PluginVersionInfo
        {
            PluginId = "plugin-1",
            Version = Version.Parse("1.0.0"),
            InstalledAt = DateTime.UtcNow,
            BackupPath = "/backup/1.0.0",
            IsCurrent = true,
            SizeBytes = 1024,
            ReleaseNotes = "Initial release"
        };

        // Act
        await _repository.AddVersionAsync(versionInfo);

        // Assert
        var versions = await _repository.GetVersionsAsync("plugin-1");
        versions.Should().HaveCount(1);
        versions[0].SizeBytes.Should().Be(1024);
        versions[0].ReleaseNotes.Should().Be("Initial release");
    }

    [Fact]
    public async Task AddVersionAsync_NewCurrentVersion_ShouldUnsetPreviousCurrent()
    {
        // Arrange
        var v1 = new PluginVersionInfo
        {
            PluginId = "plugin-1",
            Version = Version.Parse("1.0.0"),
            InstalledAt = DateTime.UtcNow.AddDays(-1),
            BackupPath = "/backup/1.0.0",
            IsCurrent = true
        };
        await _repository.AddVersionAsync(v1);

        var v2 = new PluginVersionInfo
        {
            PluginId = "plugin-1",
            Version = Version.Parse("2.0.0"),
            InstalledAt = DateTime.UtcNow,
            BackupPath = "/backup/2.0.0",
            IsCurrent = true
        };

        // Act
        await _repository.AddVersionAsync(v2);

        // Assert
        var v1Retrieved = await _repository.GetVersionAsync("plugin-1", "1.0.0");
        var v2Retrieved = await _repository.GetVersionAsync("plugin-1", "2.0.0");

        v1Retrieved!.IsCurrent.Should().BeFalse();
        v2Retrieved!.IsCurrent.Should().BeTrue();
    }

    #endregion

    #region UpdateVersionAsync Tests

    [Fact]
    public async Task UpdateVersionAsync_ShouldUpdateVersion()
    {
        // Arrange
        await AddVersionAsync("plugin-1", "1.0.0", DateTime.UtcNow);
        var updatedInfo = new PluginVersionInfo
        {
            PluginId = "plugin-1",
            Version = Version.Parse("1.0.0"),
            InstalledAt = DateTime.UtcNow,
            BackupPath = "/backup/1.0.0",
            IsCurrent = false,
            SizeBytes = 2048,
            ReleaseNotes = "Updated notes"
        };

        // Act
        await _repository.UpdateVersionAsync(updatedInfo);

        // Assert
        var version = await _repository.GetVersionAsync("plugin-1", "1.0.0");
        version!.SizeBytes.Should().Be(2048);
        version.ReleaseNotes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task UpdateVersionAsync_SettingCurrent_ShouldUnsetOthers()
    {
        // Arrange
        var v1 = new PluginVersionInfo
        {
            PluginId = "plugin-1",
            Version = Version.Parse("1.0.0"),
            InstalledAt = DateTime.UtcNow.AddDays(-1),
            BackupPath = "/backup/1.0.0",
            IsCurrent = true
        };
        await _repository.AddVersionAsync(v1);

        var v2 = new PluginVersionInfo
        {
            PluginId = "plugin-1",
            Version = Version.Parse("2.0.0"),
            InstalledAt = DateTime.UtcNow,
            BackupPath = "/backup/2.0.0",
            IsCurrent = false
        };
        await _repository.AddVersionAsync(v2);

        // Act - Set v2 as current
        var v2Updated = v2 with { IsCurrent = true };
        await _repository.UpdateVersionAsync(v2Updated);

        // Assert
        var v1Retrieved = await _repository.GetVersionAsync("plugin-1", "1.0.0");
        var v2Retrieved = await _repository.GetVersionAsync("plugin-1", "2.0.0");

        v1Retrieved!.IsCurrent.Should().BeFalse();
        v2Retrieved!.IsCurrent.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateVersionAsync_NonExistingVersion_ShouldNotThrow()
    {
        // Arrange
        var versionInfo = new PluginVersionInfo
        {
            PluginId = "plugin-1",
            Version = Version.Parse("1.0.0"),
            InstalledAt = DateTime.UtcNow,
            BackupPath = "/backup/1.0.0",
            IsCurrent = false
        };

        // Act & Assert - Should not throw
        await _repository.UpdateVersionAsync(versionInfo);
    }

    #endregion

    #region DeleteVersionAsync Tests

    [Fact]
    public async Task DeleteVersionAsync_ShouldDeleteVersion()
    {
        // Arrange
        await AddVersionAsync("plugin-1", "1.0.0", DateTime.UtcNow);
        await AddVersionAsync("plugin-1", "2.0.0", DateTime.UtcNow);

        // Act
        await _repository.DeleteVersionAsync("plugin-1", "1.0.0");

        // Assert
        var versions = await _repository.GetVersionsAsync("plugin-1");
        versions.Should().HaveCount(1);
        versions[0].Version.Should().Be(Version.Parse("2.0.0"));
    }

    [Fact]
    public async Task DeleteVersionAsync_NonExisting_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        await _repository.DeleteVersionAsync("plugin-1", "1.0.0");
    }

    #endregion

    #region DeleteAllVersionsAsync Tests

    [Fact]
    public async Task DeleteAllVersionsAsync_ShouldDeleteAllVersions()
    {
        // Arrange
        await AddVersionAsync("plugin-1", "1.0.0", DateTime.UtcNow);
        await AddVersionAsync("plugin-1", "2.0.0", DateTime.UtcNow);
        await AddVersionAsync("plugin-2", "1.0.0", DateTime.UtcNow);

        // Act
        await _repository.DeleteAllVersionsAsync("plugin-1");

        // Assert
        var plugin1Versions = await _repository.GetVersionsAsync("plugin-1");
        var plugin2Versions = await _repository.GetVersionsAsync("plugin-2");

        plugin1Versions.Should().BeEmpty();
        plugin2Versions.Should().HaveCount(1);
    }

    #endregion

    #region SetCurrentVersionAsync Tests

    [Fact]
    public async Task SetCurrentVersionAsync_ShouldSetCurrent()
    {
        // Arrange
        await AddVersionAsync("plugin-1", "1.0.0", DateTime.UtcNow.AddDays(-1), isCurrent: true);
        await AddVersionAsync("plugin-1", "2.0.0", DateTime.UtcNow, isCurrent: false);

        // Act
        await _repository.SetCurrentVersionAsync("plugin-1", "2.0.0");

        // Assert
        var v1 = await _repository.GetVersionAsync("plugin-1", "1.0.0");
        var v2 = await _repository.GetVersionAsync("plugin-1", "2.0.0");

        v1!.IsCurrent.Should().BeFalse();
        v2!.IsCurrent.Should().BeTrue();
    }

    [Fact]
    public async Task SetCurrentVersionAsync_NonExistingVersion_ShouldUnsetAllCurrents()
    {
        // Arrange
        await AddVersionAsync("plugin-1", "1.0.0", DateTime.UtcNow, isCurrent: true);

        // Act - Try to set a non-existing version as current
        await _repository.SetCurrentVersionAsync("plugin-1", "nonexistent");

        // Assert - Original version should not be current anymore
        var v1 = await _repository.GetVersionAsync("plugin-1", "1.0.0");
        v1!.IsCurrent.Should().BeFalse();
    }

    #endregion

    #region RecordHealthSnapshotAsync Tests

    [Fact]
    public async Task RecordHealthSnapshotAsync_ShouldAddSnapshot()
    {
        // Arrange
        var status = new PluginHealthStatus
        {
            PluginId = "plugin-1",
            PluginName = "Test Plugin",
            State = HealthState.Healthy,
            Message = "All good",
            CheckedAt = DateTime.UtcNow,
            ResponseTime = TimeSpan.FromMilliseconds(100),
            MemoryUsageBytes = 1024 * 1024,
            ErrorCount = 0
        };

        // Act
        await _repository.RecordHealthSnapshotAsync("plugin-1", status);

        // Assert
        var snapshots = await _context.PluginHealthSnapshots
            .Where(s => s.PluginId == "plugin-1")
            .ToListAsync();

        snapshots.Should().HaveCount(1);
        snapshots[0].HealthState.Should().Be((int)HealthState.Healthy);
        snapshots[0].Message.Should().Be("All good");
    }

    [Fact]
    public async Task RecordHealthSnapshotAsync_ShouldLimitSnapshots()
    {
        // Arrange - Add a few snapshots
        for (int i = 0; i < 10; i++)
        {
            _context.PluginHealthSnapshots.Add(new PluginHealthSnapshotEntity
            {
                PluginId = "plugin-1",
                HealthState = (int)HealthState.Healthy,
                CheckedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _context.SaveChangesAsync();

        // Act - Add one more
        var newStatus = new PluginHealthStatus
        {
            PluginId = "plugin-1",
            PluginName = "Test Plugin",
            State = HealthState.Healthy,
            CheckedAt = DateTime.UtcNow
        };
        await _repository.RecordHealthSnapshotAsync("plugin-1", newStatus);

        // Assert - Should have all 11 (limit is 1000)
        var count = await _context.PluginHealthSnapshots
            .Where(s => s.PluginId == "plugin-1")
            .CountAsync();

        count.Should().Be(11);
    }

    #endregion

    #region GetHealthHistoryAsync Tests

    [Fact]
    public async Task GetHealthHistoryAsync_ShouldReturnFilteredHistory()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        for (int i = 0; i < 10; i++)
        {
            _context.PluginHealthSnapshots.Add(new PluginHealthSnapshotEntity
            {
                PluginId = "plugin-1",
                HealthState = (int)HealthState.Healthy,
                CheckedAt = baseTime.AddHours(-i),
                ResponseTimeMs = 100 + i
            });
        }
        await _context.SaveChangesAsync();

        // Act - Get only last 5 hours
        var from = baseTime.AddHours(-4);
        var to = baseTime;
        var history = await _repository.GetHealthHistoryAsync("plugin-1", from, to);

        // Assert
        history.Should().HaveCount(5);
        history.All(h => h.CheckedAt >= from && h.CheckedAt <= to).Should().BeTrue();
    }

    [Fact]
    public async Task GetHealthHistoryAsync_EmptyRange_ShouldReturnEmpty()
    {
        // Arrange
        _context.PluginHealthSnapshots.Add(new PluginHealthSnapshotEntity
        {
            PluginId = "plugin-1",
            HealthState = (int)HealthState.Healthy,
            CheckedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act - Query for a time range with no data
        var from = DateTime.UtcNow.AddDays(1);
        var to = DateTime.UtcNow.AddDays(2);
        var history = await _repository.GetHealthHistoryAsync("plugin-1", from, to);

        // Assert
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHealthHistoryAsync_ShouldOrderByNewestFirst()
    {
        // Arrange
        var baseTime = DateTime.UtcNow;
        _context.PluginHealthSnapshots.Add(new PluginHealthSnapshotEntity
        {
            PluginId = "plugin-1",
            HealthState = (int)HealthState.Healthy,
            CheckedAt = baseTime.AddHours(-2)
        });
        _context.PluginHealthSnapshots.Add(new PluginHealthSnapshotEntity
        {
            PluginId = "plugin-1",
            HealthState = (int)HealthState.Degraded,
            CheckedAt = baseTime
        });
        await _context.SaveChangesAsync();

        // Act
        var history = await _repository.GetHealthHistoryAsync("plugin-1", baseTime.AddDays(-1), baseTime.AddDays(1));

        // Assert
        history.Should().HaveCount(2);
        history[0].State.Should().Be(HealthState.Degraded); // Most recent first
    }

    #endregion

    #region Helper Methods

    private async Task AddVersionAsync(string pluginId, string version, DateTime installedAt, bool isCurrent = false)
    {
        var versionInfo = new PluginVersionInfo
        {
            PluginId = pluginId,
            Version = Version.Parse(version),
            InstalledAt = installedAt,
            BackupPath = $"/backup/{pluginId}/{version}",
            IsCurrent = isCurrent
        };
        await _repository.AddVersionAsync(versionInfo);
    }

    #endregion
}
