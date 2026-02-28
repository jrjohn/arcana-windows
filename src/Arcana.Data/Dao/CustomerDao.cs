using Arcana.Domain.Entities;

namespace Arcana.Data.Dao;

/// <summary>
/// Data Access Object interface for Customer entity.
/// Provides raw database access operations used by the repository layer.
/// </summary>
public interface CustomerDao
{
    /// <summary>
    /// Finds a customer by their unique code.
    /// </summary>
    Task<Customer?> FindByCode(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for active customers matching a search term (name, code, or phone).
    /// </summary>
    Task<IReadOnlyList<Customer>> Search(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a customer code already exists (optionally excluding a given customer ID).
    /// </summary>
    Task<bool> CodeExists(string code, int? excludeId = null, CancellationToken cancellationToken = default);
}
