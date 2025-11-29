using Arcana.Core.Common;
using Arcana.Data.Repository;
using Arcana.Domain.Entities;
using Arcana.Domain.Services;
using Arcana.Domain.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Arcana.Infrastructure.Services;

/// <summary>
/// Customer service implementation.
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IValidator<Customer> _validator;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ICustomerRepository customerRepository,
        IValidator<Customer> validator,
        ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<Customer>>> GetCustomersAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _customerRepository.GetPagedAsync(request, cancellationToken);
            return Result<PagedResult<Customer>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers");
            return Result<PagedResult<Customer>>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get customers", ex));
        }
    }

    public async Task<Result<Customer>> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
            if (customer == null)
            {
                return Result<Customer>.Failure(new AppError.Data(ErrorCode.NotFound, $"Customer with ID {id} not found"));
            }
            return Result<Customer>.Success(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {CustomerId}", id);
            return Result<Customer>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get customer", ex));
        }
    }

    public async Task<Result<Customer>> GetCustomerByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _customerRepository.GetByCodeAsync(code, cancellationToken);
            if (customer == null)
            {
                return Result<Customer>.Failure(new AppError.Data(ErrorCode.NotFound, $"Customer {code} not found"));
            }
            return Result<Customer>.Success(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {CustomerCode}", code);
            return Result<Customer>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to get customer", ex));
        }
    }

    public async Task<Result<IReadOnlyList<Customer>>> SearchCustomersAsync(string searchTerm, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var customers = await _customerRepository.SearchAsync(searchTerm, maxResults, cancellationToken);
            return Result<IReadOnlyList<Customer>>.Success(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers");
            return Result<IReadOnlyList<Customer>>.Failure(new AppError.Data(ErrorCode.QueryFailed, "Failed to search customers", ex));
        }
    }

    public async Task<Result<Customer>> CreateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check for duplicate code
            if (await CustomerCodeExistsAsync(customer.Code, null, cancellationToken))
            {
                return Result<Customer>.Failure(new AppError.Validation(ErrorCode.DuplicateEntry, "Customer code already exists", new[] { $"Customer code {customer.Code} already exists" }));
            }

            // Validate
            var validationResult = await _validator.ValidateAsync(customer, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<Customer>.Failure(new AppError.Validation(ErrorCode.ValidationFailed, "Customer validation failed", errors));
            }

            var created = await _customerRepository.AddAsync(customer, cancellationToken);
            _logger.LogInformation("Created customer {CustomerCode}", customer.Code);

            return Result<Customer>.Success(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return Result<Customer>.Failure(new AppError.Data(ErrorCode.DatabaseError, "Failed to create customer", ex));
        }
    }

    public async Task<Result<Customer>> UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _customerRepository.GetByIdAsync(customer.Id, cancellationToken);
            if (existing == null)
            {
                return Result<Customer>.Failure(new AppError.Data(ErrorCode.NotFound, $"Customer with ID {customer.Id} not found"));
            }

            // Check for duplicate code
            if (await CustomerCodeExistsAsync(customer.Code, customer.Id, cancellationToken))
            {
                return Result<Customer>.Failure(new AppError.Validation(ErrorCode.DuplicateEntry, "Customer code already exists", new[] { $"Customer code {customer.Code} already exists" }));
            }

            // Validate
            var validationResult = await _validator.ValidateAsync(customer, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<Customer>.Failure(new AppError.Validation(ErrorCode.ValidationFailed, "Customer validation failed", errors));
            }

            await _customerRepository.UpdateAsync(customer, cancellationToken);
            _logger.LogInformation("Updated customer {CustomerCode}", customer.Code);

            return Result<Customer>.Success(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", customer.Id);
            return Result<Customer>.Failure(new AppError.Data(ErrorCode.DatabaseError, "Failed to update customer", ex));
        }
    }

    public async Task<Result> DeleteCustomerAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
            if (customer == null)
            {
                return Result.Failure(new AppError.Data(ErrorCode.NotFound, $"Customer with ID {id} not found"));
            }

            await _customerRepository.DeleteAsync(customer, cancellationToken);
            _logger.LogInformation("Deleted customer {CustomerCode}", customer.Code);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
            return Result.Failure(new AppError.Data(ErrorCode.DatabaseError, "Failed to delete customer", ex));
        }
    }

    public async Task<bool> CustomerCodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _customerRepository.CodeExistsAsync(code, excludeId, cancellationToken);
    }
}
