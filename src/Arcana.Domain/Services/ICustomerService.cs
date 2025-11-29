using Arcana.Core.Common;
using Arcana.Domain.Entities;

namespace Arcana.Domain.Services;

/// <summary>
/// Customer service interface.
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Gets all customers with paging.
    /// </summary>
    Task<Result<PagedResult<Customer>>> GetCustomersAsync(PageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    Task<Result<Customer>> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a customer by code.
    /// </summary>
    Task<Result<Customer>> GetCustomerByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches customers by name or code.
    /// </summary>
    Task<Result<IReadOnlyList<Customer>>> SearchCustomersAsync(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    Task<Result<Customer>> CreateCustomerAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing customer.
    /// </summary>
    Task<Result<Customer>> UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a customer (soft delete).
    /// </summary>
    Task<Result> DeleteCustomerAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a customer code already exists.
    /// </summary>
    Task<bool> CustomerCodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
}
