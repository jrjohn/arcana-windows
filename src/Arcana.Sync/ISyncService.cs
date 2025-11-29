using Arcana.Core.Common;

namespace Arcana.Sync;

/// <summary>
/// Sync service interface for background synchronization.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Gets the current sync status.
    /// </summary>
    SyncState State { get; }

    /// <summary>
    /// Gets the last sync time.
    /// </summary>
    DateTime? LastSyncTime { get; }

    /// <summary>
    /// Gets the number of pending items to sync.
    /// </summary>
    int PendingCount { get; }

    /// <summary>
    /// Starts the background sync service.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the background sync service.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces an immediate sync.
    /// </summary>
    Task<Result> SyncNowAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues an entity for sync.
    /// </summary>
    Task QueueForSyncAsync<T>(T entity, SyncOperationType operation, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Event raised when sync state changes.
    /// </summary>
    event EventHandler<SyncStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Event raised when sync completes.
    /// </summary>
    event EventHandler<SyncCompletedEventArgs>? SyncCompleted;
}

/// <summary>
/// Sync state enumeration.
/// </summary>
public enum SyncState
{
    Idle,
    Syncing,
    Error,
    Offline
}

/// <summary>
/// Sync operation type.
/// </summary>
public enum SyncOperationType
{
    Create,
    Update,
    Delete
}

/// <summary>
/// Event args for sync state changes.
/// </summary>
public class SyncStateChangedEventArgs : EventArgs
{
    public SyncState OldState { get; }
    public SyncState NewState { get; }
    public string? Message { get; }

    public SyncStateChangedEventArgs(SyncState oldState, SyncState newState, string? message = null)
    {
        OldState = oldState;
        NewState = newState;
        Message = message;
    }
}

/// <summary>
/// Event args for sync completion.
/// </summary>
public class SyncCompletedEventArgs : EventArgs
{
    public bool Success { get; }
    public int ItemsSynced { get; }
    public int ItemsFailed { get; }
    public string? ErrorMessage { get; }
    public TimeSpan Duration { get; }

    public SyncCompletedEventArgs(bool success, int itemsSynced, int itemsFailed, TimeSpan duration, string? errorMessage = null)
    {
        Success = success;
        ItemsSynced = itemsSynced;
        ItemsFailed = itemsFailed;
        Duration = duration;
        ErrorMessage = errorMessage;
    }
}
