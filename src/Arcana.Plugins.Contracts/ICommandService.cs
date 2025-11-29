namespace Arcana.Plugins.Contracts;

/// <summary>
/// Command service for registering and executing commands.
/// </summary>
public interface ICommandService
{
    /// <summary>
    /// Registers a command.
    /// </summary>
    IDisposable RegisterCommand(string commandId, Func<object?[], Task> handler);

    /// <summary>
    /// Registers a command with typed argument.
    /// </summary>
    IDisposable RegisterCommand<T>(string commandId, Func<T, Task> handler);

    /// <summary>
    /// Executes a command by ID.
    /// </summary>
    Task<object?> ExecuteAsync(string commandId, params object?[] args);

    /// <summary>
    /// Gets all registered command IDs.
    /// </summary>
    IReadOnlyList<string> GetCommands();

    /// <summary>
    /// Checks if a command exists.
    /// </summary>
    bool HasCommand(string commandId);
}

/// <summary>
/// Command definition.
/// </summary>
public record CommandDefinition
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Category { get; init; }
    public string? Icon { get; init; }
    public string? Tooltip { get; init; }
    public string? Shortcut { get; init; }
    public string? When { get; init; }
}
