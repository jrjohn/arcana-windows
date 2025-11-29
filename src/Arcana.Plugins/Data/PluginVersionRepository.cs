using Arcana.Plugins.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Arcana.Plugins.Data;

/// <summary>
/// Repository for plugin version history using SQLite.
/// 使用 SQLite 儲存插件版本歷史的儲存庫
/// </summary>
public class PluginVersionRepository : IPluginVersionRepository
{
    private readonly PluginDbContext _context;

    public PluginVersionRepository(PluginDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PluginVersionInfo>> GetVersionsAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        var entities = await _context.PluginVersions
            .Where(v => v.PluginId == pluginId)
            .OrderByDescending(v => v.InstalledAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => new PluginVersionInfo
        {
            PluginId = e.PluginId,
            Version = Version.Parse(e.Version),
            InstalledAt = e.InstalledAt,
            BackupPath = e.BackupPath,
            IsCurrent = e.IsCurrent,
            SizeBytes = e.SizeBytes,
            ReleaseNotes = e.ReleaseNotes
        }).ToList();
    }

    public async Task<PluginVersionInfo?> GetVersionAsync(string pluginId, string version, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PluginVersions
            .FirstOrDefaultAsync(v => v.PluginId == pluginId && v.Version == version, cancellationToken);

        if (entity == null) return null;

        return new PluginVersionInfo
        {
            PluginId = entity.PluginId,
            Version = Version.Parse(entity.Version),
            InstalledAt = entity.InstalledAt,
            BackupPath = entity.BackupPath,
            IsCurrent = entity.IsCurrent,
            SizeBytes = entity.SizeBytes,
            ReleaseNotes = entity.ReleaseNotes
        };
    }

    public async Task<PluginVersionInfo?> GetCurrentVersionAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PluginVersions
            .FirstOrDefaultAsync(v => v.PluginId == pluginId && v.IsCurrent, cancellationToken);

        if (entity == null) return null;

        return new PluginVersionInfo
        {
            PluginId = entity.PluginId,
            Version = Version.Parse(entity.Version),
            InstalledAt = entity.InstalledAt,
            BackupPath = entity.BackupPath,
            IsCurrent = true,
            SizeBytes = entity.SizeBytes,
            ReleaseNotes = entity.ReleaseNotes
        };
    }

    public async Task AddVersionAsync(PluginVersionInfo versionInfo, CancellationToken cancellationToken = default)
    {
        // Mark all existing versions as not current
        if (versionInfo.IsCurrent)
        {
            await _context.PluginVersions
                .Where(v => v.PluginId == versionInfo.PluginId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.IsCurrent, false), cancellationToken);
        }

        var entity = new PluginVersionEntity
        {
            PluginId = versionInfo.PluginId,
            Version = versionInfo.Version.ToString(),
            InstalledAt = versionInfo.InstalledAt,
            BackupPath = versionInfo.BackupPath,
            IsCurrent = versionInfo.IsCurrent,
            SizeBytes = versionInfo.SizeBytes,
            ReleaseNotes = versionInfo.ReleaseNotes
        };

        _context.PluginVersions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateVersionAsync(PluginVersionInfo versionInfo, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PluginVersions
            .FirstOrDefaultAsync(v => v.PluginId == versionInfo.PluginId && v.Version == versionInfo.Version.ToString(), cancellationToken);

        if (entity != null)
        {
            // If setting as current, unset others
            if (versionInfo.IsCurrent && !entity.IsCurrent)
            {
                await _context.PluginVersions
                    .Where(v => v.PluginId == versionInfo.PluginId && v.Id != entity.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.IsCurrent, false), cancellationToken);
            }

            entity.IsCurrent = versionInfo.IsCurrent;
            entity.SizeBytes = versionInfo.SizeBytes;
            entity.ReleaseNotes = versionInfo.ReleaseNotes;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteVersionAsync(string pluginId, string version, CancellationToken cancellationToken = default)
    {
        await _context.PluginVersions
            .Where(v => v.PluginId == pluginId && v.Version == version)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DeleteAllVersionsAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        await _context.PluginVersions
            .Where(v => v.PluginId == pluginId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task SetCurrentVersionAsync(string pluginId, string version, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Unset all current
            await _context.PluginVersions
                .Where(v => v.PluginId == pluginId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.IsCurrent, false), cancellationToken);

            // Set new current
            await _context.PluginVersions
                .Where(v => v.PluginId == pluginId && v.Version == version)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.IsCurrent, true), cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RecordHealthSnapshotAsync(string pluginId, PluginHealthStatus status, CancellationToken cancellationToken = default)
    {
        var entity = new PluginHealthSnapshotEntity
        {
            PluginId = pluginId,
            CheckedAt = status.CheckedAt,
            HealthState = (int)status.State,
            Message = status.Message,
            ResponseTimeMs = (long)(status.ResponseTime?.TotalMilliseconds ?? 0),
            MemoryUsageBytes = status.MemoryUsageBytes,
            ErrorCount = status.ErrorCount,
            DetailsJson = status.Details != null
                ? System.Text.Json.JsonSerializer.Serialize(status.Details)
                : null
        };

        _context.PluginHealthSnapshots.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // Keep only last 1000 snapshots per plugin
        var oldSnapshots = await _context.PluginHealthSnapshots
            .Where(h => h.PluginId == pluginId)
            .OrderByDescending(h => h.CheckedAt)
            .Skip(1000)
            .ToListAsync(cancellationToken);

        if (oldSnapshots.Any())
        {
            _context.PluginHealthSnapshots.RemoveRange(oldSnapshots);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<PluginHealthStatus>> GetHealthHistoryAsync(
        string pluginId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.PluginHealthSnapshots
            .Where(h => h.PluginId == pluginId && h.CheckedAt >= from && h.CheckedAt <= to)
            .OrderByDescending(h => h.CheckedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => new PluginHealthStatus
        {
            PluginId = e.PluginId,
            PluginName = "", // Would need to join with metadata
            State = (HealthState)e.HealthState,
            Message = e.Message,
            CheckedAt = e.CheckedAt,
            ResponseTime = TimeSpan.FromMilliseconds(e.ResponseTimeMs),
            MemoryUsageBytes = e.MemoryUsageBytes,
            ErrorCount = e.ErrorCount
        }).ToList();
    }
}

/// <summary>
/// Interface for plugin version repository.
/// </summary>
public interface IPluginVersionRepository
{
    Task<IReadOnlyList<PluginVersionInfo>> GetVersionsAsync(string pluginId, CancellationToken cancellationToken = default);
    Task<PluginVersionInfo?> GetVersionAsync(string pluginId, string version, CancellationToken cancellationToken = default);
    Task<PluginVersionInfo?> GetCurrentVersionAsync(string pluginId, CancellationToken cancellationToken = default);
    Task AddVersionAsync(PluginVersionInfo versionInfo, CancellationToken cancellationToken = default);
    Task UpdateVersionAsync(PluginVersionInfo versionInfo, CancellationToken cancellationToken = default);
    Task DeleteVersionAsync(string pluginId, string version, CancellationToken cancellationToken = default);
    Task DeleteAllVersionsAsync(string pluginId, CancellationToken cancellationToken = default);
    Task SetCurrentVersionAsync(string pluginId, string version, CancellationToken cancellationToken = default);
    Task RecordHealthSnapshotAsync(string pluginId, PluginHealthStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PluginHealthStatus>> GetHealthHistoryAsync(string pluginId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
