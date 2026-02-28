using Arcana.Core.Common;
using Arcana.Domain.Entities;

namespace Arcana.Data.Dao;

/// <summary>
/// Data Access Object interface for Order entity.
/// Provides raw database access operations used by the repository layer.
/// </summary>
public interface OrderDao
{
    /// <summary>
    /// Finds an order by ID, including its line items and customer.
    /// </summary>
    Task<Order?> FindByIdWithItems(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an order by its order number, including line items.
    /// </summary>
    Task<Order?> FindByOrderNumber(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all orders belonging to a specific customer.
    /// </summary>
    Task<IReadOnlyList<Order>> FindByCustomerId(int customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds orders whose order date falls within the given range.
    /// </summary>
    Task<IReadOnlyList<Order>> FindByDateRange(DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds orders by their current status.
    /// </summary>
    Task<IReadOnlyList<Order>> FindByStatus(OrderStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the next sequential order number for today.
    /// </summary>
    Task<string> GenerateOrderNumber(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paged list of orders filtered by optional status, customer, and date range.
    /// </summary>
    Task<PagedResult<Order>> FindPagedWithFilters(
        PageRequest request,
        OrderStatus? status = null,
        int? customerId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}
