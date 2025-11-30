namespace Arcana.Plugins.Contracts.Validation;

/// <summary>
/// Exception thrown when contribution validation fails.
/// </summary>
public class ContributionValidationException : Exception
{
    public ContributionValidationException(string message) : base(message) { }
    public ContributionValidationException(string message, Exception innerException) : base(message, innerException) { }
    public IReadOnlyList<string> ValidationErrors { get; init; } = [];
}

/// <summary>
/// Validation result for contributions.
/// </summary>
public record ContributionValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public static ContributionValidationResult Success() => new() { IsValid = true };

    public static ContributionValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors
    };

    public static ContributionValidationResult WithWarnings(params string[] warnings) => new()
    {
        IsValid = true,
        Warnings = warnings
    };
}
