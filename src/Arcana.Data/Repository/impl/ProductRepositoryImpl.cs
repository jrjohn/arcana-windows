using Arcana.Data.Dao;
using Arcana.Data.Local;
using Arcana.Domain.Entities;

namespace Arcana.Data.Repository.Impl;

/// <summary>
/// Product repository implementation.
/// Generic CRUD operations are provided by the base RepositoryImpl class.
/// Product-specific queries are delegated to ProductDao.
/// </summary>
public class ProductRepositoryImpl : RepositoryImpl<Product>, ProductRepository
{
    private readonly ProductDao _dao;

    public ProductRepositoryImpl(AppDbContext context, ProductDao dao) : base(context)
    {
        _dao = dao;
    }

    public Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => _dao.FindByCode(code, cancellationToken);

    public Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
        => _dao.FindByBarcode(barcode, cancellationToken);

    public Task<IReadOnlyList<Product>> SearchAsync(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default)
        => _dao.Search(searchTerm, maxResults, cancellationToken);

    public Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
        => _dao.FindByCategory(categoryId, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
        => _dao.CodeExists(code, excludeId, cancellationToken);

    public Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => _dao.FindAllCategories(cancellationToken);
}
