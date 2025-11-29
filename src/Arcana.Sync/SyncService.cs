using System.Text.Json;
using Arcana.Core.Common;
using Arcana.Data.Local;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Arcana.Sync;

/// <summary>
/// Background sync service implementation.
/// 背景同步服務實作
/// </summary>
public class SyncService : ISyncService
{
    private readonly AppDbContext _context;
    private readonly INetworkMonitor _networkMonitor;
    private readonly ILogger<SyncService> _logger;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    private CancellationTokenSource? _cts;
    private Task? _backgroundTask;
    private SyncState _state = SyncState.Idle;
    private DateTime? _lastSyncTime;

    public SyncState State => _state;
    public DateTime? LastSyncTime => _lastSyncTime;
    public int PendingCount => GetPendingCountSafe();

    public event EventHandler<SyncStateChangedEventArgs>? StateChanged;
    public event EventHandler<SyncCompletedEventArgs>? SyncCompleted;

    public SyncService(
        AppDbContext context,
        INetworkMonitor networkMonitor,
        ILogger<SyncService> logger)
    {
        _context = context;
        _networkMonitor = networkMonitor;
        _logger = logger;

        _networkMonitor.StatusChanged += OnNetworkStatusChanged;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _backgroundTask = RunBackgroundSyncAsync(_cts.Token);
        _logger.LogInformation("Sync service started");
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _cts?.Cancel();

        if (_backgroundTask != null)
        {
            await _backgroundTask;
        }

        _logger.LogInformation("Sync service stopped");
    }

    public async Task<Result> SyncNowAsync(CancellationToken cancellationToken = default)
    {
        if (!_networkMonitor.IsOnline)
        {
            SetState(SyncState.Offline, "No network connection");
            return Result.Failure(new AppError.Network(ErrorCode.NetworkUnavailable, "No network connection"));
        }

        return await ExecuteSyncAsync(cancellationToken);
    }

    public async Task QueueForSyncAsync<T>(T entity, SyncOperationType operation, CancellationToken cancellationToken = default) where T : class
    {
        var queueItem = new SyncQueueItem
        {
            EntityType = typeof(T).Name,
            EntityId = GetEntityId(entity),
            Operation = operation switch
            {
                SyncOperationType.Create => SyncOperation.Create,
                SyncOperationType.Update => SyncOperation.Update,
                SyncOperationType.Delete => SyncOperation.Delete,
                _ => SyncOperation.Update
            },
            Payload = JsonSerializer.Serialize(entity),
            CreatedAt = DateTime.UtcNow,
            Status = SyncStatus.Pending
        };

        _context.SyncQueue.Add(queueItem);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Queued {EntityType} {EntityId} for {Operation}", queueItem.EntityType, queueItem.EntityId, operation);
    }

    private async Task RunBackgroundSyncAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait for 5 minutes between syncs
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);

                if (_networkMonitor.IsOnline)
                {
                    await ExecuteSyncAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background sync");
                SetState(SyncState.Error, ex.Message);
            }
        }
    }

    private async Task<Result> ExecuteSyncAsync(CancellationToken cancellationToken)
    {
        if (!await _syncLock.WaitAsync(0, cancellationToken))
        {
            return Result.Success(); // Already syncing
        }

        try
        {
            SetState(SyncState.Syncing);
            var startTime = DateTime.UtcNow;
            var itemsSynced = 0;
            var itemsFailed = 0;

            // Get pending items
            var pendingItems = await _context.SyncQueue
                .Where(q => q.Status == SyncStatus.Pending)
                .OrderBy(q => q.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

            foreach (var item in pendingItems)
            {
                try
                {
                    // TODO: Implement actual API sync here
                    // For now, just mark as completed
                    item.Status = SyncStatus.Completed;
                    itemsSynced++;
                }
                catch (Exception ex)
                {
                    item.Status = SyncStatus.Failed;
                    item.RetryCount++;
                    item.LastError = ex.Message;
                    itemsFailed++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            _lastSyncTime = DateTime.UtcNow;
            var duration = DateTime.UtcNow - startTime;

            SetState(SyncState.Idle);
            SyncCompleted?.Invoke(this, new SyncCompletedEventArgs(itemsFailed == 0, itemsSynced, itemsFailed, duration));

            _logger.LogInformation("Sync completed: {Synced} synced, {Failed} failed in {Duration:N2}s",
                itemsSynced, itemsFailed, duration.TotalSeconds);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed");
            SetState(SyncState.Error, ex.Message);
            return Result.Failure(new AppError.Unknown(ex.Message, ex));
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private int GetPendingCountSafe()
    {
        try
        {
            return _context.SyncQueue.Count(q => q.Status == SyncStatus.Pending);
        }
        catch
        {
            // Table may not exist yet during startup
            return 0;
        }
    }

    private async Task<int> GetPendingCountAsync()
    {
        try
        {
            return await _context.SyncQueue.CountAsync(q => q.Status == SyncStatus.Pending);
        }
        catch
        {
            // Table may not exist yet during startup
            return 0;
        }
    }

    private void SetState(SyncState newState, string? message = null)
    {
        var oldState = _state;
        _state = newState;
        StateChanged?.Invoke(this, new SyncStateChangedEventArgs(oldState, newState, message));
    }

    private void OnNetworkStatusChanged(object? sender, NetworkStatusChangedEventArgs e)
    {
        if (!e.IsOnline)
        {
            SetState(SyncState.Offline, "Network disconnected");
        }
        else if (_state == SyncState.Offline)
        {
            SetState(SyncState.Idle, "Network connected");
        }
    }

    private static string GetEntityId<T>(T entity) where T : class
    {
        var idProperty = typeof(T).GetProperty("Id");
        return idProperty?.GetValue(entity)?.ToString() ?? Guid.NewGuid().ToString();
    }
}
