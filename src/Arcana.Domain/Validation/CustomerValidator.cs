using Arcana.Domain.Entities;
using FluentValidation;

namespace Arcana.Domain.Validation;

/// <summary>
/// Customer entity validator.
/// 客戶實體驗證器
/// </summary>
public class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("客戶代碼不可為空")
            .MaximumLength(20).WithMessage("客戶代碼不可超過20個字元");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("客戶名稱不可為空")
            .MaximumLength(100).WithMessage("客戶名稱不可超過100個字元");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("電子郵件格式不正確")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("電話號碼不可超過20個字元")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("信用額度不可為負數");

        RuleFor(x => x.TaxId)
            .MaximumLength(20).WithMessage("統一編號不可超過20個字元")
            .When(x => !string.IsNullOrEmpty(x.TaxId));
    }
}
