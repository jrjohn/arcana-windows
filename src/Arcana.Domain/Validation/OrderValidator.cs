using Arcana.Domain.Entities;
using FluentValidation;

namespace Arcana.Domain.Validation;

/// <summary>
/// Order entity validator.
/// </summary>
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("Order number cannot be empty")
            .MaximumLength(20).WithMessage("Order number cannot exceed 20 characters");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Please select a customer");

        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name cannot be empty");

        RuleFor(x => x.OrderDate)
            .NotEmpty().WithMessage("Order date cannot be empty");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order items cannot be empty")
            .Must(items => items.Any()).WithMessage("Order must have at least one item");

        RuleFor(x => x.TaxRate)
            .GreaterThanOrEqualTo(0).WithMessage("Tax rate cannot be negative")
            .LessThanOrEqualTo(100).WithMessage("Tax rate cannot exceed 100%");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount amount cannot be negative");

        RuleFor(x => x.ShippingCost)
            .GreaterThanOrEqualTo(0).WithMessage("Shipping cost cannot be negative");

        RuleForEach(x => x.Items).SetValidator(new OrderItemValidator());
    }
}

/// <summary>
/// Order item validator.
/// </summary>
public class OrderItemValidator : AbstractValidator<OrderItem>
{
    public OrderItemValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Please select a product");

        RuleFor(x => x.ProductCode)
            .NotEmpty().WithMessage("Product code cannot be empty");

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Product name cannot be empty");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price cannot be negative");

        RuleFor(x => x.DiscountPercent)
            .GreaterThanOrEqualTo(0).WithMessage("Discount percent cannot be negative")
            .LessThanOrEqualTo(100).WithMessage("Discount percent cannot exceed 100%");
    }
}
