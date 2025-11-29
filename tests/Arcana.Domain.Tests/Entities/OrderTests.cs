using Arcana.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Arcana.Domain.Tests.Entities;

public class OrderTests
{
    [Fact]
    public void CalculateTotals_SingleItem_ShouldCalculateCorrectly()
    {
        // Arrange
        var order = new Order
        {
            TaxRate = 5,
            Items = new List<OrderItem>
            {
                new()
                {
                    Quantity = 2,
                    UnitPrice = 100,
                    DiscountPercent = 0
                }
            }
        };

        // Act
        order.CalculateTotals();

        // Assert
        order.Subtotal.Should().Be(200);
        order.TaxAmount.Should().Be(10);
        order.TotalAmount.Should().Be(210);
    }

    [Fact]
    public void CalculateTotals_MultipleItems_ShouldCalculateCorrectly()
    {
        // Arrange
        var order = new Order
        {
            TaxRate = 5,
            Items = new List<OrderItem>
            {
                new() { Quantity = 2, UnitPrice = 100, DiscountPercent = 0 },
                new() { Quantity = 3, UnitPrice = 50, DiscountPercent = 0 }
            }
        };

        // Act
        order.CalculateTotals();

        // Assert
        order.Subtotal.Should().Be(350);
        order.TaxAmount.Should().Be(17.5m);
        order.TotalAmount.Should().Be(367.5m);
    }

    [Fact]
    public void CalculateTotals_WithDiscount_ShouldCalculateCorrectly()
    {
        // Arrange
        var order = new Order
        {
            TaxRate = 5,
            DiscountAmount = 50,
            Items = new List<OrderItem>
            {
                new() { Quantity = 2, UnitPrice = 100, DiscountPercent = 0 }
            }
        };

        // Act
        order.CalculateTotals();

        // Assert
        order.Subtotal.Should().Be(200);
        order.TaxAmount.Should().Be(10);
        order.TotalAmount.Should().Be(160); // 200 + 10 - 50
    }

    [Fact]
    public void CalculateTotals_WithShipping_ShouldCalculateCorrectly()
    {
        // Arrange
        var order = new Order
        {
            TaxRate = 5,
            ShippingCost = 100,
            Items = new List<OrderItem>
            {
                new() { Quantity = 2, UnitPrice = 100, DiscountPercent = 0 }
            }
        };

        // Act
        order.CalculateTotals();

        // Assert
        order.Subtotal.Should().Be(200);
        order.TaxAmount.Should().Be(10);
        order.TotalAmount.Should().Be(310); // 200 + 10 + 100
    }

    [Fact]
    public void OrderItem_LineTotal_ShouldCalculateCorrectly()
    {
        // Arrange
        var item = new OrderItem
        {
            Quantity = 3,
            UnitPrice = 100,
            DiscountPercent = 10
        };

        // Act
        var lineTotal = item.LineTotal;

        // Assert
        lineTotal.Should().Be(270); // 3 * 100 * 0.9
    }

    [Fact]
    public void OrderItem_LineTotal_NoDiscount_ShouldCalculateCorrectly()
    {
        // Arrange
        var item = new OrderItem
        {
            Quantity = 5,
            UnitPrice = 200,
            DiscountPercent = 0
        };

        // Act
        var lineTotal = item.LineTotal;

        // Assert
        lineTotal.Should().Be(1000);
    }
}
