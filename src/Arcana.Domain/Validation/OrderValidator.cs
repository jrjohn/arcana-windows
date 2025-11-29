using Arcana.Domain.Entities;
using FluentValidation;

namespace Arcana.Domain.Validation;

/// <summary>
/// Order entity validator.
/// 訂單實體驗證器
/// </summary>
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("訂單編號不可為空")
            .MaximumLength(20).WithMessage("訂單編號不可超過20個字元");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("請選擇客戶");

        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("客戶名稱不可為空");

        RuleFor(x => x.OrderDate)
            .NotEmpty().WithMessage("訂單日期不可為空");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("訂單明細不可為空")
            .Must(items => items.Any()).WithMessage("訂單至少需要一個明細項目");

        RuleFor(x => x.TaxRate)
            .GreaterThanOrEqualTo(0).WithMessage("稅率不可為負數")
            .LessThanOrEqualTo(100).WithMessage("稅率不可超過100%");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("折扣金額不可為負數");

        RuleFor(x => x.ShippingCost)
            .GreaterThanOrEqualTo(0).WithMessage("運費不可為負數");

        RuleForEach(x => x.Items).SetValidator(new OrderItemValidator());
    }
}

/// <summary>
/// Order item validator.
/// 訂單明細驗證器
/// </summary>
public class OrderItemValidator : AbstractValidator<OrderItem>
{
    public OrderItemValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("請選擇產品");

        RuleFor(x => x.ProductCode)
            .NotEmpty().WithMessage("產品代碼不可為空");

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("產品名稱不可為空");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("數量必須大於0");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("單價不可為負數");

        RuleFor(x => x.DiscountPercent)
            .GreaterThanOrEqualTo(0).WithMessage("折扣率不可為負數")
            .LessThanOrEqualTo(100).WithMessage("折扣率不可超過100%");
    }
}
