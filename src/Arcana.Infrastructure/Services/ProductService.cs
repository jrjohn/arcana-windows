using Arcana.Core.Common;
using Arcana.Data.Repository;
using Arcana.Domain.Entities;
using Arcana.Domain.Services;
using Arcana.Domain.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Arcana.Infrastructure.Services;

/// <summary>
/// Product service implementation.
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IValidator<Product> _validator;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository,
        IValidator<Product> validator,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<Product>>> GetProductsAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _productRepository.GetPagedAsync(request, cancellationToken);
            return Result<PagedResult<Product>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            return Result<PagedResult<Product>>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get products", ex));
        }
    }

    public async Task<Result<Product>> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product == null)
            {
                return Result<Product>.Failure(new AppError.Data(ErrorCode.NotFound, $"Product with ID {id} not found"));
            }
            return Result<Product>.Success(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", id);
            return Result<Product>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get product", ex));
        }
    }

    public async Task<Result<Product>> GetProductByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _productRepository.GetByCodeAsync(code, cancellationToken);
            if (product == null)
            {
                return Result<Product>.Failure(new AppError.Data(ErrorCode.NotFound, $"Product {code} not found"));
            }
            return Result<Product>.Success(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductCode}", code);
            return Result<Product>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get product", ex));
        }
    }

    public async Task<Result<IReadOnlyList<Product>>> SearchProductsAsync(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var products = await _productRepository.SearchAsync(searchTerm, maxResults, cancellationToken);
            return Result<IReadOnlyList<Product>>.Success(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return Result<IReadOnlyList<Product>>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to search products", ex));
        }
    }

    public async Task<Result<IReadOnlyList<Product>>> GetProductsByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var products = await _productRepository.GetByCategoryAsync(categoryId, cancellationToken);
            return Result<IReadOnlyList<Product>>.Success(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products by category {CategoryId}", categoryId);
            return Result<IReadOnlyList<Product>>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get products", ex));
        }
    }

    public async Task<Result<Product>> CreateProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check for duplicate code
            if (await _productRepository.CodeExistsAsync(product.Code, null, cancellationToken))
            {
                return Result<Product>.Failure(new AppError.Validation(ErrorCode.DuplicateEntry, "Product code already exists", new[] { $"Product code {product.Code} already exists" }));
            }

            // Validate
            var validationResult = await _validator.ValidateAsync(product, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<Product>.Failure(new AppError.Validation(ErrorCode.ValidationFailed, "Product validation failed", errors));
            }

            var created = await _productRepository.AddAsync(product, cancellationToken);
            _logger.LogInformation("Created product {ProductCode}", product.Code);

            return Result<Product>.Success(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return Result<Product>.Failure(new AppError.Data(ErrorCode.DatabaseError, "Failed to create product", ex));
        }
    }

    public async Task<Result<Product>> UpdateProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _productRepository.GetByIdAsync(product.Id, cancellationToken);
            if (existing == null)
            {
                return Result<Product>.Failure(new AppError.Data(ErrorCode.NotFound, $"Product with ID {product.Id} not found"));
            }

            // Check for duplicate code
            if (await _productRepository.CodeExistsAsync(product.Code, product.Id, cancellationToken))
            {
                return Result<Product>.Failure(new AppError.Validation(ErrorCode.DuplicateEntry, "Product code already exists", new[] { $"Product code {product.Code} already exists" }));
            }

            // Validate
            var validationResult = await _validator.ValidateAsync(product, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<Product>.Failure(new AppError.Validation(ErrorCode.ValidationFailed, "Product validation failed", errors));
            }

            await _productRepository.UpdateAsync(product, cancellationToken);
            _logger.LogInformation("Updated product {ProductCode}", product.Code);

            return Result<Product>.Success(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", product.Id);
            return Result<Product>.Failure(new AppError.Data(ErrorCode.DatabaseError, "Failed to update product", ex));
        }
    }

    public async Task<Result> DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product == null)
            {
                return Result.Failure(new AppError.Data(ErrorCode.NotFound, $"Product with ID {id} not found"));
            }

            await _productRepository.DeleteAsync(product, cancellationToken);
            _logger.LogInformation("Deleted product {ProductCode}", product.Code);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            return Result.Failure(new AppError.Data(ErrorCode.DatabaseError, "Failed to delete product", ex));
        }
    }

    public async Task<Result<IReadOnlyList<ProductCategory>>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await _productRepository.GetCategoriesAsync(cancellationToken);
            return Result<IReadOnlyList<ProductCategory>>.Success(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product categories");
            return Result<IReadOnlyList<ProductCategory>>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get categories", ex));
        }
    }
}
