using Arcana.Domain.Entities;
using FluentValidation;

namespace Arcana.Domain.Validation;

/// <summary>
/// Customer entity validator.
/// </summary>
public class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Customer code cannot be empty")
            .MaximumLength(20).WithMessage("Customer code cannot exceed 20 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Customer name cannot be empty")
            .MaximumLength(100).WithMessage("Customer name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email format is invalid")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("Credit limit cannot be negative");

        RuleFor(x => x.TaxId)
            .MaximumLength(20).WithMessage("Tax ID cannot exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.TaxId));
    }
}
