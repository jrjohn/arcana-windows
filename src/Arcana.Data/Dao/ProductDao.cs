using Arcana.Domain.Entities;

namespace Arcana.Data.Dao;

/// <summary>
/// Data Access Object interface for Product entity.
/// Provides raw database access operations used by the repository layer.
/// </summary>
public interface ProductDao
{
    /// <summary>
    /// Finds a product by its unique code.
    /// </summary>
    Task<Product?> FindByCode(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a product by its barcode.
    /// </summary>
    Task<Product?> FindByBarcode(string barcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for active products matching a search term (name, code, or barcode).
    /// </summary>
    Task<IReadOnlyList<Product>> Search(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all active products within a specific category.
    /// </summary>
    Task<IReadOnlyList<Product>> FindByCategory(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a product code already exists (optionally excluding a given product ID).
    /// </summary>
    Task<bool> CodeExists(string code, int? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all non-deleted product categories, ordered by sort order then name.
    /// </summary>
    Task<IReadOnlyList<ProductCategory>> FindAllCategories(CancellationToken cancellationToken = default);
}
