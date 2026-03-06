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

public class CustomerServiceTests
{
    private readonly Mock<CustomerRepository> _repoMock;
    private readonly Mock<IValidator<Customer>> _validatorMock;
    private readonly Mock<ILogger<CustomerServiceImpl>> _loggerMock;
    private readonly CustomerServiceImpl _service;

    public CustomerServiceTests()
    {
        _repoMock = new Mock<CustomerRepository>();
        _validatorMock = new Mock<IValidator<Customer>>();
        _loggerMock = new Mock<ILogger<CustomerServiceImpl>>();
        _service = new CustomerServiceImpl(_repoMock.Object, _validatorMock.Object, _loggerMock.Object);

        // Default: validation passes
        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private static Customer CreateCustomer(int id = 1, string code = "CUST001", string name = "Test Customer") =>
        new Customer { Id = id, Code = code, Name = name };

    #region GetCustomerByIdAsync

    [Fact]
    public async Task GetCustomerByIdAsync_ExistingCustomer_ShouldReturnSuccess()
    {
        var customer = CreateCustomer();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var result = await _service.GetCustomerByIdAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(customer);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_NotFound_ShouldReturnFailure()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await _service.GetCustomerByIdAsync(99);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetCustomerByIdAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.GetCustomerByIdAsync(1);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region GetCustomerByCodeAsync

    [Fact]
    public async Task GetCustomerByCodeAsync_ExistingCode_ShouldReturnSuccess()
    {
        var customer = CreateCustomer();
        _repoMock.Setup(r => r.GetByCodeAsync("CUST001", It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var result = await _service.GetCustomerByCodeAsync("CUST001");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(customer);
    }

    [Fact]
    public async Task GetCustomerByCodeAsync_NotFound_ShouldReturnFailure()
    {
        _repoMock.Setup(r => r.GetByCodeAsync("NOTEXIST", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await _service.GetCustomerByCodeAsync("NOTEXIST");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetCustomerByCodeAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _repoMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.GetCustomerByCodeAsync("CUST001");

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region GetCustomersAsync

    [Fact]
    public async Task GetCustomersAsync_ShouldReturnPagedResult()
    {
        var customers = new List<Customer> { CreateCustomer() };
        var pagedResult = new PagedResult<Customer>(customers, 1, 20, 1);
        _repoMock.Setup(r => r.GetPagedAsync(It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _service.GetCustomersAsync(new PageRequest());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCustomersAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _repoMock.Setup(r => r.GetPagedAsync(It.IsAny<PageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.GetCustomersAsync(new PageRequest());

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region SearchCustomersAsync

    [Fact]
    public async Task SearchCustomersAsync_ShouldReturnMatchingCustomers()
    {
        var customers = new List<Customer> { CreateCustomer(), CreateCustomer(2, "CUST002", "Test 2") };
        _repoMock.Setup(r => r.SearchAsync("test", 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Customer>)customers);

        var result = await _service.SearchCustomersAsync("test");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchCustomersAsync_RepositoryThrows_ShouldReturnFailure()
    {
        _repoMock.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));

        var result = await _service.SearchCustomersAsync("test");

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region CreateCustomerAsync

    [Fact]
    public async Task CreateCustomerAsync_Valid_ShouldReturnSuccess()
    {
        var customer = CreateCustomer();
        _repoMock.Setup(r => r.CodeExistsAsync("CUST001", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repoMock.Setup(r => r.AddAsync(customer, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var result = await _service.CreateCustomerAsync(customer);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(customer);
    }

    [Fact]
    public async Task CreateCustomerAsync_DuplicateCode_ShouldReturnFailure()
    {
        var customer = CreateCustomer();
        _repoMock.Setup(r => r.CodeExistsAsync("CUST001", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.CreateCustomerAsync(customer);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCustomerAsync_ValidationFails_ShouldReturnFailure()
    {
        var customer = CreateCustomer();
        _repoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var failures = new List<ValidationFailure> { new ValidationFailure("Name", "Name is required") };
        _validatorMock.Setup(v => v.ValidateAsync(customer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var result = await _service.CreateCustomerAsync(customer);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCustomerAsync_RepositoryThrows_ShouldReturnFailure()
    {
        var customer = CreateCustomer();
        _repoMock.Setup(r => r.CodeExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repoMock.Setup(r => r.AddAsync(customer, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.CreateCustomerAsync(customer);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region UpdateCustomerAsync

    [Fact]
    public async Task UpdateCustomerAsync_Valid_ShouldReturnSuccess()
    {
        var customer = CreateCustomer();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _repoMock.Setup(r => r.CodeExistsAsync("CUST001", 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateAsync(customer, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateCustomerAsync(customer);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCustomerAsync_NotFound_ShouldReturnFailure()
    {
        var customer = CreateCustomer();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        var result = await _service.UpdateCustomerAsync(customer);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCustomerAsync_DuplicateCode_ShouldReturnFailure()
    {
        var customer = CreateCustomer();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _repoMock.Setup(r => r.CodeExistsAsync("CUST001", 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.UpdateCustomerAsync(customer);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region DeleteCustomerAsync

    [Fact]
    public async Task DeleteCustomerAsync_ExistingCustomer_ShouldReturnSuccess()
    {
        var customer = CreateCustomer();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _repoMock.Setup(r => r.DeleteAsync(customer, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.DeleteCustomerAsync(1);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCustomerAsync_NotFound_ShouldReturnFailure()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);

        var result = await _service.DeleteCustomerAsync(99);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCustomerAsync_RepositoryThrows_ShouldReturnFailure()
    {
        var customer = CreateCustomer();
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _repoMock.Setup(r => r.DeleteAsync(customer, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("db error"));

        var result = await _service.DeleteCustomerAsync(1);

        result.IsSuccess.Should().BeFalse();
    }

    #endregion

    #region CustomerCodeExistsAsync

    [Fact]
    public async Task CustomerCodeExistsAsync_ExistingCode_ShouldReturnTrue()
    {
        _repoMock.Setup(r => r.CodeExistsAsync("CUST001", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await _service.CustomerCodeExistsAsync("CUST001");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CustomerCodeExistsAsync_WithExclude_ShouldPassThroughToRepo()
    {
        _repoMock.Setup(r => r.CodeExistsAsync("CUST001", 5, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var result = await _service.CustomerCodeExistsAsync("CUST001", 5);
        result.Should().BeFalse();
    }

    #endregion
}
