using Arcana.Domain.Entities;
using FluentValidation;

namespace Arcana.Domain.Validation;

/// <summary>
/// Product entity validator.
/// </summary>
public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Product code cannot be empty")
            .MaximumLength(50).WithMessage("Product code cannot exceed 50 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name cannot be empty")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("Unit cannot be empty")
            .MaximumLength(10).WithMessage("Unit cannot exceed 10 characters");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative");

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0).WithMessage("Cost cannot be negative");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative");

        RuleFor(x => x.MinStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stock level cannot be negative");

        RuleFor(x => x.MaxStockLevel)
            .GreaterThanOrEqualTo(x => x.MinStockLevel).WithMessage("Maximum stock level cannot be less than minimum stock level")
            .When(x => x.MaxStockLevel > 0);

        RuleFor(x => x.Barcode)
            .MaximumLength(50).WithMessage("Barcode cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.Barcode));
    }
}
