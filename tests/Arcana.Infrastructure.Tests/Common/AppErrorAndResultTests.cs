using Arcana.Core.Common;
using FluentAssertions;
using Xunit;

namespace Arcana.Infrastructure.Tests.Common;

public class AppErrorTests
{
    [Fact]
    public void NetworkError_ToUserMessage_ShouldReturnMessage()
    {
        var err = new AppError.Network(ErrorCode.NetworkUnavailable, "No network");
        err.ToUserMessage().Should().Be("No network");
        err.Code.Should().Be(ErrorCode.NetworkUnavailable);
    }

    [Fact]
    public void ValidationError_WithErrors_ToUserMessage_ShouldJoinErrors()
    {
        var err = new AppError.Validation(ErrorCode.ValidationFailed, "Invalid", ["Field required", "Too short"]);
        err.ToUserMessage().Should().Contain("Field required");
        err.ToUserMessage().Should().Contain("Too short");
        err.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void ValidationError_EmptyErrors_ToUserMessage_ShouldReturnMessage()
    {
        var err = new AppError.Validation(ErrorCode.ValidationFailed, "Invalid input", []);
        err.ToUserMessage().Should().Be("Invalid input");
    }

    [Fact]
    public void ServerError_ShouldHaveStatusCode()
    {
        var err = new AppError.Server(ErrorCode.ServerError, "Server error", 500);
        err.StatusCode.Should().Be(500);
        err.ToUserMessage().Should().Be("Server error");
    }

    [Fact]
    public void AuthError_ShouldSetProperties()
    {
        var err = new AppError.Auth(ErrorCode.AuthenticationFailed, "Invalid credentials");
        err.Code.Should().Be(ErrorCode.AuthenticationFailed);
        err.Message.Should().Be("Invalid credentials");
    }

    [Fact]
    public void PluginError_ShouldSetPluginId()
    {
        var err = new AppError.Plugin(ErrorCode.PluginNotFound, "Plugin missing", "arcana.order");
        err.PluginId.Should().Be("arcana.order");
        err.Code.Should().Be(ErrorCode.PluginNotFound);
    }

    [Fact]
    public void UnknownError_WithException_ShouldSetInnerException()
    {
        var inner = new InvalidOperationException("cause");
        var err = new AppError.Unknown("Unexpected error", inner);
        err.Code.Should().Be(ErrorCode.Unknown);
        err.InnerException.Should().Be(inner);
    }

    [Fact]
    public void DataError_ShouldSetProperties()
    {
        var err = new AppError.Data(ErrorCode.DatabaseError, "DB failed");
        err.Code.Should().Be(ErrorCode.DatabaseError);
        err.ToUserMessage().Should().Be("DB failed");
    }
}

public class ResultTests
{
    [Fact]
    public void Success_ShouldSetValueAndIsSuccess()
    {
        var result = Result<int>.Success(42);
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldSetErrorAndIsFailure()
    {
        var error = new AppError.Auth(ErrorCode.AuthenticationFailed, "Unauthorized");
        var result = Result<string>.Failure(error);
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Match_OnSuccess_ShouldCallSuccessHandler()
    {
        var result = Result<int>.Success(10);
        var output = result.Match(v => v * 2, _ => -1);
        output.Should().Be(20);
    }

    [Fact]
    public void Match_OnFailure_ShouldCallFailureHandler()
    {
        var error = new AppError.Network(ErrorCode.NetworkUnavailable, "No network");
        var result = Result<int>.Failure(error);
        var output = result.Match(v => v, _ => -99);
        output.Should().Be(-99);
    }

    [Fact]
    public void Map_OnSuccess_ShouldTransformValue()
    {
        var result = Result<int>.Success(5).Map(v => v.ToString());
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("5");
    }

    [Fact]
    public void Bind_OnSuccess_ShouldChain()
    {
        var result = Result<int>.Success(5).Bind(v => Result<string>.Success($"val={v}"));
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("val=5");
    }

    [Fact]
    public void GetValueOrDefault_OnFailure_ShouldReturnDefault()
    {
        var error = new AppError.Auth(ErrorCode.Unauthorized, "Forbidden");
        var result = Result<int>.Failure(error);
        result.GetValueOrDefault(99).Should().Be(99);
    }

    [Fact]
    public void OnSuccess_ShouldInvokeAction()
    {
        var called = false;
        Result<int>.Success(1).OnSuccess(_ => called = true);
        called.Should().BeTrue();
    }

    [Fact]
    public void OnFailure_ShouldInvokeAction()
    {
        var called = false;
        var err = new AppError.Auth(ErrorCode.AuthenticationFailed, "Fail");
        Result<int>.Failure(err).OnFailure(_ => called = true);
        called.Should().BeTrue();
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateSuccess()
    {
        Result<string> result = "hello";
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void ImplicitConversion_FromError_ShouldCreateFailure()
    {
        AppError err = new AppError.Auth(ErrorCode.Forbidden, "No access");
        Result<int> result = err;
        result.IsFailure.Should().BeTrue();
    }
}
