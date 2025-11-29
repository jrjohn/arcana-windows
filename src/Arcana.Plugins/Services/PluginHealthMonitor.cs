using System.Collections.Concurrent;
using System.Diagnostics;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;
using Microsoft.Extensions.Logging;

namespace Arcana.Plugins.Services;

/// <summary>
/// Plugin health monitoring service.
/// </summary>
public class PluginHealthMonitor : IDisposable
{
    private readonly ILogger<PluginHealthMonitor> _logger;
    private readonly ConcurrentDictionary<string, PluginHealthStatus> _healthCache = new();
    private readonly ConcurrentDictionary<string, PluginHealthMetrics> _metrics = new();
    private readonly Timer _monitorTimer;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private bool _disposed;

    public event EventHandler<PluginHealthChangedEventArgs>? HealthChanged;

    public PluginHealthMonitor(ILogger<PluginHealthMonitor> logger)
    {
        _logger = logger;
        _monitorTimer = new Timer(OnMonitorTick, null, _checkInterval, _checkInterval);
    }

    /// <summary>
    /// Checks the health of a specific plugin.
    /// </summary>
    public async Task<PluginHealthStatus> CheckHealthAsync(PluginInfo pluginInfo, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var checks = new List<HealthCheckResult>();

        try
        {
            // Basic state check
            checks.Add(new HealthCheckResult
            {
                CheckName = "State",
                State = pluginInfo.State == PluginState.Active ? HealthState.Healthy : HealthState.Degraded,
                Message = $"Plugin state: {pluginInfo.State}",
                Duration = TimeSpan.Zero
            });

            // Check if plugin implements health check interface
            if (pluginInfo.PluginInstance is IPluginHealthCheck healthCheck)
            {
                var checkStopwatch = Stopwatch.StartNew();
                var result = await healthCheck.CheckHealthAsync(cancellationToken);
                checkStopwatch.Stop();

                checks.Add(new HealthCheckResult
                {
                    CheckName = "PluginHealthCheck",
                    State = result.State,
                    Message = result.Message,
                    Duration = checkStopwatch.Elapsed
                });

                if (result.Checks != null)
                {
                    checks.AddRange(result.Checks);
                }
            }

            // Error rate check
            var metrics = GetOrCreateMetrics(pluginInfo.Id);
            var errorRate = metrics.GetErrorRate(TimeSpan.FromMinutes(5));
            checks.Add(new HealthCheckResult
            {
                CheckName = "ErrorRate",
                State = errorRate switch
                {
                    < 0.01 => HealthState.Healthy,
                    < 0.1 => HealthState.Degraded,
                    _ => HealthState.Unhealthy
                },
                Message = $"Error rate: {errorRate:P2}",
                Duration = TimeSpan.Zero
            });

            // Memory check
            var memoryUsage = pluginInfo.MemoryUsageBytes;
            checks.Add(new HealthCheckResult
            {
                CheckName = "Memory",
                State = memoryUsage switch
                {
                    < 100 * 1024 * 1024 => HealthState.Healthy,     // < 100MB
                    < 500 * 1024 * 1024 => HealthState.Degraded,    // < 500MB
                    _ => HealthState.Unhealthy
                },
                Message = $"Memory usage: {memoryUsage / 1024.0 / 1024.0:N2} MB",
                Duration = TimeSpan.Zero
            });

            stopwatch.Stop();

            // Determine overall state
            var overallState = checks.All(c => c.State == HealthState.Healthy)
                ? HealthState.Healthy
                : checks.Any(c => c.State == HealthState.Unhealthy)
                    ? HealthState.Unhealthy
                    : HealthState.Degraded;

            var status = new PluginHealthStatus
            {
                PluginId = pluginInfo.Id,
                PluginName = pluginInfo.Name,
                State = overallState,
                Message = overallState == HealthState.Healthy ? "All checks passed" : "Some checks failed",
                ResponseTime = stopwatch.Elapsed,
                MemoryUsageBytes = memoryUsage,
                ErrorCount = pluginInfo.ErrorCount,
                LastErrorAt = pluginInfo.LastErrorAt,
                LastError = pluginInfo.LastError,
                Details = checks
            };

            // Update cache and notify if changed
            var previousStatus = _healthCache.GetValueOrDefault(pluginInfo.Id);
            _healthCache[pluginInfo.Id] = status;

            if (previousStatus?.State != status.State)
            {
                HealthChanged?.Invoke(this, new PluginHealthChangedEventArgs
                {
                    PluginId = pluginInfo.Id,
                    OldState = previousStatus?.State ?? HealthState.Unknown,
                    NewState = status.State
                });
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for plugin {PluginId}", pluginInfo.Id);

            var status = new PluginHealthStatus
            {
                PluginId = pluginInfo.Id,
                PluginName = pluginInfo.Name,
                State = HealthState.Unhealthy,
                Message = $"Health check failed: {ex.Message}",
                ResponseTime = stopwatch.Elapsed,
                ErrorCount = pluginInfo.ErrorCount + 1,
                LastErrorAt = DateTime.UtcNow,
                LastError = ex.Message,
                Details = checks
            };

            _healthCache[pluginInfo.Id] = status;
            return status;
        }
    }

    /// <summary>
    /// Gets the cached health status.
    /// </summary>
    public PluginHealthStatus? GetCachedHealth(string pluginId)
    {
        return _healthCache.GetValueOrDefault(pluginId);
    }

    /// <summary>
    /// Records an error for a plugin.
    /// </summary>
    public void RecordError(string pluginId, Exception exception)
    {
        var metrics = GetOrCreateMetrics(pluginId);
        metrics.RecordError(exception);
    }

    /// <summary>
    /// Records a successful operation for a plugin.
    /// </summary>
    public void RecordSuccess(string pluginId)
    {
        var metrics = GetOrCreateMetrics(pluginId);
        metrics.RecordSuccess();
    }

    /// <summary>
    /// Gets health metrics for a plugin.
    /// </summary>
    public PluginHealthMetrics? GetMetrics(string pluginId)
    {
        return _metrics.GetValueOrDefault(pluginId);
    }

    private PluginHealthMetrics GetOrCreateMetrics(string pluginId)
    {
        return _metrics.GetOrAdd(pluginId, _ => new PluginHealthMetrics(pluginId));
    }

    private void OnMonitorTick(object? state)
    {
        // This would be called by PluginManager to trigger health checks
        _logger.LogDebug("Health monitor tick");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _monitorTimer.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Plugin health metrics tracking.
/// </summary>
public class PluginHealthMetrics
{
    private readonly string _pluginId;
    private readonly Queue<(DateTime Time, bool Success, string? Error)> _recentOperations = new();
    private readonly object _lock = new();
    private const int MaxOperations = 1000;

    public string PluginId => _pluginId;
    public int TotalOperations { get; private set; }
    public int TotalErrors { get; private set; }
    public DateTime? LastOperationAt { get; private set; }
    public DateTime? LastErrorAt { get; private set; }
    public string? LastError { get; private set; }

    public PluginHealthMetrics(string pluginId)
    {
        _pluginId = pluginId;
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            TotalOperations++;
            LastOperationAt = DateTime.UtcNow;
            AddOperation(true, null);
        }
    }

    public void RecordError(Exception exception)
    {
        lock (_lock)
        {
            TotalOperations++;
            TotalErrors++;
            LastOperationAt = DateTime.UtcNow;
            LastErrorAt = DateTime.UtcNow;
            LastError = exception.Message;
            AddOperation(false, exception.Message);
        }
    }

    public double GetErrorRate(TimeSpan window)
    {
        lock (_lock)
        {
            var cutoff = DateTime.UtcNow - window;
            var recentOps = _recentOperations.Where(o => o.Time >= cutoff).ToList();
            if (recentOps.Count == 0) return 0;
            return (double)recentOps.Count(o => !o.Success) / recentOps.Count;
        }
    }

    private void AddOperation(bool success, string? error)
    {
        _recentOperations.Enqueue((DateTime.UtcNow, success, error));
        while (_recentOperations.Count > MaxOperations)
        {
            _recentOperations.Dequeue();
        }
    }
}

/// <summary>
/// Plugin health changed event args.
/// </summary>
public class PluginHealthChangedEventArgs : EventArgs
{
    public required string PluginId { get; init; }
    public required HealthState OldState { get; init; }
    public required HealthState NewState { get; init; }
}
