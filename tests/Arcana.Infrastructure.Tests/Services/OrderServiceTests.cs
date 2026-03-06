using Arcana.Core.Common;
using Arcana.Data.Repository;
using Arcana.Domain.Entities;
using Arcana.Infrastructure.Services.Impl;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Infrastructure.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<OrderRepository> _orderRepoMock;
    private readonly Mock<CustomerRepository> _customerRepoMock;
    private readonly Mock<IValidator<Order>> _validatorMock;
    private readonly Mock<ILogger<OrderServiceImpl>> _loggerMock;
    private readonly OrderServiceImpl _service;

    public OrderServiceTests()
    {
        _orderRepoMock = new Mock<OrderRepository>();
        _customerRepoMock = new Mock<CustomerRepository>();
        _validatorMock = new Mock<IValidator<Order>>();
        _loggerMock = new Mock<ILogger<OrderServiceImpl>>();
        _service = new OrderServiceImpl(
            _orderRepoMock.Object,
            _customerRepoMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);

        // Default: validation passes
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private static Order CreateOrder(int id = 1, string number = "ORD-001", int customerId = 10) =>
        new Order { Id = id, OrderNumber = number, CustomerId = customerId, CustomerName = "Test Customer" };

    #region GetOrdersAsync

    [Fact]
    public async Task GetOrdersAsync_ShouldReturnPagedResult()
    {
        var orders = new List<Order> { CreateOrder() };
        var paged = new PagedResult<Order>(orders, 1, 20, 1);
        _orderRepoMock.Setup(r => r.GetPagedAsync(It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await _service.GetOrdersAsync(new PageRequest());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOrdersAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _orderRepoMock.Setup(r => r.GetPagedAsync(It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.GetOrdersAsync(new PageRequest());

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region GetOrderByIdAsync

    [Fact]
    public async Task GetOrderByIdAsync_ExistingOrder_ShouldReturnSuccess()
    {
        var order = CreateOrder();
        _orderRepoMock.Setup(r => r.GetByIdWithItemsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _service.GetOrderByIdAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(order);
    }

    [Fact]
    public async Task GetOrderByIdAsync_NotFound_ShouldReturnFailure()
    {
        _orderRepoMock.Setup(r => r.GetByIdWithItemsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _service.GetOrderByIdAsync(99);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrderByIdAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _orderRepoMock.Setup(r => r.GetByIdWithItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.GetOrderByIdAsync(1);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region GetOrderByNumberAsync

    [Fact]
    public async Task GetOrderByNumberAsync_ExistingNumber_ShouldReturnSuccess()
    {
        var order = CreateOrder();
        _orderRepoMock.Setup(r => r.GetByOrderNumberAsync("ORD-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _service.GetOrderByNumberAsync("ORD-001");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(order);
    }

    [Fact]
    public async Task GetOrderByNumberAsync_NotFound_ShouldReturnFailure()
    {
        _orderRepoMock.Setup(r => r.GetByOrderNumberAsync("INVALID", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _service.GetOrderByNumberAsync("INVALID");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrderByNumberAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _orderRepoMock.Setup(r => r.GetByOrderNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.GetOrderByNumberAsync("ORD-001");

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region CreateOrderAsync

    [Fact]
    public async Task CreateOrderAsync_Valid_ShouldReturnSuccess()
    {
        var order = CreateOrder();
        order.OrderNumber = "ORD-001";
        _orderRepoMock.Setup(r => r.AddAsync(order, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await _service.CreateOrderAsync(order);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(order);
    }

    [Fact]
    public async Task CreateOrderAsync_WithEmptyOrderNumber_ShouldGenerateNumber()
    {
        var order = CreateOrder();
        order.OrderNumber = "";
        _orderRepoMock.Setup(r => r.GenerateOrderNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("ORD-AUTO-001");
        _orderRepoMock.Setup(r => r.AddAsync(order, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await _service.CreateOrderAsync(order);

        result.IsSuccess.Should().BeTrue();
        order.OrderNumber.Should().Be("ORD-AUTO-001");
    }

    [Fact]
    public async Task CreateOrderAsync_ValidationFails_ShouldReturnFailure()
    {
        var order = CreateOrder();
        var failures = new List<ValidationFailure> { new ValidationFailure("CustomerId", "Customer required") };
        _validatorMock.Setup(v => v.ValidateAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var result = await _service.CreateOrderAsync(order);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOrderAsync_WithItems_ShouldSetLineNumbers()
    {
        var order = CreateOrder();
        order.Items = new List<OrderItem>
        {
            new OrderItem { ProductId = 1, Quantity = 2, UnitPrice = 100 },
            new OrderItem { ProductId = 2, Quantity = 1, UnitPrice = 50 }
        };
        _orderRepoMock.Setup(r => r.AddAsync(order, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var result = await _service.CreateOrderAsync(order);

        result.IsSuccess.Should().BeTrue();
        order.Items.First().LineNumber.Should().Be(1);
        order.Items.Last().LineNumber.Should().Be(2);
    }

    [Fact]
    public async Task CreateOrderAsync_RepositoryThrows_ShouldReturnFailure()
    {
        var order = CreateOrder();
        _orderRepoMock.Setup(r => r.AddAsync(order, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateOrderAsync(order);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region UpdateOrderAsync

    [Fact]
    public async Task UpdateOrderAsync_Valid_ShouldReturnSuccess()
    {
        var order = CreateOrder();
        _orderRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.UpdateAsync(order, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateOrderAsync(order);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(order);
    }

    [Fact]
    public async Task UpdateOrderAsync_NotFound_ShouldReturnFailure()
    {
        var order = CreateOrder();
        _orderRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var result = await _service.UpdateOrderAsync(order);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateOrderAsync_ValidationFails_ShouldReturnFailure()
    {
        var order = CreateOrder();
        _orderRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        var failures = new List<ValidationFailure> { new ValidationFailure("OrderNumber", "Required") };
        _validatorMock.Setup(v => v.ValidateAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var result = await _service.UpdateOrderAsync(order);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateOrderAsync_RepositoryThrows_ShouldReturnFailure()
    {
        var order = CreateOrder();
        _orderRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateOrderAsync(order);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region DeleteOrderAsync

    [Fact]
    public async Task DeleteOrderAsync_ExistingOrder_ShouldReturnSuccess()
    {
        var order = CreateOrder();
        _orderRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.DeleteAsync(order, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.DeleteOrderAsync(1);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteOrderAsync_NotFound_ShouldReturnFailure()
    {
        _orderRepoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var result = await _service.DeleteOrderAsync(99);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteOrderAsync_RepositoryThrows_ShouldReturnFailure()
    {
        var order = CreateOrder();
        _orderRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.DeleteAsync(order, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteOrderAsync(1);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region ChangeStatusAsync

    [Fact]
    public async Task ChangeStatusAsync_ExistingOrder_ShouldUpdateStatus()
    {
        var order = CreateOrder();
        _orderRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.UpdateAsync(order, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.ChangeStatusAsync(1, OrderStatus.Confirmed);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task ChangeStatusAsync_NotFound_ShouldReturnFailure()
    {
        _orderRepoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var result = await _service.ChangeStatusAsync(99, OrderStatus.Confirmed);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ChangeStatusAsync_RepositoryThrows_ShouldReturnFailure()
    {
        var order = CreateOrder();
        _orderRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _orderRepoMock.Setup(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.ChangeStatusAsync(1, OrderStatus.Cancelled);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region GetOrdersByCustomerAsync

    [Fact]
    public async Task GetOrdersByCustomerAsync_ShouldReturnOrders()
    {
        var orders = new List<Order> { CreateOrder(), CreateOrder(2, "ORD-002") };
        _orderRepoMock.Setup(r => r.GetByCustomerIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Order>)orders);

        var result = await _service.GetOrdersByCustomerAsync(10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrdersByCustomerAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _orderRepoMock.Setup(r => r.GetByCustomerIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.GetOrdersByCustomerAsync(10);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region GetOrdersByDateRangeAsync

    [Fact]
    public async Task GetOrdersByDateRangeAsync_ShouldReturnOrders()
    {
        var orders = new List<Order> { CreateOrder() };
        var from = DateTime.Today.AddDays(-7);
        var to = DateTime.Today;
        _orderRepoMock.Setup(r => r.GetByDateRangeAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Order>)orders);

        var result = await _service.GetOrdersByDateRangeAsync(from, to);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOrdersByDateRangeAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _orderRepoMock.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.GetOrdersByDateRangeAsync(DateTime.Today.AddDays(-7), DateTime.Today);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region GetOrdersByStatusAsync

    [Fact]
    public async Task GetOrdersByStatusAsync_ShouldReturnMatchingOrders()
    {
        var orders = new List<Order> { CreateOrder() };
        _orderRepoMock.Setup(r => r.GetByStatusAsync(OrderStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Order>)orders);

        var result = await _service.GetOrdersByStatusAsync(OrderStatus.Pending);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOrdersByStatusAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _orderRepoMock.Setup(r => r.GetByStatusAsync(It.IsAny<OrderStatus>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.GetOrdersByStatusAsync(OrderStatus.Pending);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region GenerateOrderNumberAsync

    [Fact]
    public async Task GenerateOrderNumberAsync_ShouldReturnGeneratedNumber()
    {
        _orderRepoMock.Setup(r => r.GenerateOrderNumberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("ORD-20260001");

        var result = await _service.GenerateOrderNumberAsync();

        result.Should().Be("ORD-20260001");
    }

    #endregion
}
