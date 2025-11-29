namespace Arcana.Core.Common;

/// <summary>
/// Error codes for the application.
/// </summary>
public enum ErrorCode
{
    // Network errors (1000-1999)
    NetworkUnavailable = 1000,
    ConnectionTimeout = 1001,
    ServerUnreachable = 1002,

    // Validation errors (2000-2999)
    ValidationFailed = 2000,
    InvalidEmail = 2001,
    RequiredFieldMissing = 2002,
    InvalidFormat = 2003,
    ValueOutOfRange = 2004,

    // Server errors (3000-3999)
    ServerError = 3000,
    NotFound = 3001,
    Unauthorized = 3002,
    Forbidden = 3003,
    BadRequest = 3004,

    // Authentication errors (4000-4999)
    AuthenticationFailed = 4000,
    TokenExpired = 4001,
    InvalidCredentials = 4002,
    AccountLocked = 4003,
    SessionExpired = 4004,

    // Data errors (5000-5999)
    ConflictError = 5000,
    DataCorruption = 5001,
    DuplicateEntry = 5002,
    ReferentialIntegrity = 5003,

    // Database errors (6000-6999)
    DatabaseError = 6000,
    MigrationFailed = 6001,
    ConnectionFailed = 6002,
    QueryFailed = 6003,

    // Plugin errors (7000-7999)
    PluginLoadFailed = 7000,
    PluginNotFound = 7001,
    PluginIncompatible = 7002,
    PluginDisabled = 7003,

    // File errors (8000-8999)
    FileNotFound = 8000,
    FileAccessDenied = 8001,
    FileCorrupted = 8002,

    // Unknown (9000-9999)
    Unknown = 9000
}

/// <summary>
/// Base class for all application errors.
/// </summary>
public abstract record AppError(ErrorCode Code, string Message, Exception? InnerException = null)
{
    /// <summary>
    /// Creates a user-friendly error message
    /// </summary>
    public virtual string ToUserMessage() => Message;

    /// <summary>
    /// Network-related errors
    /// </summary>
    public sealed record Network(ErrorCode Code, string Message, Exception? Inner = null)
        : AppError(Code, Message, Inner);

    /// <summary>
    /// Validation errors with a list of specific validation issues
    /// </summary>
    public sealed record Validation(ErrorCode Code, string Message, IReadOnlyList<string> Errors)
        : AppError(Code, Message)
    {
        public override string ToUserMessage()
            => Errors.Count > 0 ? string.Join("\n", Errors) : Message;
    }

    /// <summary>
    /// Server/API errors
    /// </summary>
    public sealed record Server(ErrorCode Code, string Message, int? StatusCode = null)
        : AppError(Code, Message);

    /// <summary>
    /// Data layer errors
    /// </summary>
    public sealed record Data(ErrorCode Code, string Message, Exception? Inner = null)
        : AppError(Code, Message, Inner);

    /// <summary>
    /// Authentication/Authorization errors
    /// </summary>
    public sealed record Auth(ErrorCode Code, string Message)
        : AppError(Code, Message);

    /// <summary>
    /// Plugin system errors
    /// </summary>
    public sealed record Plugin(ErrorCode Code, string Message, string? PluginId = null)
        : AppError(Code, Message);

    /// <summary>
    /// Unknown/unhandled errors
    /// </summary>
    public sealed record Unknown(string Message, Exception? Inner = null)
        : AppError(ErrorCode.Unknown, Message, Inner);
}
