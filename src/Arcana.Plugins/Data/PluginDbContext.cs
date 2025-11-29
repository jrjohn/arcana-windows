using Microsoft.EntityFrameworkCore;

namespace Arcana.Plugins.Data;

/// <summary>
/// Database context for plugin data storage.
/// 插件資料儲存的資料庫上下文
/// </summary>
public class PluginDbContext : DbContext
{
    public DbSet<PluginVersionEntity> PluginVersions => Set<PluginVersionEntity>();
    public DbSet<PluginMetadataEntity> PluginMetadata => Set<PluginMetadataEntity>();
    public DbSet<PluginHealthSnapshotEntity> PluginHealthSnapshots => Set<PluginHealthSnapshotEntity>();

    public PluginDbContext(DbContextOptions<PluginDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PluginVersionEntity
        modelBuilder.Entity<PluginVersionEntity>(entity =>
        {
            entity.ToTable("PluginVersions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PluginId);
            entity.HasIndex(e => new { e.PluginId, e.Version }).IsUnique();
        });

        // PluginMetadataEntity
        modelBuilder.Entity<PluginMetadataEntity>(entity =>
        {
            entity.ToTable("PluginMetadata");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PluginId).IsUnique();
        });

        // PluginHealthSnapshotEntity
        modelBuilder.Entity<PluginHealthSnapshotEntity>(entity =>
        {
            entity.ToTable("PluginHealthSnapshots");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PluginId);
            entity.HasIndex(e => e.CheckedAt);
        });
    }
}
