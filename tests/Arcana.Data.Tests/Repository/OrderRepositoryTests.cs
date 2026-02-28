using Arcana.Data.Dao.Impl;
using Arcana.Data.Local;
using Arcana.Data.Repository;
using Arcana.Data.Repository.Impl;
using Arcana.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Arcana.Data.Tests.Repository;

public class OrderRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly OrderRepositoryImpl _repository;

    public OrderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new OrderRepositoryImpl(_context, new OrderDaoImpl(_context));

        SeedData();
    }

    private void SeedData()
    {
        var customer = new Customer
        {
            Id = 1,
            Code = "CUST001",
            Name = "Test Customer"
        };
        _context.Customers.Add(customer);

        var orders = new[]
        {
            new Order
            {
                Id = 1,
                OrderNumber = "ORD20240101001",
                OrderDate = DateTime.Today,
                CustomerId = 1,
                CustomerName = "Test Customer",
                Status = OrderStatus.Draft,
                TotalAmount = 1000
            },
            new Order
            {
                Id = 2,
                OrderNumber = "ORD20240101002",
                OrderDate = DateTime.Today.AddDays(-1),
                CustomerId = 1,
                CustomerName = "Test Customer",
                Status = OrderStatus.Confirmed,
                TotalAmount = 2000
            },
            new Order
            {
                Id = 3,
                OrderNumber = "ORD20240101003",
                OrderDate = DateTime.Today.AddDays(-2),
                CustomerId = 1,
                CustomerName = "Test Customer",
                Status = OrderStatus.Completed,
                TotalAmount = 3000
            }
        };

        _context.Orders.AddRange(orders);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingOrder_ShouldReturnOrder()
    {
        // Act
        var order = await _repository.GetByIdAsync(1);

        // Assert
        order.Should().NotBeNull();
        order!.OrderNumber.Should().Be("ORD20240101001");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingOrder_ShouldReturnNull()
    {
        // Act
        var order = await _repository.GetByIdAsync(999);

        // Assert
        order.Should().BeNull();
    }

    [Fact]
    public async Task GetByOrderNumberAsync_ExistingOrder_ShouldReturnOrder()
    {
        // Act
        var order = await _repository.GetByOrderNumberAsync("ORD20240101001");

        // Assert
        order.Should().NotBeNull();
        order!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnOrdersWithStatus()
    {
        // Act
        var orders = await _repository.GetByStatusAsync(OrderStatus.Draft);

        // Assert
        orders.Should().HaveCount(1);
        orders[0].OrderNumber.Should().Be("ORD20240101001");
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ShouldReturnCustomerOrders()
    {
        // Act
        var orders = await _repository.GetByCustomerIdAsync(1);

        // Assert
        orders.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnOrdersInRange()
    {
        // Act
        var orders = await _repository.GetByDateRangeAsync(
            DateTime.Today.AddDays(-1),
            DateTime.Today);

        // Assert
        orders.Should().HaveCount(2);
    }

    [Fact]
    public async Task GenerateOrderNumberAsync_ShouldGenerateSequentialNumber()
    {
        // Act
        var orderNumber = await _repository.GenerateOrderNumberAsync();

        // Assert
        orderNumber.Should().StartWith($"ORD{DateTime.Today:yyyyMMdd}");
    }

    [Fact]
    public async Task AddAsync_ShouldAddNewOrder()
    {
        // Arrange
        var newOrder = new Order
        {
            OrderNumber = "ORD20240102001",
            OrderDate = DateTime.Today,
            CustomerId = 1,
            CustomerName = "Test Customer",
            Status = OrderStatus.Draft
        };

        // Act
        var result = await _repository.AddAsync(newOrder);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        var saved = await _repository.GetByIdAsync(result.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDelete()
    {
        // Arrange
        var order = await _repository.GetByIdAsync(1);

        // Act
        await _repository.DeleteAsync(order!);

        // Assert
        var deleted = await _context.Orders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == 1);
        deleted!.IsDeleted.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
