namespace Arcana.Core.Common;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// 代表操作結果，可能成功並帶有值，或失敗並帶有錯誤
/// </summary>
/// <typeparam name="T">The type of the value on success</typeparam>
public readonly struct Result<T>
{
    /// <summary>
    /// The value if the operation succeeded
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// The error if the operation failed
    /// </summary>
    public AppError? Error { get; }

    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool IsSuccess => Error == null;

    /// <summary>
    /// Whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    private Result(T? value, AppError? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result with the given value
    /// </summary>
    public static Result<T> Success(T value) => new(value, null);

    /// <summary>
    /// Creates a failed result with the given error
    /// </summary>
    public static Result<T> Failure(AppError error) => new(default, error);

    /// <summary>
    /// Pattern matches on the result, calling the appropriate function based on success or failure
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> success, Func<AppError, TResult> failure)
        => IsSuccess ? success(Value!) : failure(Error!);

    /// <summary>
    /// Maps the success value to a new type
    /// </summary>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        => IsSuccess ? Result<TNew>.Success(mapper(Value!)) : Result<TNew>.Failure(Error!);

    /// <summary>
    /// Chains another operation that returns a Result
    /// </summary>
    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
        => IsSuccess ? binder(Value!) : Result<TNew>.Failure(Error!);

    /// <summary>
    /// Returns the value if successful, or the default value if failed
    /// </summary>
    public T GetValueOrDefault(T defaultValue = default!)
        => IsSuccess ? Value! : defaultValue;

    /// <summary>
    /// Executes an action if the result is successful
    /// </summary>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess) action(Value!);
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure
    /// </summary>
    public Result<T> OnFailure(Action<AppError> action)
    {
        if (IsFailure) action(Error!);
        return this;
    }

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(AppError error) => Failure(error);
}

/// <summary>
/// Represents a result without a value (unit result)
/// </summary>
public readonly struct Result
{
    public AppError? Error { get; }
    public bool IsSuccess => Error == null;
    public bool IsFailure => !IsSuccess;

    private Result(AppError? error) => Error = error;

    public static Result Success() => new(null);
    public static Result Failure(AppError error) => new(error);

    public TResult Match<TResult>(Func<TResult> success, Func<AppError, TResult> failure)
        => IsSuccess ? success() : failure(Error!);

    public static implicit operator Result(AppError error) => Failure(error);
}
