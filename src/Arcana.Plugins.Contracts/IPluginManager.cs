namespace Arcana.Plugins.Contracts;

/// <summary>
/// Plugin manager interface for plugin lifecycle management.
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// Gets all loaded plugins.
    /// </summary>
    IReadOnlyList<IPluginInfo> GetAllPlugins();

    /// <summary>
    /// Gets a plugin by ID.
    /// </summary>
    IPluginInfo? GetPlugin(string pluginId);

    /// <summary>
    /// Activates a plugin.
    /// </summary>
    Task<PluginOperationResult> ActivatePluginAsync(string pluginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a plugin.
    /// </summary>
    Task<PluginOperationResult> DeactivatePluginAsync(string pluginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs a plugin from a package file.
    /// </summary>
    Task<PluginOperationResult> InstallPluginAsync(string packagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs a plugin from a stream.
    /// </summary>
    Task<PluginOperationResult> InstallPluginAsync(Stream packageStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstalls a plugin.
    /// </summary>
    Task<PluginOperationResult> UninstallPluginAsync(string pluginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upgrades a plugin to a new version.
    /// </summary>
    Task<PluginOperationResult> UpgradePluginAsync(string pluginId, string packagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a plugin to a previous version.
    /// </summary>
    Task<PluginOperationResult> RollbackPluginAsync(string pluginId, string targetVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available versions for rollback.
    /// </summary>
    IReadOnlyList<PluginVersionInfo> GetAvailableVersions(string pluginId);

    /// <summary>
    /// Reloads a plugin (hot reload).
    /// </summary>
    Task<PluginOperationResult> ReloadPluginAsync(string pluginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the health status of a plugin.
    /// </summary>
    PluginHealthStatus GetHealthStatus(string pluginId);

    /// <summary>
    /// Checks the health of all plugins.
    /// </summary>
    Task<IReadOnlyList<PluginHealthStatus>> CheckAllHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a plugin state changes.
    /// </summary>
    event EventHandler<PluginStateChangedEventArgs>? PluginStateChanged;

    /// <summary>
    /// Event raised when plugin installation progress changes.
    /// </summary>
    event EventHandler<PluginInstallProgressEventArgs>? InstallProgressChanged;
}

/// <summary>
/// Plugin information interface.
/// </summary>
public interface IPluginInfo
{
    string Id { get; }
    string Name { get; }
    Version Version { get; }
    string? Description { get; }
    string? Author { get; }
    PluginType Type { get; }
    PluginState State { get; }
    string? IconPath { get; }
    string InstallPath { get; }
    DateTime InstalledAt { get; }
    DateTime? LastActivatedAt { get; }
    bool IsBuiltIn { get; }
    bool CanUninstall { get; }
    bool CanUpgrade { get; }
    string[]? Dependencies { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
}

/// <summary>
/// Plugin version information for rollback.
/// </summary>
public record PluginVersionInfo
{
    public required string PluginId { get; init; }
    public required Version Version { get; init; }
    public required DateTime InstalledAt { get; init; }
    public required string BackupPath { get; init; }
    public bool IsCurrent { get; init; }
    public long SizeBytes { get; init; }
    public string? ReleaseNotes { get; init; }
}

/// <summary>
/// Plugin health status.
/// </summary>
public record PluginHealthStatus
{
    public required string PluginId { get; init; }
    public required string PluginName { get; init; }
    public required HealthState State { get; init; }
    public string? Message { get; init; }
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
    public TimeSpan? ResponseTime { get; init; }
    public long MemoryUsageBytes { get; init; }
    public int ErrorCount { get; init; }
    public DateTime? LastErrorAt { get; init; }
    public string? LastError { get; init; }
    public IReadOnlyList<HealthCheckResult>? Details { get; init; }
}

/// <summary>
/// Health state enumeration.
/// </summary>
public enum HealthState
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}

/// <summary>
/// Individual health check result.
/// </summary>
public record HealthCheckResult
{
    public required string CheckName { get; init; }
    public required HealthState State { get; init; }
    public string? Message { get; init; }
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Plugin operation result.
/// </summary>
public record PluginOperationResult
{
    public bool Success { get; init; }
    public string? PluginId { get; init; }
    public string? Message { get; init; }
    public string? ErrorCode { get; init; }
    public Exception? Exception { get; init; }
    public IReadOnlyList<string>? Warnings { get; init; }

    public static PluginOperationResult Succeeded(string pluginId, string? message = null)
        => new() { Success = true, PluginId = pluginId, Message = message };

    public static PluginOperationResult Failed(string? pluginId, string message, string? errorCode = null, Exception? exception = null)
        => new() { Success = false, PluginId = pluginId, Message = message, ErrorCode = errorCode, Exception = exception };
}

/// <summary>
/// Plugin state changed event args.
/// </summary>
public class PluginStateChangedEventArgs : EventArgs
{
    public required string PluginId { get; init; }
    public required PluginState OldState { get; init; }
    public required PluginState NewState { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// Plugin install progress event args.
/// </summary>
public class PluginInstallProgressEventArgs : EventArgs
{
    public required string PluginId { get; init; }
    public required InstallPhase Phase { get; init; }
    public double Progress { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// Installation phase.
/// </summary>
public enum InstallPhase
{
    Downloading,
    Extracting,
    Validating,
    BackingUp,
    Installing,
    Activating,
    Completed,
    Failed,
    RollingBack
}
