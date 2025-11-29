namespace Arcana.Plugins.Contracts;

/// <summary>
/// Interface for plugins that support health checks.
/// </summary>
public interface IPluginHealthCheck
{
    /// <summary>
    /// Performs a health check on the plugin.
    /// </summary>
    Task<PluginHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Plugin health check result.
/// </summary>
public record PluginHealthCheckResult
{
    public required HealthState State { get; init; }
    public string? Message { get; init; }
    public IReadOnlyDictionary<string, object>? Data { get; init; }
    public IReadOnlyList<HealthCheckResult>? Checks { get; init; }

    public static PluginHealthCheckResult Healthy(string? message = null)
        => new() { State = HealthState.Healthy, Message = message };

    public static PluginHealthCheckResult Degraded(string message)
        => new() { State = HealthState.Degraded, Message = message };

    public static PluginHealthCheckResult Unhealthy(string message)
        => new() { State = HealthState.Unhealthy, Message = message };
}

/// <summary>
/// Interface for plugins that support hot reload.
/// </summary>
public interface IPluginHotReload
{
    /// <summary>
    /// Called before the plugin is reloaded.
    /// </summary>
    Task OnBeforeReloadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after the plugin is reloaded.
    /// </summary>
    Task OnAfterReloadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the state that should be preserved during reload.
    /// </summary>
    Task<IDictionary<string, object>> GetStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores the state after reload.
    /// </summary>
    Task RestoreStateAsync(IDictionary<string, object> state, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for plugins that support upgrade/rollback.
/// </summary>
public interface IPluginUpgradeable
{
    /// <summary>
    /// Called before upgrade to prepare for migration.
    /// </summary>
    Task<bool> OnBeforeUpgradeAsync(Version fromVersion, Version toVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after upgrade to perform migration.
    /// </summary>
    Task OnAfterUpgradeAsync(Version fromVersion, Version toVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called before rollback.
    /// </summary>
    Task<bool> OnBeforeRollbackAsync(Version fromVersion, Version toVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after rollback.
    /// </summary>
    Task OnAfterRollbackAsync(Version fromVersion, Version toVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the plugin can be upgraded to the specified version.
    /// </summary>
    bool CanUpgradeTo(Version targetVersion);

    /// <summary>
    /// Gets whether the plugin can be rolled back to the specified version.
    /// </summary>
    bool CanRollbackTo(Version targetVersion);
}
