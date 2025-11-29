using Arcana.Domain.Entities;
using Arcana.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace Arcana.Domain.Tests.Validation;

public class OrderValidatorTests
{
    private readonly OrderValidator _validator = new();

    [Fact]
    public void Validate_ValidOrder_ShouldPass()
    {
        // Arrange
        var order = CreateValidOrder();

        // Act
        var result = _validator.Validate(order);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyOrderNumber_ShouldFail()
    {
        // Arrange
        var order = CreateValidOrder();
        order.OrderNumber = string.Empty;

        // Act
        var result = _validator.Validate(order);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderNumber");
    }

    [Fact]
    public void Validate_NoCustomer_ShouldFail()
    {
        // Arrange
        var order = CreateValidOrder();
        order.CustomerId = 0;

        // Act
        var result = _validator.Validate(order);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public void Validate_EmptyItems_ShouldFail()
    {
        // Arrange
        var order = CreateValidOrder();
        order.Items.Clear();

        // Act
        var result = _validator.Validate(order);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void Validate_NegativeTaxRate_ShouldFail()
    {
        // Arrange
        var order = CreateValidOrder();
        order.TaxRate = -5;

        // Act
        var result = _validator.Validate(order);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxRate");
    }

    [Fact]
    public void Validate_TaxRateOver100_ShouldFail()
    {
        // Arrange
        var order = CreateValidOrder();
        order.TaxRate = 150;

        // Act
        var result = _validator.Validate(order);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxRate");
    }

    private static Order CreateValidOrder()
    {
        return new Order
        {
            OrderNumber = "ORD202401010001",
            OrderDate = DateTime.Today,
            CustomerId = 1,
            CustomerName = "Test Customer",
            Status = OrderStatus.Draft,
            TaxRate = 5,
            Items = new List<OrderItem>
            {
                new()
                {
                    LineNumber = 1,
                    ProductId = 1,
                    ProductCode = "PROD001",
                    ProductName = "Test Product",
                    Unit = "PCS",
                    Quantity = 1,
                    UnitPrice = 100
                }
            }
        };
    }
}
