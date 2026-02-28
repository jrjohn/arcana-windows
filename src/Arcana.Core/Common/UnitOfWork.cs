namespace Arcana.Core.Common;

/// <summary>
/// Unit of Work pattern interface for managing transactions.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets whether changes have been made in this unit of work.
    /// </summary>
    bool HasChanges { get; }

    /// <summary>
    /// Gets whether this unit of work has been committed.
    /// </summary>
    bool IsCommitted { get; }

    /// <summary>
    /// Gets whether this unit of work has been rolled back.
    /// </summary>
    bool IsRolledBack { get; }

    /// <summary>
    /// Commits all changes made in this unit of work.
    /// </summary>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back all changes made in this unit of work.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a repository for the specified entity type.
    /// </summary>
    Repository<T> GetRepository<T>() where T : class;

    /// <summary>
    /// Gets a repository for the specified entity type with a specific key type.
    /// </summary>
    Repository<T, TKey> GetRepository<T, TKey>() where T : class where TKey : notnull;

    /// <summary>
    /// Begins a new transaction scope.
    /// </summary>
    Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Transaction scope for explicit transaction management.
/// </summary>
public interface ITransactionScope : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the transaction ID.
    /// </summary>
    Guid TransactionId { get; }

    /// <summary>
    /// Gets whether this transaction is active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for creating unit of work instances.
/// </summary>
public interface UnitOfWorkFactory
{
    /// <summary>
    /// Creates a new unit of work.
    /// </summary>
    IUnitOfWork Create();

    /// <summary>
    /// Creates a new unit of work with an explicit transaction.
    /// </summary>
    Task<IUnitOfWork> CreateWithTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a unit of work operation.
/// </summary>
public record UnitOfWorkResult
{
    public bool Success { get; init; }
    public int AffectedRows { get; init; }
    public string? Error { get; init; }
    public Exception? Exception { get; init; }

    public static UnitOfWorkResult Succeeded(int affectedRows) =>
        new() { Success = true, AffectedRows = affectedRows };

    public static UnitOfWorkResult Failed(string error, Exception? exception = null) =>
        new() { Success = false, Error = error, Exception = exception };
}
