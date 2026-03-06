using Arcana.Domain.Entities;
using Arcana.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace Arcana.Domain.Tests.Validation;

public class ProductValidatorTests
{
    private readonly ProductValidator _validator = new();

    private static Product CreateValidProduct() => new Product
    {
        Code = "P001",
        Name = "Test Widget",
        Unit = "PCS",
        Price = 100m,
        Cost = 60m,
        StockQuantity = 50m,
        MinStockLevel = 5m,
        MaxStockLevel = 200m,
        IsActive = true
    };

    // ─── Valid product ────────────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidProduct_ShouldPass()
    {
        var product = CreateValidProduct();
        var result = _validator.Validate(product);
        result.IsValid.Should().BeTrue();
    }

    // ─── Code validation ─────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyCode_ShouldFail()
    {
        var product = CreateValidProduct();
        product.Code = string.Empty;

        var result = _validator.Validate(product);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_CodeTooLong_ShouldFail()
    {
        var product = CreateValidProduct();
        product.Code = new string('X', 51);

        var result = _validator.Validate(product);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_CodeExactlyMaxLength_ShouldPass()
    {
        var product = CreateValidProduct();
        product.Code = new string('X', 50);

        var result = _validator.Validate(product);

        result.IsValid.Should().BeTrue();
    }

    // ─── Name validation ─────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        var product = CreateValidProduct();
        product.Name = string.Empty;

        var result = _validator.Validate(product);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_NameTooLong_ShouldFail()
    {
        var product = CreateValidProduct();
        product.Name = new string('A', 201);

        var result = _validator.Validate(product);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_NameExactlyMaxLength_ShouldPass()
    {
        var product = CreateValidProduct();
        product.Name = new string('A', 200);

        var result = _validator.Validate(product);

        result.IsValid.Should().BeTrue();
    }

    // ─── Unit validation ─────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyUnit_ShouldFail()
    {
        var product = CreateValidProduct();
        product.Unit = string.Empty;

        var result = _validator.Validate(product);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Unit");
    }

    [Fact]
    public void Validate_UnitTooLong_ShouldFail()
    {
        var product = CreateValidProduct();
        product.Unit = "TOOLONGUNIT";

        var result = _validator.Validate(product);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Unit");
    }

    [Fact]
    public void Validate_UnitExactlyMaxLength_ShouldPass()
    {
        var product = CreateValidProduct();
        product.Unit = "1234567890"; // 10 chars

        var result = _validator.Validate(product);

        result.IsValid.Should().BeTrue();
    }

    // ─── Price validation ─────────────────────────────────────────────────────

    [Fact]
    public void Validate_NegativePrice_ShouldFail()
    {
        var product = CreateValidProduct();
        product.Price = -1m;

        var result = _validator.Validate(product);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }

    [Fact]
    public void Validate_ZeroPrice_ShouldPass()
    {
        var product = CreateValidProduct();
        product.Price = 0m;

        var result = _validator.Validate(product);

        result.IsValid.Should().BeTrue();
    }

    // ─── Cost validation ─────────────────────────────────────────────────────

    [Fact]
    public void Validate_NegativeCost_ShouldFail()
    {
        var product = CreateValidProduct();
        product.Cost = -5m;

        var result = _validator.Validate(product);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Cost");
    }

    [Fact]
    public void Validate_ZeroCost_ShouldPass()
    {
        var product = CreateValidProduct();
        product.Cost = 0m;

        var result = _validator.Validate(product);

        result.IsValid.Should().BeTrue();
    }

    // ─── Stock quantity validation ────────────────────────────────────────────

    [Fact]
    public void Validate_NegativeStockQuantity_ShouldFail()
    {
        var product = CreateValidProduct();
        product.StockQuantity = -10m;

        var result = _validator.Validate(product);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StockQuantity");
    }

    [Fact]
    public void Validate_ZeroStockQuantity_ShouldPass()
    {
        var product = CreateValidProduct();
        product.StockQuantity = 0m;

        var result = _validator.Validate(product);

        result.IsValid.Should().BeTrue();
    }

    // ─── MinStockLevel validation ─────────────────────────────────────────────

    [Fact]
    public void Validate_NegativeMinStockLevel_ShouldFail()
    {
        var product = CreateValidProduct();
        product.MinStockLevel = -1m;

        var result = _validator.Validate(product);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MinStockLevel");
    }

    [Fact]
    public void Validate_ZeroMinStockLevel_ShouldPass()
    {
        var product = CreateValidProduct();
        product.MinStockLevel = 0m;

        var result = _validator.Validate(product);

        result.IsValid.Should().BeTrue();
    }

    // ─── MaxStockLevel validation ─────────────────────────────────────────────

    [Fact]
    public void Validate_MaxStockLevelLessThanMin_ShouldFail()
    {
        var product = CreateValidProduct();
        product.MinStockLevel = 50m;
        product.MaxStockLevel = 10m; // less than min

        var result = _validator.Validate(product);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxStockLevel");
    }

    [Fact]
    public void Validate_MaxStockLevelEqualToMin_ShouldPass()
    {
        var product = CreateValidProduct();
        product.MinStockLevel = 10m;
        product.MaxStockLevel = 10m;

        var result = _validator.Validate(product);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ZeroMaxStockLevel_ShouldPass()
    {
        // MaxStockLevel rule only applies When MaxStockLevel > 0
        var product = CreateValidProduct();
        product.MinStockLevel = 100m;
        product.MaxStockLevel = 0m;

        var result = _validator.Validate(product);

        result.IsValid.Should().BeTrue();
    }

    // ─── Barcode validation ───────────────────────────────────────────────────

    [Fact]
    public void Validate_NullBarcode_ShouldPass()
    {
        var product = CreateValidProduct();
        product.Barcode = null;

        var result = _validator.Validate(product);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidBarcode_ShouldPass()
    {
        var product = CreateValidProduct();
        product.Barcode = "1234567890123";

        var result = _validator.Validate(product);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_BarcodeTooLong_ShouldFail()
    {
        var product = CreateValidProduct();
        product.Barcode = new string('1', 51);

        var result = _validator.Validate(product);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Barcode");
    }

    [Fact]
    public void Validate_BarcodeExactlyMaxLength_ShouldPass()
    {
        var product = CreateValidProduct();
        product.Barcode = new string('1', 50);

        var result = _validator.Validate(product);

        result.IsValid.Should().BeTrue();
    }

    // ─── Multiple errors ──────────────────────────────────────────────────────

    [Fact]
    public void Validate_MultipleInvalid_ShouldReturnMultipleErrors()
    {
        var product = new Product
        {
            Code = string.Empty,
            Name = string.Empty,
            Unit = string.Empty,
            Price = -1m,
            Cost = -5m,
            StockQuantity = 0m,
            MinStockLevel = 0m,
            MaxStockLevel = 0m
        };

        var result = _validator.Validate(product);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(3);
    }
}
