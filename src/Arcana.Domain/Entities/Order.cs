namespace Arcana.Domain.Entities;

/// <summary>
/// Order status enumeration.
/// 訂單狀態
/// </summary>
public enum OrderStatus
{
    Draft = 0,       // 草稿
    Pending = 1,     // 待處理
    Confirmed = 2,   // 已確認
    Processing = 3,  // 處理中
    Shipped = 4,     // 已出貨
    Delivered = 5,   // 已送達
    Completed = 6,   // 已完成
    Cancelled = 7    // 已取消
}

/// <summary>
/// Payment status enumeration.
/// 付款狀態
/// </summary>
public enum PaymentStatus
{
    Unpaid = 0,      // 未付款
    PartialPaid = 1, // 部分付款
    Paid = 2,        // 已付款
    Refunded = 3     // 已退款
}

/// <summary>
/// Payment method enumeration.
/// 付款方式
/// </summary>
public enum PaymentMethod
{
    Cash = 0,        // 現金
    CreditCard = 1,  // 信用卡
    BankTransfer = 2,// 銀行轉帳
    Check = 3,       // 支票
    Credit = 4       // 賒帳
}

/// <summary>
/// Order entity (master).
/// 訂單實體 (主檔)
/// </summary>
public class Order : BaseEntity
{
    /// <summary>
    /// Order number.
    /// 訂單編號
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Order date.
    /// 訂單日期
    /// </summary>
    public DateTime OrderDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Customer ID.
    /// 客戶ID
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Customer name (denormalized for display).
    /// 客戶名稱
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Order status.
    /// 訂單狀態
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    /// <summary>
    /// Payment status.
    /// 付款狀態
    /// </summary>
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    /// <summary>
    /// Payment method.
    /// 付款方式
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    /// <summary>
    /// Subtotal (before tax and discount).
    /// 小計
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Tax rate (percentage).
    /// 稅率
    /// </summary>
    public decimal TaxRate { get; set; } = 5m;

    /// <summary>
    /// Tax amount.
    /// 稅額
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Discount amount.
    /// 折扣金額
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Shipping cost.
    /// 運費
    /// </summary>
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Total amount.
    /// 總金額
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Amount paid.
    /// 已付金額
    /// </summary>
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// Shipping address.
    /// 送貨地址
    /// </summary>
    public string? ShippingAddress { get; set; }

    /// <summary>
    /// Shipping city.
    /// 送貨城市
    /// </summary>
    public string? ShippingCity { get; set; }

    /// <summary>
    /// Shipping postal code.
    /// 送貨郵遞區號
    /// </summary>
    public string? ShippingPostalCode { get; set; }

    /// <summary>
    /// Expected delivery date.
    /// 預計送達日期
    /// </summary>
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Actual delivery date.
    /// 實際送達日期
    /// </summary>
    public DateTime? ActualDeliveryDate { get; set; }

    /// <summary>
    /// Notes.
    /// 備註
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Navigation property for customer.
    /// </summary>
    public virtual Customer? Customer { get; set; }

    /// <summary>
    /// Navigation property for order items.
    /// </summary>
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    /// <summary>
    /// Calculates the order totals.
    /// </summary>
    public void CalculateTotals()
    {
        Subtotal = Items.Sum(i => i.LineTotal);
        TaxAmount = Math.Round(Subtotal * TaxRate / 100, 2);
        TotalAmount = Subtotal + TaxAmount + ShippingCost - DiscountAmount;
    }
}

/// <summary>
/// Order item entity (detail).
/// 訂單明細實體
/// </summary>
public class OrderItem : BaseEntity
{
    /// <summary>
    /// Parent order ID.
    /// 訂單ID
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Line number.
    /// 行號
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Product ID.
    /// 產品ID
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Product code (denormalized).
    /// 產品代碼
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Product name (denormalized).
    /// 產品名稱
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Unit of measure.
    /// 單位
    /// </summary>
    public string Unit { get; set; } = "PCS";

    /// <summary>
    /// Quantity.
    /// 數量
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price.
    /// 單價
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Discount percentage.
    /// 折扣率
    /// </summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>
    /// Line total.
    /// 行總計
    /// </summary>
    public decimal LineTotal => Math.Round(Quantity * UnitPrice * (1 - DiscountPercent / 100), 2);

    /// <summary>
    /// Notes.
    /// 備註
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Navigation property for order.
    /// </summary>
    public virtual Order? Order { get; set; }

    /// <summary>
    /// Navigation property for product.
    /// </summary>
    public virtual Product? Product { get; set; }
}
