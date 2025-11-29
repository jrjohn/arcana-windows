using Arcana.Domain.Entities;
using FluentValidation;

namespace Arcana.Domain.Validation;

/// <summary>
/// Product entity validator.
/// 產品實體驗證器
/// </summary>
public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("產品代碼不可為空")
            .MaximumLength(50).WithMessage("產品代碼不可超過50個字元");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("產品名稱不可為空")
            .MaximumLength(200).WithMessage("產品名稱不可超過200個字元");

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("單位不可為空")
            .MaximumLength(10).WithMessage("單位不可超過10個字元");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("售價不可為負數");

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0).WithMessage("成本不可為負數");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("庫存數量不可為負數");

        RuleFor(x => x.MinStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("最低庫存不可為負數");

        RuleFor(x => x.MaxStockLevel)
            .GreaterThanOrEqualTo(x => x.MinStockLevel).WithMessage("最高庫存不可小於最低庫存")
            .When(x => x.MaxStockLevel > 0);

        RuleFor(x => x.Barcode)
            .MaximumLength(50).WithMessage("條碼不可超過50個字元")
            .When(x => !string.IsNullOrEmpty(x.Barcode));
    }
}
