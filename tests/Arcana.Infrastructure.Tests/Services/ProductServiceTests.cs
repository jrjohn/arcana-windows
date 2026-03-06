using Arcana.Core.Common;
using Arcana.Data.Repository;
using Arcana.Domain.Entities;
using Arcana.Infrastructure.Services.Impl;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Infrastructure.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<ProductRepository> _productRepoMock;
    private readonly Mock<IValidator<Product>> _validatorMock;
    private readonly Mock<ILogger<ProductServiceImpl>> _loggerMock;
    private readonly ProductServiceImpl _service;

    public ProductServiceTests()
    {
        _productRepoMock = new Mock<ProductRepository>();
        _validatorMock = new Mock<IValidator<Product>>();
        _loggerMock = new Mock<ILogger<ProductServiceImpl>>();
        _service = new ProductServiceImpl(
            _productRepoMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);

        // Default: validation passes
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private static Product CreateProduct(int id = 1, string code = "P001", string name = "Widget") =>
        new Product { Id = id, Code = code, Name = name, Unit = "PCS", Price = 100m, Cost = 60m };

    #region GetProductsAsync

    [Fact]
    public async Task GetProductsAsync_ShouldReturnPagedResult()
    {
        var products = new List<Product> { CreateProduct() };
        var paged = new PagedResult<Product>(products, 1, 20, 1);
        _productRepoMock.Setup(r => r.GetPagedAsync(It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await _service.GetProductsAsync(new PageRequest());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetProductsAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _productRepoMock.Setup(r => r.GetPagedAsync(It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.GetProductsAsync(new PageRequest());

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region GetProductByIdAsync

    [Fact]
    public async Task GetProductByIdAsync_ExistingProduct_ShouldReturnSuccess()
    {
        var product = CreateProduct();
        _productRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _service.GetProductByIdAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(product);
    }

    [Fact]
    public async Task GetProductByIdAsync_NotFound_ShouldReturnFailure()
    {
        _productRepoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var result = await _service.GetProductByIdAsync(99);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetProductByIdAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.GetProductByIdAsync(1);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region GetProductByCodeAsync

    [Fact]
    public async Task GetProductByCodeAsync_ExistingCode_ShouldReturnSuccess()
    {
        var product = CreateProduct();
        _productRepoMock.Setup(r => r.GetByCodeAsync("P001", It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _service.GetProductByCodeAsync("P001");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(product);
    }

    [Fact]
    public async Task GetProductByCodeAsync_NotFound_ShouldReturnFailure()
    {
        _productRepoMock.Setup(r => r.GetByCodeAsync("NOTEXIST", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _service.GetProductByCodeAsync("NOTEXIST");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetProductByCodeAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _productRepoMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.GetProductByCodeAsync("P001");

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region SearchProductsAsync

    [Fact]
    public async Task SearchProductsAsync_ShouldReturnMatchingProducts()
    {
        var products = new List<Product> { CreateProduct(), CreateProduct(2, "P002", "Gadget") };
        _productRepoMock.Setup(r => r.SearchAsync("P", 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Product>)products);

        var result = await _service.SearchProductsAsync("P");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchProductsAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _productRepoMock.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.SearchProductsAsync("P");

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region GetProductsByCategoryAsync

    [Fact]
    public async Task GetProductsByCategoryAsync_ShouldReturnProductsInCategory()
    {
        var products = new List<Product> { CreateProduct() };
        _productRepoMock.Setup(r => r.GetByCategoryAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Product>)products);

        var result = await _service.GetProductsByCategoryAsync(5);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetProductsByCategoryAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _productRepoMock.Setup(r => r.GetByCategoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.GetProductsByCategoryAsync(5);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region CreateProductAsync

    [Fact]
    public async Task CreateProductAsync_Valid_ShouldReturnSuccess()
    {
        var product = CreateProduct();
        _productRepoMock.Setup(r => r.CodeExistsAsync("P001", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _productRepoMock.Setup(r => r.AddAsync(product, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _service.CreateProductAsync(product);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(product);
    }

    [Fact]
    public async Task CreateProductAsync_DuplicateCode_ShouldReturnFailure()
    {
        var product = CreateProduct();
        _productRepoMock.Setup(r => r.CodeExistsAsync("P001", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.CreateProductAsync(product);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateProductAsync_ValidationFails_ShouldReturnFailure()
    {
        var product = CreateProduct();
        _productRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var failures = new List<ValidationFailure> { new ValidationFailure("Name", "Name is required") };
        _validatorMock.Setup(v => v.ValidateAsync(product, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var result = await _service.CreateProductAsync(product);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateProductAsync_RepositoryThrows_ShouldReturnFailure()
    {
        var product = CreateProduct();
        _productRepoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _productRepoMock.Setup(r => r.AddAsync(product, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateProductAsync(product);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region UpdateProductAsync

    [Fact]
    public async Task UpdateProductAsync_Valid_ShouldReturnSuccess()
    {
        var product = CreateProduct();
        _productRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.CodeExistsAsync("P001", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _productRepoMock.Setup(r => r.UpdateAsync(product, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateProductAsync(product);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(product);
    }

    [Fact]
    public async Task UpdateProductAsync_NotFound_ShouldReturnFailure()
    {
        var product = CreateProduct();
        _productRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var result = await _service.UpdateProductAsync(product);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProductAsync_DuplicateCode_ShouldReturnFailure()
    {
        var product = CreateProduct();
        _productRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.CodeExistsAsync("P001", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.UpdateProductAsync(product);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProductAsync_ValidationFails_ShouldReturnFailure()
    {
        var product = CreateProduct();
        _productRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.CodeExistsAsync("P001", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var failures = new List<ValidationFailure> { new ValidationFailure("Price", "Price cannot be negative") };
        _validatorMock.Setup(v => v.ValidateAsync(product, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var result = await _service.UpdateProductAsync(product);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProductAsync_RepositoryThrows_ShouldReturnFailure()
    {
        var product = CreateProduct();
        _productRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.CodeExistsAsync("P001", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _productRepoMock.Setup(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.UpdateProductAsync(product);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region DeleteProductAsync

    [Fact]
    public async Task DeleteProductAsync_ExistingProduct_ShouldReturnSuccess()
    {
        var product = CreateProduct();
        _productRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.DeleteAsync(product, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.DeleteProductAsync(1);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProductAsync_NotFound_ShouldReturnFailure()
    {
        _productRepoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var result = await _service.DeleteProductAsync(99);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProductAsync_RepositoryThrows_ShouldReturnFailure()
    {
        var product = CreateProduct();
        _productRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);
        _productRepoMock.Setup(r => r.DeleteAsync(product, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteProductAsync(1);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region GetCategoriesAsync

    [Fact]
    public async Task GetCategoriesAsync_ShouldReturnCategories()
    {
        var categories = new List<ProductCategory>
        {
            new ProductCategory { Id = 1, Code = "CAT001", Name = "Electronics" },
            new ProductCategory { Id = 2, Code = "CAT002", Name = "Furniture" }
        };
        _productRepoMock.Setup(r => r.GetCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ProductCategory>)categories);

        var result = await _service.GetCategoriesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCategoriesAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _productRepoMock.Setup(r => r.GetCategoriesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.GetCategoriesAsync();

        result.IsSuccess.Should().BeFalse();
    }

    #endregion
}
