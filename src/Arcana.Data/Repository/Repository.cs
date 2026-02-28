using System.Linq.Expressions;
using Arcana.Core.Common;
using Arcana.Data.Local;
using Microsoft.EntityFrameworkCore;
using CoreCommon = Arcana.Core.Common;

namespace Arcana.Data.Repository;

/// <summary>
/// Generic repository implementation using Entity Framework Core.
/// </summary>
public class RepositoryImpl<TEntity, TKey> : CoreCommon.Repository<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    public RepositoryImpl(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        // Apply sorting if specified
        if (!string.IsNullOrEmpty(request.SortBy))
        {
            query = request.Descending
                ? query.OrderByDescending(e => EF.Property<object>(e, request.SortBy))
                : query.OrderBy(e => EF.Property<object>(e, request.SortBy));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>(items, request.Page, request.PageSize, totalCount);
    }

    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        return predicate == null
            ? await DbSet.CountAsync(cancellationToken)
            : await DbSet.CountAsync(predicate, cancellationToken);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        // Mark for sync if syncable
        if (entity is ISyncable syncable)
        {
            syncable.IsPendingSync = true;
        }

        await DbSet.AddAsync(entity, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            if (entity is ISyncable syncable)
            {
                syncable.IsPendingSync = true;
            }
        }

        await DbSet.AddRangeAsync(entities, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is ISyncable syncable)
        {
            syncable.IsPendingSync = true;
        }

        DbSet.Update(entity);
        await SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        // Soft delete if supported
        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.IsDeleted = true;
            softDeletable.DeletedAt = DateTime.UtcNow;

            if (entity is ISyncable syncable)
            {
                syncable.IsPendingSync = true;
            }

            DbSet.Update(entity);
        }
        else
        {
            DbSet.Remove(entity);
        }

        await SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            await DeleteAsync(entity, cancellationToken);
        }
    }

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual IQueryable<TEntity> Query()
    {
        return DbSet.AsQueryable();
    }
}

/// <summary>
/// Repository implementation with int primary key.
/// </summary>
public class RepositoryImpl<TEntity> : RepositoryImpl<TEntity, int>, CoreCommon.Repository<TEntity>
    where TEntity : class
{
    public RepositoryImpl(AppDbContext context) : base(context)
    {
    }
}
