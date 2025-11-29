using Arcana.Data.Local;
using Arcana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arcana.Data.Repository;

/// <summary>
/// Customer repository interface with customer-specific operations.
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> SearchAsync(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Customer repository implementation.
/// </summary>
public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Customer?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> SearchAsync(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await DbSet
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Take(maxResults)
                .ToListAsync(cancellationToken);
        }

        var term = searchTerm.ToLower();
        return await DbSet
            .Where(c => c.IsActive &&
                       (c.Code.ToLower().Contains(term) ||
                        c.Name.ToLower().Contains(term) ||
                        (c.Phone != null && c.Phone.Contains(term))))
            .OrderBy(c => c.Name)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(c => c.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
