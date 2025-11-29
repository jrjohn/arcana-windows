namespace Arcana.Domain.Entities;

/// <summary>
/// Product entity.
/// 產品實體
/// </summary>
public class Product : BaseEntity
{
    /// <summary>
    /// Product code (SKU).
    /// 產品代碼
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Product name.
    /// 產品名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Product description.
    /// 產品描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category ID.
    /// 分類ID
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Unit of measure.
    /// 單位
    /// </summary>
    public string Unit { get; set; } = "PCS";

    /// <summary>
    /// Standard selling price.
    /// 標準售價
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Cost price.
    /// 成本價
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Current stock quantity.
    /// 庫存數量
    /// </summary>
    public decimal StockQuantity { get; set; }

    /// <summary>
    /// Minimum stock level.
    /// 最低庫存
    /// </summary>
    public decimal MinStockLevel { get; set; }

    /// <summary>
    /// Maximum stock level.
    /// 最高庫存
    /// </summary>
    public decimal MaxStockLevel { get; set; }

    /// <summary>
    /// Whether the product is active.
    /// 是否啟用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Barcode.
    /// 條碼
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// Weight in kg.
    /// 重量(公斤)
    /// </summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// Navigation property for category.
    /// </summary>
    public virtual ProductCategory? Category { get; set; }
}

/// <summary>
/// Product category entity.
/// 產品分類
/// </summary>
public class ProductCategory : BaseEntity
{
    /// <summary>
    /// Category code.
    /// 分類代碼
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Category name.
    /// 分類名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parent category ID.
    /// 父分類ID
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// Sort order.
    /// 排序
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Navigation property for parent category.
    /// </summary>
    public virtual ProductCategory? Parent { get; set; }

    /// <summary>
    /// Navigation property for child categories.
    /// </summary>
    public virtual ICollection<ProductCategory> Children { get; set; } = new List<ProductCategory>();

    /// <summary>
    /// Navigation property for products.
    /// </summary>
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
