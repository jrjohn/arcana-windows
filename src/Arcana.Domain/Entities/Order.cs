namespace Arcana.Domain.Entities;

/// <summary>
/// Order status enumeration.
/// </summary>
public enum OrderStatus
{
    Draft = 0,       // Draft
    Pending = 1,     // Pending
    Confirmed = 2,   // Confirmed
    Processing = 3,  // Processing
    Shipped = 4,     // Shipped
    Delivered = 5,   // Delivered
    Completed = 6,   // Completed
    Cancelled = 7    // Cancelled
}

/// <summary>
/// Payment status enumeration.
/// </summary>
public enum PaymentStatus
{
    Unpaid = 0,      // Unpaid
    PartialPaid = 1, // Partial paid
    Paid = 2,        // Paid
    Refunded = 3     // Refunded
}

/// <summary>
/// Payment method enumeration.
/// </summary>
public enum PaymentMethod
{
    Cash = 0,        // Cash
    CreditCard = 1,  // Credit card
    BankTransfer = 2,// Bank transfer
    Check = 3,       // Check
    Credit = 4       // Credit
}

/// <summary>
/// Order entity (master).
/// </summary>
public class Order : BaseEntity
{
    /// <summary>
    /// Order number.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Order date.
    /// </summary>
    public DateTime OrderDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Customer ID.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Customer name (denormalized for display).
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Order status.
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    /// <summary>
    /// Payment status.
    /// </summary>
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    /// <summary>
    /// Payment method.
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    /// <summary>
    /// Subtotal (before tax and discount).
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Tax rate (percentage).
    /// </summary>
    public decimal TaxRate { get; set; } = 5m;

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Discount amount.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Shipping cost.
    /// </summary>
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Total amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Amount paid.
    /// </summary>
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// Shipping address.
    /// </summary>
    public string? ShippingAddress { get; set; }

    /// <summary>
    /// Shipping city.
    /// </summary>
    public string? ShippingCity { get; set; }

    /// <summary>
    /// Shipping postal code.
    /// </summary>
    public string? ShippingPostalCode { get; set; }

    /// <summary>
    /// Expected delivery date.
    /// </summary>
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Actual delivery date.
    /// </summary>
    public DateTime? ActualDeliveryDate { get; set; }

    /// <summary>
    /// Notes.
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
/// </summary>
public class OrderItem : BaseEntity
{
    /// <summary>
    /// Parent order ID.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Line number.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Product code (denormalized).
    /// </summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>
    /// Product name (denormalized).
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Unit of measure.
    /// </summary>
    public string Unit { get; set; } = "PCS";

    /// <summary>
    /// Quantity.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Discount percentage.
    /// </summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>
    /// Line total.
    /// </summary>
    public decimal LineTotal => Math.Round(Quantity * UnitPrice * (1 - DiscountPercent / 100), 2);

    /// <summary>
    /// Notes.
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
