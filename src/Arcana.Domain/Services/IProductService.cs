using Arcana.Core.Common;
using Arcana.Domain.Entities;

namespace Arcana.Domain.Services;

/// <summary>
/// Product service interface.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Gets all products with paging.
    /// </summary>
    Task<Result<PagedResult<Product>>> GetProductsAsync(PageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by ID.
    /// </summary>
    Task<Result<Product>> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by code.
    /// </summary>
    Task<Result<Product>> GetProductByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches products by name, code, or barcode.
    /// </summary>
    Task<Result<IReadOnlyList<Product>>> SearchProductsAsync(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products by category.
    /// </summary>
    Task<Result<IReadOnlyList<Product>>> GetProductsByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new product.
    /// </summary>
    Task<Result<Product>> CreateProductAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    Task<Result<Product>> UpdateProductAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a product (soft delete).
    /// </summary>
    Task<Result> DeleteProductAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all product categories.
    /// </summary>
    Task<Result<IReadOnlyList<ProductCategory>>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
