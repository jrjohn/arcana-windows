using Arcana.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Arcana.Domain.Tests.Entities;

public class ProductTests
{
    private static Product CreateProduct() => new Product
    {
        Id = 1,
        Code = "P001",
        Name = "Widget",
        Description = "A widget",
        Unit = "PCS",
        Price = 100m,
        Cost = 60m,
        StockQuantity = 50m,
        MinStockLevel = 5m,
        MaxStockLevel = 200m,
        IsActive = true
    };

    // ─── Basic property tests ─────────────────────────────────────────────────

    [Fact]
    public void Product_Id_ShouldBeSet()
    {
        var p = CreateProduct();
        p.Id.Should().Be(1);
    }

    [Fact]
    public void Product_Code_ShouldBeSet()
    {
        var p = CreateProduct();
        p.Code.Should().Be("P001");
    }

    [Fact]
    public void Product_Name_ShouldBeSet()
    {
        var p = CreateProduct();
        p.Name.Should().Be("Widget");
    }

    [Fact]
    public void Product_Description_ShouldBeSet()
    {
        var p = CreateProduct();
        p.Description.Should().Be("A widget");
    }

    [Fact]
    public void Product_Unit_DefaultValue_ShouldBePCS()
    {
        var p = new Product();
        p.Unit.Should().Be("PCS");
    }

    [Fact]
    public void Product_IsActive_DefaultValue_ShouldBeTrue()
    {
        var p = new Product();
        p.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Product_Price_ShouldBeSet()
    {
        var p = CreateProduct();
        p.Price.Should().Be(100m);
    }

    [Fact]
    public void Product_Cost_ShouldBeSet()
    {
        var p = CreateProduct();
        p.Cost.Should().Be(60m);
    }

    [Fact]
    public void Product_StockQuantity_ShouldBeSet()
    {
        var p = CreateProduct();
        p.StockQuantity.Should().Be(50m);
    }

    [Fact]
    public void Product_MinStockLevel_ShouldBeSet()
    {
        var p = CreateProduct();
        p.MinStockLevel.Should().Be(5m);
    }

    [Fact]
    public void Product_MaxStockLevel_ShouldBeSet()
    {
        var p = CreateProduct();
        p.MaxStockLevel.Should().Be(200m);
    }

    [Fact]
    public void Product_Barcode_DefaultValue_ShouldBeNull()
    {
        var p = new Product();
        p.Barcode.Should().BeNull();
    }

    [Fact]
    public void Product_Barcode_ShouldBeSettable()
    {
        var p = CreateProduct();
        p.Barcode = "1234567890";
        p.Barcode.Should().Be("1234567890");
    }

    [Fact]
    public void Product_Weight_DefaultValue_ShouldBeNull()
    {
        var p = new Product();
        p.Weight.Should().BeNull();
    }

    [Fact]
    public void Product_Weight_ShouldBeSettable()
    {
        var p = CreateProduct();
        p.Weight = 1.5m;
        p.Weight.Should().Be(1.5m);
    }

    [Fact]
    public void Product_CategoryId_DefaultValue_ShouldBeNull()
    {
        var p = new Product();
        p.CategoryId.Should().BeNull();
    }

    [Fact]
    public void Product_CategoryId_ShouldBeSettable()
    {
        var p = CreateProduct();
        p.CategoryId = 5;
        p.CategoryId.Should().Be(5);
    }

    [Fact]
    public void Product_Code_DefaultValue_ShouldBeEmptyString()
    {
        var p = new Product();
        p.Code.Should().Be(string.Empty);
    }

    [Fact]
    public void Product_Name_DefaultValue_ShouldBeEmptyString()
    {
        var p = new Product();
        p.Name.Should().Be(string.Empty);
    }
}

public class ProductCategoryTests
{
    [Fact]
    public void ProductCategory_Code_ShouldBeSet()
    {
        var cat = new ProductCategory { Code = "CAT001", Name = "Electronics" };
        cat.Code.Should().Be("CAT001");
    }

    [Fact]
    public void ProductCategory_Name_ShouldBeSet()
    {
        var cat = new ProductCategory { Code = "CAT001", Name = "Electronics" };
        cat.Name.Should().Be("Electronics");
    }

    [Fact]
    public void ProductCategory_ParentId_DefaultValue_ShouldBeNull()
    {
        var cat = new ProductCategory { Code = "C", Name = "Cat" };
        cat.ParentId.Should().BeNull();
    }

    [Fact]
    public void ProductCategory_SortOrder_DefaultValue_ShouldBeZero()
    {
        var cat = new ProductCategory { Code = "C", Name = "Cat" };
        cat.SortOrder.Should().Be(0);
    }

    [Fact]
    public void ProductCategory_Children_DefaultValue_ShouldBeEmpty()
    {
        var cat = new ProductCategory { Code = "C", Name = "Cat" };
        cat.Children.Should().BeEmpty();
    }

    [Fact]
    public void ProductCategory_Products_DefaultValue_ShouldBeEmpty()
    {
        var cat = new ProductCategory { Code = "C", Name = "Cat" };
        cat.Products.Should().BeEmpty();
    }

    [Fact]
    public void ProductCategory_Parent_ShouldBeSettable()
    {
        var parent = new ProductCategory { Code = "P", Name = "Parent" };
        var child = new ProductCategory { Code = "C", Name = "Child", ParentId = parent.Id };
        child.Parent = parent;

        child.Parent.Should().Be(parent);
    }
}
