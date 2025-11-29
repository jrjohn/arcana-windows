using Arcana.Core.Common;
using Arcana.Data.Local;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using CoreCommon = Arcana.Core.Common;

namespace Arcana.Data.Repository;

/// <summary>
/// Unit of Work implementation using Entity Framework Core.
/// 使用 Entity Framework Core 實現的工作單元
/// </summary>
public class UnitOfWork : CoreCommon.IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, object> _repositories = new();
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public bool HasChanges => _context.ChangeTracker.HasChanges();
    public bool IsCommitted { get; private set; }
    public bool IsRolledBack { get; private set; }

    public UnitOfWork(AppDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    public CoreCommon.IRepository<T> GetRepository<T>() where T : class
    {
        var type = typeof(T);
        if (!_repositories.TryGetValue(type, out var repo))
        {
            // Try to get specialized repository from DI
            var specializedRepo = _serviceProvider.GetService<CoreCommon.IRepository<T>>();
            if (specializedRepo != null)
            {
                _repositories[type] = specializedRepo;
                return specializedRepo;
            }

            // Fall back to generic repository
            repo = new Repository<T>(_context);
            _repositories[type] = repo;
        }
        return (CoreCommon.IRepository<T>)repo;
    }

    public CoreCommon.IRepository<T, TKey> GetRepository<T, TKey>() where T : class where TKey : notnull
    {
        var type = typeof(T);
        var keyType = typeof(TKey);
        var compositeKey = (type, keyType);

        if (!_repositories.TryGetValue(type, out var repo))
        {
            // Try to get specialized repository from DI
            var specializedRepo = _serviceProvider.GetService<CoreCommon.IRepository<T, TKey>>();
            if (specializedRepo != null)
            {
                _repositories[type] = specializedRepo;
                return specializedRepo;
            }

            // Fall back to generic repository
            repo = new Repository<T, TKey>(_context);
            _repositories[type] = repo;
        }
        return (CoreCommon.IRepository<T, TKey>)repo;
    }

    public async Task<CoreCommon.ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new TransactionScope(_transaction, this);
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        if (IsCommitted)
        {
            throw new InvalidOperationException("This unit of work has already been committed");
        }

        if (IsRolledBack)
        {
            throw new InvalidOperationException("This unit of work has been rolled back");
        }

        try
        {
            // Apply audit fields
            ApplyAuditFields();

            var result = await _context.SaveChangesAsync(cancellationToken);

            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }

            IsCommitted = true;
            return result;
        }
        catch
        {
            await RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (IsRolledBack) return;

        try
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
            }

            // Detach all tracked entities
            foreach (var entry in _context.ChangeTracker.Entries().ToList())
            {
                entry.State = EntityState.Detached;
            }
        }
        finally
        {
            IsRolledBack = true;
        }
    }

    private void ApplyAuditFields()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in _context.ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.ModifiedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedAt = now;
                    break;
            }
        }
    }

    internal void ClearTransaction()
    {
        _transaction = null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _repositories.Clear();
            _disposed = true;
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }
            _repositories.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// Transaction scope implementation.
/// </summary>
public class TransactionScope : CoreCommon.ITransactionScope
{
    private readonly IDbContextTransaction _transaction;
    private readonly UnitOfWork _unitOfWork;
    private bool _disposed;

    public Guid TransactionId => _transaction.TransactionId;
    public bool IsActive { get; private set; } = true;

    internal TransactionScope(IDbContextTransaction transaction, UnitOfWork unitOfWork)
    {
        _transaction = transaction;
        _unitOfWork = unitOfWork;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Transaction is no longer active");
        }

        await _transaction.CommitAsync(cancellationToken);
        IsActive = false;
        _unitOfWork.ClearTransaction();
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (!IsActive) return;

        await _transaction.RollbackAsync(cancellationToken);
        IsActive = false;
        _unitOfWork.ClearTransaction();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (IsActive)
            {
                _transaction.Rollback();
                IsActive = false;
                _unitOfWork.ClearTransaction();
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (IsActive)
            {
                await _transaction.RollbackAsync();
                IsActive = false;
                _unitOfWork.ClearTransaction();
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Factory for creating unit of work instances.
/// </summary>
public class UnitOfWorkFactory : CoreCommon.IUnitOfWorkFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public UnitOfWorkFactory(IServiceProvider serviceProvider, IDbContextFactory<AppDbContext> contextFactory)
    {
        _serviceProvider = serviceProvider;
        _contextFactory = contextFactory;
    }

    public CoreCommon.IUnitOfWork Create()
    {
        var context = _contextFactory.CreateDbContext();
        return new UnitOfWork(context, _serviceProvider);
    }

    public async Task<CoreCommon.IUnitOfWork> CreateWithTransactionAsync(CancellationToken cancellationToken = default)
    {
        var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var uow = new UnitOfWork(context, _serviceProvider);
        await uow.BeginTransactionAsync(cancellationToken);
        return uow;
    }
}
