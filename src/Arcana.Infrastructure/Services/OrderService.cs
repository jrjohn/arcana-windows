using Arcana.Core.Common;
using Arcana.Data.Repository;
using Arcana.Domain.Entities;
using Arcana.Domain.Services;
using Arcana.Domain.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Arcana.Infrastructure.Services;

/// <summary>
/// Order service implementation.
/// 訂單服務實作
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IValidator<Order> _validator;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IValidator<Order> validator,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<Order>>> GetOrdersAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _orderRepository.GetPagedAsync(request, cancellationToken);
            return Result<PagedResult<Order>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders");
            return Result<PagedResult<Order>>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get orders", ex));
        }
    }

    public async Task<Result<Order>> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await _orderRepository.GetByIdWithItemsAsync(id, cancellationToken);
            if (order == null)
            {
                return Result<Order>.Failure(new AppError.Data(ErrorCode.NotFound, $"Order with ID {id} not found"));
            }
            return Result<Order>.Success(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId}", id);
            return Result<Order>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get order", ex));
        }
    }

    public async Task<Result<Order>> GetOrderByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await _orderRepository.GetByOrderNumberAsync(orderNumber, cancellationToken);
            if (order == null)
            {
                return Result<Order>.Failure(new AppError.Data(ErrorCode.NotFound, $"Order {orderNumber} not found"));
            }
            return Result<Order>.Success(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderNumber}", orderNumber);
            return Result<Order>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get order", ex));
        }
    }

    public async Task<Result<Order>> CreateOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate order number if not provided
            if (string.IsNullOrEmpty(order.OrderNumber))
            {
                order.OrderNumber = await GenerateOrderNumberAsync(cancellationToken);
            }

            // Calculate totals
            order.CalculateTotals();

            // Validate
            var validationResult = await _validator.ValidateAsync(order, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<Order>.Failure(new AppError.Validation(ErrorCode.ValidationFailed, "Order validation failed", errors));
            }

            // Set line numbers
            var lineNumber = 1;
            foreach (var item in order.Items)
            {
                item.LineNumber = lineNumber++;
            }

            var created = await _orderRepository.AddAsync(order, cancellationToken);
            _logger.LogInformation("Created order {OrderNumber}", order.OrderNumber);

            return Result<Order>.Success(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return Result<Order>.Failure(new AppError.Data(ErrorCode.DatabaseError, "Failed to create order", ex));
        }
    }

    public async Task<Result<Order>> UpdateOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _orderRepository.GetByIdAsync(order.Id, cancellationToken);
            if (existing == null)
            {
                return Result<Order>.Failure(new AppError.Data(ErrorCode.NotFound, $"Order with ID {order.Id} not found"));
            }

            // Calculate totals
            order.CalculateTotals();

            // Validate
            var validationResult = await _validator.ValidateAsync(order, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<Order>.Failure(new AppError.Validation(ErrorCode.ValidationFailed, "Order validation failed", errors));
            }

            await _orderRepository.UpdateAsync(order, cancellationToken);
            _logger.LogInformation("Updated order {OrderNumber}", order.OrderNumber);

            return Result<Order>.Success(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId}", order.Id);
            return Result<Order>.Failure(new AppError.Data(ErrorCode.DatabaseError, "Failed to update order", ex));
        }
    }

    public async Task<Result> DeleteOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
            if (order == null)
            {
                return Result.Failure(new AppError.Data(ErrorCode.NotFound, $"Order with ID {id} not found"));
            }

            await _orderRepository.DeleteAsync(order, cancellationToken);
            _logger.LogInformation("Deleted order {OrderNumber}", order.OrderNumber);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order {OrderId}", id);
            return Result.Failure(new AppError.Data(ErrorCode.DatabaseError, "Failed to delete order", ex));
        }
    }

    public async Task<Result<Order>> ChangeStatusAsync(int id, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
            if (order == null)
            {
                return Result<Order>.Failure(new AppError.Data(ErrorCode.NotFound, $"Order with ID {id} not found"));
            }

            order.Status = newStatus;
            await _orderRepository.UpdateAsync(order, cancellationToken);
            _logger.LogInformation("Changed order {OrderNumber} status to {Status}", order.OrderNumber, newStatus);

            return Result<Order>.Success(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing order status {OrderId}", id);
            return Result<Order>.Failure(new AppError.Data(ErrorCode.DatabaseError, "Failed to change order status", ex));
        }
    }

    public async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        return await _orderRepository.GenerateOrderNumberAsync(cancellationToken);
    }

    public async Task<Result<IReadOnlyList<Order>>> GetOrdersByCustomerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var orders = await _orderRepository.GetByCustomerIdAsync(customerId, cancellationToken);
            return Result<IReadOnlyList<Order>>.Success(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders for customer {CustomerId}", customerId);
            return Result<IReadOnlyList<Order>>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get orders", ex));
        }
    }

    public async Task<Result<IReadOnlyList<Order>>> GetOrdersByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            var orders = await _orderRepository.GetByDateRangeAsync(from, to, cancellationToken);
            return Result<IReadOnlyList<Order>>.Success(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders by date range");
            return Result<IReadOnlyList<Order>>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get orders", ex));
        }
    }

    public async Task<Result<IReadOnlyList<Order>>> GetOrdersByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var orders = await _orderRepository.GetByStatusAsync(status, cancellationToken);
            return Result<IReadOnlyList<Order>>.Success(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders by status {Status}", status);
            return Result<IReadOnlyList<Order>>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get orders", ex));
        }
    }
}
