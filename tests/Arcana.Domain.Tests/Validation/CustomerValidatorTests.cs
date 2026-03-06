using Arcana.Domain.Entities;
using Arcana.Domain.Validation;
using FluentAssertions;
using Xunit;

namespace Arcana.Domain.Tests.Validation;

public class CustomerValidatorTests
{
    private readonly CustomerValidator _validator = new();

    private static Customer CreateValidCustomer() => new Customer
    {
        Code = "CUST001",
        Name = "Test Customer",
        Email = "test@example.com",
        Phone = "0912345678",
        CreditLimit = 10000m
    };

    // ─── Valid customer ──────────────────────────────────────────────────────

    [Fact]
    public void Validate_ValidCustomer_ShouldPass()
    {
        var customer = CreateValidCustomer();
        var result = _validator.Validate(customer);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MinimalValidCustomer_ShouldPass()
    {
        var customer = new Customer
        {
            Code = "C001",
            Name = "Minimal"
        };
        var result = _validator.Validate(customer);
        result.IsValid.Should().BeTrue();
    }

    // ─── Code ────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyCode_ShouldFail()
    {
        var customer = CreateValidCustomer();
        customer.Code = string.Empty;

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_CodeExceeds20Chars_ShouldFail()
    {
        var customer = CreateValidCustomer();
        customer.Code = new string('A', 21);

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code" && e.ErrorMessage.Contains("20"));
    }

    [Fact]
    public void Validate_CodeExactly20Chars_ShouldPass()
    {
        var customer = CreateValidCustomer();
        customer.Code = new string('A', 20);

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeTrue();
    }

    // ─── Name ────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        var customer = CreateValidCustomer();
        customer.Name = string.Empty;

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_NameExceeds100Chars_ShouldFail()
    {
        var customer = CreateValidCustomer();
        customer.Name = new string('N', 101);

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("100"));
    }

    [Fact]
    public void Validate_NameExactly100Chars_ShouldPass()
    {
        var customer = CreateValidCustomer();
        customer.Name = new string('N', 100);

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeTrue();
    }

    // ─── Email ───────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_InvalidEmail_ShouldFail()
    {
        var customer = CreateValidCustomer();
        customer.Email = "not-an-email";

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_ValidEmail_ShouldPass()
    {
        var customer = CreateValidCustomer();
        customer.Email = "user@domain.com";

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullEmail_ShouldPass()
    {
        var customer = CreateValidCustomer();
        customer.Email = null;

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyEmail_ShouldPass()
    {
        // When empty string, the When() condition prevents validation
        var customer = CreateValidCustomer();
        customer.Email = string.Empty;

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeTrue();
    }

    // ─── Phone ───────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_PhoneExceeds20Chars_ShouldFail()
    {
        var customer = CreateValidCustomer();
        customer.Phone = new string('1', 21);

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Phone" && e.ErrorMessage.Contains("20"));
    }

    [Fact]
    public void Validate_PhoneExactly20Chars_ShouldPass()
    {
        var customer = CreateValidCustomer();
        customer.Phone = new string('1', 20);

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullPhone_ShouldPass()
    {
        var customer = CreateValidCustomer();
        customer.Phone = null;

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyPhone_ShouldPass()
    {
        // When() condition skips validation for empty phone
        var customer = CreateValidCustomer();
        customer.Phone = string.Empty;

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeTrue();
    }

    // ─── CreditLimit ─────────────────────────────────────────────────────────

    [Fact]
    public void Validate_NegativeCreditLimit_ShouldFail()
    {
        var customer = CreateValidCustomer();
        customer.CreditLimit = -1m;

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CreditLimit");
    }

    [Fact]
    public void Validate_ZeroCreditLimit_ShouldPass()
    {
        var customer = CreateValidCustomer();
        customer.CreditLimit = 0m;

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PositiveCreditLimit_ShouldPass()
    {
        var customer = CreateValidCustomer();
        customer.CreditLimit = 999999m;

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeTrue();
    }

    // ─── TaxId ────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_TaxIdExceeds20Chars_ShouldFail()
    {
        var customer = CreateValidCustomer();
        customer.TaxId = new string('T', 21);

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TaxId");
    }

    [Fact]
    public void Validate_TaxIdExactly20Chars_ShouldPass()
    {
        var customer = CreateValidCustomer();
        customer.TaxId = new string('T', 20);

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullTaxId_ShouldPass()
    {
        var customer = CreateValidCustomer();
        customer.TaxId = null;

        var result = _validator.Validate(customer);

        result.IsValid.Should().BeTrue();
    }
}
