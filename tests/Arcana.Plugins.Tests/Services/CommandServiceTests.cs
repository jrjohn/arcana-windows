using Arcana.Plugins.Contracts.Validation;
using Arcana.Plugins.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Plugins.Tests.Services;

public class CommandServiceTests
{
    private readonly Mock<ILogger<CommandService>> _loggerMock;
    private readonly CommandService _service;

    public CommandServiceTests()
    {
        _loggerMock = new Mock<ILogger<CommandService>>();
        _service = new CommandService(_loggerMock.Object);
    }

    #region RegisterCommand Tests

    [Fact]
    public void RegisterCommand_ValidCommand_ShouldRegisterSuccessfully()
    {
        // Arrange
        var handler = (object?[] args) => Task.CompletedTask;

        // Act
        var subscription = _service.RegisterCommand("test.command", handler);

        // Assert
        subscription.Should().NotBeNull();
        _service.HasCommand("test.command").Should().BeTrue();
    }

    [Fact]
    public void RegisterCommand_DuplicateId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var handler = (object?[] args) => Task.CompletedTask;
        _service.RegisterCommand("test.command", handler);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            _service.RegisterCommand("test.command", handler));
    }

    [Fact]
    public void RegisterCommand_InvalidId_ShouldThrowContributionValidationException()
    {
        // Arrange
        var handler = (object?[] args) => Task.CompletedTask;

        // Act & Assert
        var exception = Assert.Throws<ContributionValidationException>(() =>
            _service.RegisterCommand("123invalid", handler));
        exception.ValidationErrors.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void RegisterCommand_EmptyOrNullId_ShouldThrowContributionValidationException(string? commandId)
    {
        // Arrange
        var handler = (object?[] args) => Task.CompletedTask;

        // Act & Assert
        var exception = Assert.Throws<ContributionValidationException>(() =>
            _service.RegisterCommand(commandId!, handler));
        exception.ValidationErrors.Should().Contain(e => e.Contains("Id is required"));
    }

    [Theory]
    [InlineData("file.new")]
    [InlineData("order.list")]
    [InlineData("app.exit")]
    [InlineData("command_test")]
    [InlineData("command-test")]
    public void RegisterCommand_ValidIdFormats_ShouldRegisterSuccessfully(string commandId)
    {
        // Arrange
        var handler = (object?[] args) => Task.CompletedTask;

        // Act
        var subscription = _service.RegisterCommand(commandId, handler);

        // Assert
        subscription.Should().NotBeNull();
        _service.HasCommand(commandId).Should().BeTrue();
    }

    [Fact]
    public void RegisterCommand_ShouldLogDebugMessage()
    {
        // Arrange
        var handler = (object?[] args) => Task.CompletedTask;

        // Act
        _service.RegisterCommand("test.command", handler);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Registered command")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region RegisterCommand<T> Tests

    [Fact]
    public void RegisterCommand_Typed_ShouldRegisterSuccessfully()
    {
        // Arrange
        var receivedArg = 0;

        // Act
        _service.RegisterCommand<int>("test.command", arg =>
        {
            receivedArg = arg;
            return Task.CompletedTask;
        });

        // Assert
        _service.HasCommand("test.command").Should().BeTrue();
    }

    [Fact]
    public async Task RegisterCommand_Typed_ShouldPassArgumentCorrectly()
    {
        // Arrange
        var receivedArg = 0;
        _service.RegisterCommand<int>("test.command", arg =>
        {
            receivedArg = arg;
            return Task.CompletedTask;
        });

        // Act
        await _service.ExecuteAsync("test.command", 42);

        // Assert
        receivedArg.Should().Be(42);
    }

    [Fact]
    public async Task RegisterCommand_Typed_ShouldHandleDefaultWhenNoArgument()
    {
        // Arrange
        var receivedArg = -1;
        _service.RegisterCommand<int>("test.command", arg =>
        {
            receivedArg = arg;
            return Task.CompletedTask;
        });

        // Act
        await _service.ExecuteAsync("test.command");

        // Assert
        receivedArg.Should().Be(0); // default(int)
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_RegisteredCommand_ShouldExecuteHandler()
    {
        // Arrange
        var executed = false;
        _service.RegisterCommand("test.command", args =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        // Act
        await _service.ExecuteAsync("test.command");

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithArguments_ShouldPassArguments()
    {
        // Arrange
        object?[]? receivedArgs = null;
        _service.RegisterCommand("test.command", args =>
        {
            receivedArgs = args;
            return Task.CompletedTask;
        });

        // Act
        await _service.ExecuteAsync("test.command", "arg1", 42, true);

        // Assert
        receivedArgs.Should().NotBeNull();
        receivedArgs.Should().HaveCount(3);
        receivedArgs![0].Should().Be("arg1");
        receivedArgs[1].Should().Be(42);
        receivedArgs[2].Should().Be(true);
    }

    [Fact]
    public async Task ExecuteAsync_NonExistingCommand_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ExecuteAsync("non.existing.command"));
        exception.Message.Should().Contain("Command not found");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNull()
    {
        // Arrange
        _service.RegisterCommand("test.command", args => Task.CompletedTask);

        // Act
        var result = await _service.ExecuteAsync("test.command");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetCommands Tests

    [Fact]
    public void GetCommands_ShouldReturnAllRegisteredCommands()
    {
        // Arrange
        _service.RegisterCommand("command1", args => Task.CompletedTask);
        _service.RegisterCommand("command2", args => Task.CompletedTask);
        _service.RegisterCommand("command3", args => Task.CompletedTask);

        // Act
        var commands = _service.GetCommands();

        // Assert
        commands.Should().HaveCount(3);
        commands.Should().Contain("command1");
        commands.Should().Contain("command2");
        commands.Should().Contain("command3");
    }

    [Fact]
    public void GetCommands_NoRegistrations_ShouldReturnEmptyList()
    {
        // Act
        var commands = _service.GetCommands();

        // Assert
        commands.Should().BeEmpty();
    }

    #endregion

    #region HasCommand Tests

    [Fact]
    public void HasCommand_RegisteredCommand_ShouldReturnTrue()
    {
        // Arrange
        _service.RegisterCommand("test.command", args => Task.CompletedTask);

        // Act
        var result = _service.HasCommand("test.command");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasCommand_NonRegisteredCommand_ShouldReturnFalse()
    {
        // Act
        var result = _service.HasCommand("non.existing");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasCommand_AfterUnregister_ShouldReturnFalse()
    {
        // Arrange
        var subscription = _service.RegisterCommand("test.command", args => Task.CompletedTask);

        // Act
        subscription.Dispose();

        // Assert
        _service.HasCommand("test.command").Should().BeFalse();
    }

    #endregion

    #region Unregister Tests

    [Fact]
    public void Dispose_Subscription_ShouldRemoveCommand()
    {
        // Arrange
        var subscription = _service.RegisterCommand("test.command", args => Task.CompletedTask);

        // Act
        subscription.Dispose();

        // Assert
        _service.HasCommand("test.command").Should().BeFalse();
    }

    [Fact]
    public async Task Dispose_Subscription_ExecuteAsync_ShouldThrow()
    {
        // Arrange
        var subscription = _service.RegisterCommand("test.command", args => Task.CompletedTask);
        subscription.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.ExecuteAsync("test.command"));
    }

    [Fact]
    public void Dispose_Subscription_MultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var subscription = _service.RegisterCommand("test.command", args => Task.CompletedTask);

        // Act & Assert - Should not throw
        subscription.Dispose();
        subscription.Dispose(); // Second dispose should be safe
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithoutLogger_ShouldWork()
    {
        // Act
        var service = new CommandService();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithLogger_ShouldWork()
    {
        // Arrange
        var logger = new Mock<ILogger<CommandService>>().Object;

        // Act
        var service = new CommandService(logger);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task ConcurrentRegistration_ShouldBeThreadSafe()
    {
        // Arrange
        var service = new CommandService();
        var tasks = new List<Task>();

        // Act - Register 100 commands concurrently
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
                service.RegisterCommand($"command{index}", args => Task.CompletedTask)));
        }

        await Task.WhenAll(tasks);

        // Assert
        service.GetCommands().Should().HaveCount(100);
    }

    [Fact]
    public async Task ConcurrentExecution_ShouldBeThreadSafe()
    {
        // Arrange
        var executionCount = 0;
        _service.RegisterCommand("test.command", args =>
        {
            Interlocked.Increment(ref executionCount);
            return Task.CompletedTask;
        });

        var tasks = new List<Task>();

        // Act - Execute 100 times concurrently
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_service.ExecuteAsync("test.command"));
        }

        await Task.WhenAll(tasks);

        // Assert
        executionCount.Should().Be(100);
    }

    #endregion
}
