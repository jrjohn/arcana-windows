namespace Arcana.Domain.Entities;

/// <summary>
/// Customer entity.
/// 客戶實體
/// </summary>
public class Customer : BaseEntity
{
    /// <summary>
    /// Customer code (unique identifier).
    /// 客戶代碼
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Customer name.
    /// 客戶名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Contact person name.
    /// 聯絡人
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// Contact phone number.
    /// 電話
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Contact email address.
    /// 電子郵件
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Billing address.
    /// 帳單地址
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// City.
    /// 城市
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// State or province.
    /// 州/省
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Postal/ZIP code.
    /// 郵遞區號
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Country.
    /// 國家
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Tax identification number.
    /// 統一編號
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>
    /// Credit limit.
    /// 信用額度
    /// </summary>
    public decimal CreditLimit { get; set; }

    /// <summary>
    /// Current balance.
    /// 目前餘額
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Whether the customer is active.
    /// 是否啟用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Notes.
    /// 備註
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Navigation property for orders.
    /// </summary>
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
