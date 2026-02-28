using Arcana.Core.Common;
using Arcana.Domain.Entities;

namespace Arcana.Data.Repository;

/// <summary>
/// Customer repository interface with customer-specific query operations.
/// </summary>
public interface CustomerRepository : Repository<Customer>
{
    Task<Customer?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> SearchAsync(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
}
