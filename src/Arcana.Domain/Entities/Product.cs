namespace Arcana.Domain.Entities;

/// <summary>
/// Product entity.
/// </summary>
public class Product : BaseEntity
{
    /// <summary>
    /// Product code (SKU).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Product name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Product description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category ID.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Unit of measure.
    /// </summary>
    public string Unit { get; set; } = "PCS";

    /// <summary>
    /// Standard selling price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Cost price.
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Current stock quantity.
    /// </summary>
    public decimal StockQuantity { get; set; }

    /// <summary>
    /// Minimum stock level.
    /// </summary>
    public decimal MinStockLevel { get; set; }

    /// <summary>
    /// Maximum stock level.
    /// </summary>
    public decimal MaxStockLevel { get; set; }

    /// <summary>
    /// Whether the product is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Barcode.
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// Weight in kg.
    /// </summary>
    public decimal? Weight { get; set; }

    /// <summary>
    /// Navigation property for category.
    /// </summary>
    public virtual ProductCategory? Category { get; set; }
}

/// <summary>
/// Product category entity.
/// </summary>
public class ProductCategory : BaseEntity
{
    /// <summary>
    /// Category code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Category name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parent category ID.
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// Sort order.
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
