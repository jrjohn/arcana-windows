using Arcana.Data.Dao;
using Arcana.Data.Local;
using Arcana.Domain.Entities;

namespace Arcana.Data.Repository.Impl;

/// <summary>
/// Customer repository implementation.
/// Generic CRUD operations are provided by the base RepositoryImpl class.
/// Customer-specific queries are delegated to CustomerDao.
/// </summary>
public class CustomerRepositoryImpl : RepositoryImpl<Customer>, CustomerRepository
{
    private readonly CustomerDao _dao;

    public CustomerRepositoryImpl(AppDbContext context, CustomerDao dao) : base(context)
    {
        _dao = dao;
    }

    public Task<Customer?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => _dao.FindByCode(code, cancellationToken);

    public Task<IReadOnlyList<Customer>> SearchAsync(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default)
        => _dao.Search(searchTerm, maxResults, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
        => _dao.CodeExists(code, excludeId, cancellationToken);
}
