using Arcana.Core.Common;
using Arcana.Domain.Entities;

namespace Arcana.Domain.Services;

/// <summary>
/// Order service interface.
/// </summary>
public interface OrderService
{
    /// <summary>
    /// Gets all orders with paging.
    /// </summary>
    Task<Result<PagedResult<Order>>> GetOrdersAsync(PageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an order by ID.
    /// </summary>
    Task<Result<Order>> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an order by order number.
    /// </summary>
    Task<Result<Order>> GetOrderByNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new order.
    /// </summary>
    Task<Result<Order>> CreateOrderAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing order.
    /// </summary>
    Task<Result<Order>> UpdateOrderAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an order (soft delete).
    /// </summary>
    Task<Result> DeleteOrderAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the order status.
    /// </summary>
    Task<Result<Order>> ChangeStatusAsync(int id, OrderStatus newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the next order number.
    /// </summary>
    Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders by customer ID.
    /// </summary>
    Task<Result<IReadOnlyList<Order>>> GetOrdersByCustomerAsync(int customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders by date range.
    /// </summary>
    Task<Result<IReadOnlyList<Order>>> GetOrdersByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders by status.
    /// </summary>
    Task<Result<IReadOnlyList<Order>>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
}
