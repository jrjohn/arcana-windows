using Arcana.Core.Common;
using Arcana.Domain.Entities;

namespace Arcana.Data.Repository;

/// <summary>
/// Order repository interface with order-specific query operations.
/// </summary>
public interface OrderRepository : Repository<Order>
{
    Task<Order?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken = default);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
    Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<Order>> GetPagedWithFiltersAsync(PageRequest request, OrderStatus? status = null, int? customerId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
}
