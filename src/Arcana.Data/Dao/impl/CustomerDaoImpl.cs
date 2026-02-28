using Arcana.Data.Local;
using Arcana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arcana.Data.Dao.Impl;

/// <summary>
/// EF Core implementation of CustomerDao.
/// All direct AppDbContext / LINQ-to-EF calls are isolated here.
/// </summary>
public class CustomerDaoImpl : CustomerDao
{
    private readonly AppDbContext _context;

    public CustomerDaoImpl(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> FindByCode(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> Search(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await _context.Customers
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Take(maxResults)
                .ToListAsync(cancellationToken);
        }

        var term = searchTerm.ToLower();
        return await _context.Customers
            .Where(c => c.IsActive &&
                       (c.Code.ToLower().Contains(term) ||
                        c.Name.ToLower().Contains(term) ||
                        (c.Phone != null && c.Phone.Contains(term))))
            .OrderBy(c => c.Name)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CodeExists(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.Where(c => c.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
