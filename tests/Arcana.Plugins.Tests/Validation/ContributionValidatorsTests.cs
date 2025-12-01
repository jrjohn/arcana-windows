using Arcana.Plugins.Contracts;
using Arcana.Plugins.Contracts.Validation;
using FluentAssertions;
using Xunit;

namespace Arcana.Plugins.Tests.Validation;

public class MenuItemValidatorTests
{
    #region Valid Menu Item Tests

    [Fact]
    public void Validate_ValidMenuItem_ShouldReturnSuccess()
    {
        // Arrange
        var item = new MenuItemDefinition
        {
            Id = "menu.file.new",
            Title = "New File",
            Location = MenuLocation.MainMenu
        };

        // Act
        var result = MenuItemValidator.Validate(item);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ValidSeparator_ShouldReturnSuccess()
    {
        // Arrange
        var item = new MenuItemDefinition
        {
            Id = "menu.file.separator1",
            Title = "",
            Location = MenuLocation.FileMenu,
            IsSeparator = true
        };

        // Act
        var result = MenuItemValidator.Validate(item);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("menu.file")]
    [InlineData("menu.file.new")]
    [InlineData("menu_file_new")]
    [InlineData("menu-file-new")]
    [InlineData("menuFileNew")]
    [InlineData("Menu123")]
    public void Validate_ValidIdFormats_ShouldReturnSuccess(string id)
    {
        // Arrange
        var item = new MenuItemDefinition
        {
            Id = id,
            Title = "Test",
            Location = MenuLocation.MainMenu
        };

        // Act
        var result = MenuItemValidator.Validate(item);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Invalid Id Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyOrNullId_ShouldReturnError(string? id)
    {
        // Arrange
        var item = new MenuItemDefinition
        {
            Id = id!,
            Title = "Test",
            Location = MenuLocation.MainMenu
        };

        // Act
        var result = MenuItemValidator.Validate(item);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Id is required"));
    }

    [Theory]
    [InlineData("123menu")]       // Starts with number
    [InlineData("_menu")]         // Starts with underscore
    [InlineData(".menu")]         // Starts with dot
    [InlineData("-menu")]         // Starts with hyphen
    public void Validate_InvalidIdFormat_ShouldReturnError(string id)
    {
        // Arrange
        var item = new MenuItemDefinition
        {
            Id = id,
            Title = "Test",
            Location = MenuLocation.MainMenu
        };

        // Act
        var result = MenuItemValidator.Validate(item);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("invalid format"));
    }

    #endregion

    #region Invalid Title Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyTitleNonSeparator_ShouldReturnError(string? title)
    {
        // Arrange
        var item = new MenuItemDefinition
        {
            Id = "menu.test",
            Title = title!,
            Location = MenuLocation.MainMenu,
            IsSeparator = false
        };

        // Act
        var result = MenuItemValidator.Validate(item);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("requires a Title"));
    }

    #endregion

    #region Warning Tests

    [Fact]
    public void Validate_NegativeOrder_ShouldReturnWarning()
    {
        // Arrange
        var item = new MenuItemDefinition
        {
            Id = "menu.test",
            Title = "Test",
            Location = MenuLocation.MainMenu,
            Order = -1
        };

        // Act
        var result = MenuItemValidator.Validate(item);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("negative order"));
    }

    [Fact]
    public void Validate_ModuleQuickAccessWithoutModuleId_ShouldReturnWarning()
    {
        // Arrange
        var item = new MenuItemDefinition
        {
            Id = "module.quick.action",
            Title = "Quick Action",
            Location = MenuLocation.ModuleQuickAccess,
            ModuleId = null
        };

        // Act
        var result = MenuItemValidator.Validate(item);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("ModuleQuickAccess") && w.Contains("ModuleId"));
    }

    [Theory]
    [InlineData("123command")]
    [InlineData("command with space")]
    public void Validate_NonStandardCommandFormat_ShouldReturnWarning(string command)
    {
        // Arrange
        var item = new MenuItemDefinition
        {
            Id = "menu.test",
            Title = "Test",
            Location = MenuLocation.MainMenu,
            Command = command
        };

        // Act
        var result = MenuItemValidator.Validate(item);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("non-standard format"));
    }

    #endregion
}

public class ViewValidatorTests
{
    #region Valid View Tests

    [Fact]
    public void Validate_ValidView_ShouldReturnSuccess()
    {
        // Arrange
        var view = new ViewDefinition
        {
            Id = "OrderListPage",
            Title = "Order List",
            TitleKey = "order.list",
            ViewClass = typeof(object)
        };

        // Act
        var result = ViewValidator.Validate(view);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("HomePage")]
    [InlineData("Order_List_Page")]
    [InlineData("Page123")]
    [InlineData("CustomerDetailPage")]
    public void Validate_ValidIdFormats_ShouldReturnSuccess(string id)
    {
        // Arrange
        var view = new ViewDefinition
        {
            Id = id,
            Title = "Test",
            TitleKey = "test.key",
            ViewClass = typeof(object)
        };

        // Act
        var result = ViewValidator.Validate(view);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Invalid Id Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyOrNullId_ShouldReturnError(string? id)
    {
        // Arrange
        var view = new ViewDefinition
        {
            Id = id!,
            Title = "Test"
        };

        // Act
        var result = ViewValidator.Validate(view);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Id is required"));
    }

    [Theory]
    [InlineData("123Page")]       // Starts with number
    [InlineData("_Page")]         // Starts with underscore
    [InlineData("Order-Page")]    // Contains hyphen
    [InlineData("Order.Page")]    // Contains dot
    public void Validate_InvalidIdFormat_ShouldReturnError(string id)
    {
        // Arrange
        var view = new ViewDefinition
        {
            Id = id,
            Title = "Test"
        };

        // Act
        var result = ViewValidator.Validate(view);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("invalid format"));
    }

    #endregion

    #region Invalid Title Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyOrNullTitle_ShouldReturnError(string? title)
    {
        // Arrange
        var view = new ViewDefinition
        {
            Id = "TestPage",
            Title = title!
        };

        // Act
        var result = ViewValidator.Validate(view);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("requires a Title"));
    }

    #endregion

    #region Warning Tests

    [Fact]
    public void Validate_NoViewClass_ShouldReturnWarning()
    {
        // Arrange
        var view = new ViewDefinition
        {
            Id = "TestPage",
            Title = "Test",
            TitleKey = "test.key",
            ViewClass = null
        };

        // Act
        var result = ViewValidator.Validate(view);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("ViewClass"));
    }

    [Fact]
    public void Validate_NoTitleKey_ShouldReturnWarning()
    {
        // Arrange
        var view = new ViewDefinition
        {
            Id = "TestPage",
            Title = "Test",
            TitleKey = null,
            ViewClass = typeof(object)
        };

        // Act
        var result = ViewValidator.Validate(view);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("TitleKey"));
    }

    [Fact]
    public void Validate_ModuleDefaultTabWithoutModuleId_ShouldReturnWarning()
    {
        // Arrange
        var view = new ViewDefinition
        {
            Id = "TestPage",
            Title = "Test",
            TitleKey = "test.key",
            ViewClass = typeof(object),
            IsModuleDefaultTab = true,
            ModuleId = null
        };

        // Act
        var result = ViewValidator.Validate(view);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("IsModuleDefaultTab") && w.Contains("ModuleId"));
    }

    #endregion
}

public class CommandValidatorTests
{
    #region Valid Command Id Tests

    [Theory]
    [InlineData("file.new")]
    [InlineData("order.list")]
    [InlineData("app.exit")]
    [InlineData("command_test")]
    [InlineData("command-test")]
    [InlineData("CommandTest")]
    [InlineData("command123")]
    public void ValidateCommandId_ValidFormats_ShouldReturnSuccess(string commandId)
    {
        // Act
        var result = CommandValidator.ValidateCommandId(commandId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region Invalid Command Id Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateCommandId_EmptyOrNull_ShouldReturnError(string? commandId)
    {
        // Act
        var result = CommandValidator.ValidateCommandId(commandId!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Id is required"));
    }

    [Theory]
    [InlineData("123command")]    // Starts with number
    [InlineData("_command")]      // Starts with underscore
    [InlineData(".command")]      // Starts with dot
    [InlineData("-command")]      // Starts with hyphen
    public void ValidateCommandId_InvalidFormat_ShouldReturnError(string commandId)
    {
        // Act
        var result = CommandValidator.ValidateCommandId(commandId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("invalid format"));
    }

    #endregion
}

public class ContributionValidationResultTests
{
    [Fact]
    public void Success_ShouldReturnValidResult()
    {
        // Act
        var result = ContributionValidationResult.Success();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Failure_ShouldReturnInvalidResultWithErrors()
    {
        // Act
        var result = ContributionValidationResult.Failure("Error 1", "Error 2");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
    }

    [Fact]
    public void WithWarnings_ShouldReturnValidResultWithWarnings()
    {
        // Act
        var result = ContributionValidationResult.WithWarnings("Warning 1", "Warning 2");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().HaveCount(2);
        result.Warnings.Should().Contain("Warning 1");
        result.Warnings.Should().Contain("Warning 2");
    }
}

public class ContributionValidationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Act
        var exception = new ContributionValidationException("Test error message");

        // Assert
        exception.Message.Should().Be("Test error message");
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner");

        // Act
        var exception = new ContributionValidationException("Outer", inner);

        // Assert
        exception.Message.Should().Be("Outer");
        exception.InnerException.Should().Be(inner);
    }

    [Fact]
    public void ValidationErrors_ShouldBeSettable()
    {
        // Act
        var exception = new ContributionValidationException("Test")
        {
            ValidationErrors = new List<string> { "Error 1", "Error 2" }
        };

        // Assert
        exception.ValidationErrors.Should().HaveCount(2);
        exception.ValidationErrors.Should().Contain("Error 1");
        exception.ValidationErrors.Should().Contain("Error 2");
    }

    [Fact]
    public void ValidationErrors_DefaultValue_ShouldBeEmpty()
    {
        // Act
        var exception = new ContributionValidationException("Test");

        // Assert
        exception.ValidationErrors.Should().BeEmpty();
    }
}
