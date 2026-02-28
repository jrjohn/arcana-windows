using Arcana.Data.Local;
using Arcana.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Arcana.Data.Dao.Impl;

/// <summary>
/// EF Core implementation of ProductDao.
/// All direct AppDbContext / LINQ-to-EF calls are isolated here.
/// </summary>
public class ProductDaoImpl : ProductDao
{
    private readonly AppDbContext _context;

    public ProductDaoImpl(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> FindByCode(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<Product?> FindByBarcode(string barcode, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Barcode == barcode, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> Search(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await _context.Products
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Take(maxResults)
                .ToListAsync(cancellationToken);
        }

        var term = searchTerm.ToLower();
        return await _context.Products
            .Where(p => p.IsActive &&
                       (p.Code.ToLower().Contains(term) ||
                        p.Name.ToLower().Contains(term) ||
                        (p.Barcode != null && p.Barcode.Contains(term))))
            .OrderBy(p => p.Name)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> FindByCategory(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CodeExists(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.Where(p => p.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductCategory>> FindAllCategories(CancellationToken cancellationToken = default)
    {
        return await _context.ProductCategories
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
