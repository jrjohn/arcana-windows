namespace Arcana.Domain.Entities;

/// <summary>
/// Customer entity.
/// </summary>
public class Customer : BaseEntity
{
    /// <summary>
    /// Customer code (unique identifier).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Customer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Contact person name.
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// Contact phone number.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Contact email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Billing address.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// City.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// State or province.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Postal/ZIP code.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Country.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Tax identification number.
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>
    /// Credit limit.
    /// </summary>
    public decimal CreditLimit { get; set; }

    /// <summary>
    /// Current balance.
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Whether the customer is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Navigation property for orders.
    /// </summary>
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
