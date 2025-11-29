namespace Arcana.Plugins.Data;

/// <summary>
/// Entity for storing plugin version history in SQLite.
/// </summary>
public class PluginVersionEntity
{
    public int Id { get; set; }
    public required string PluginId { get; set; }
    public required string Version { get; set; }
    public DateTime InstalledAt { get; set; }
    public required string BackupPath { get; set; }
    public bool IsCurrent { get; set; }
    public long SizeBytes { get; set; }
    public string? ReleaseNotes { get; set; }
    public string? Checksum { get; set; }
}

/// <summary>
/// Entity for storing plugin metadata.
/// </summary>
public class PluginMetadataEntity
{
    public int Id { get; set; }
    public required string PluginId { get; set; }
    public required string Name { get; set; }
    public required string Version { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public int Type { get; set; }
    public int State { get; set; }
    public string? IconPath { get; set; }
    public required string InstallPath { get; set; }
    public DateTime InstalledAt { get; set; }
    public DateTime? LastActivatedAt { get; set; }
    public bool IsBuiltIn { get; set; }
    public string? DependenciesJson { get; set; }
    public string? MetadataJson { get; set; }
    public int ErrorCount { get; set; }
    public DateTime? LastErrorAt { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Entity for storing plugin health snapshots.
/// </summary>
public class PluginHealthSnapshotEntity
{
    public int Id { get; set; }
    public required string PluginId { get; set; }
    public DateTime CheckedAt { get; set; }
    public int HealthState { get; set; }
    public string? Message { get; set; }
    public long ResponseTimeMs { get; set; }
    public long MemoryUsageBytes { get; set; }
    public int ErrorCount { get; set; }
    public string? DetailsJson { get; set; }
}
