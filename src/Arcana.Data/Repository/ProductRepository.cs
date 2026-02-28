using Arcana.Core.Common;
using Arcana.Domain.Entities;

namespace Arcana.Data.Repository;

/// <summary>
/// Product repository interface with product-specific query operations.
/// </summary>
public interface ProductRepository : Repository<Product>
{
    Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> SearchAsync(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
