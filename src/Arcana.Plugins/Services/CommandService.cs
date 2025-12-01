using System.Collections.Concurrent;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Contracts.Validation;
using Microsoft.Extensions.Logging;

namespace Arcana.Plugins.Services;

/// <summary>
/// Command service implementation.
/// </summary>
public class CommandService : ICommandService
{
    private readonly ConcurrentDictionary<string, Func<object?[], Task>> _commands = new();
    private readonly ILogger<CommandService>? _logger;

    public CommandService() { }

    public CommandService(ILogger<CommandService> logger)
    {
        _logger = logger;
    }

    public IDisposable RegisterCommand(string commandId, Func<object?[], Task> handler)
    {
        ValidateCommandId(commandId);

        if (!_commands.TryAdd(commandId, handler))
        {
            _logger?.LogInformation("Command already registered, skipping: {CommandId}", commandId);
            return new Subscription(() => { });
        }

        _logger?.LogInformation("Registered command: {CommandId}", commandId);
        return new Subscription(() =>
        {
            _logger?.LogInformation("Disposing command: {CommandId}", commandId);
            _commands.TryRemove(commandId, out _);
        });
    }

    private void ValidateCommandId(string commandId)
    {
        var result = CommandValidator.ValidateCommandId(commandId);

        if (!result.IsValid)
        {
            var errorMessage = string.Join("; ", result.Errors);
            _logger?.LogError("Command validation failed: {Errors}", errorMessage);
            throw new ContributionValidationException($"Command validation failed: {errorMessage}")
            {
                ValidationErrors = result.Errors
            };
        }
    }

    public IDisposable RegisterCommand<T>(string commandId, Func<T, Task> handler)
    {
        return RegisterCommand(commandId, args =>
        {
            var arg = args.Length > 0 && args[0] is T typedArg ? typedArg : default!;
            return handler(arg);
        });
    }

    public async Task<object?> ExecuteAsync(string commandId, params object?[] args)
    {
        if (!_commands.TryGetValue(commandId, out var handler))
        {
            throw new InvalidOperationException($"Command not found: {commandId}");
        }

        await handler(args);
        return null;
    }

    public IReadOnlyList<string> GetCommands()
    {
        return _commands.Keys.ToList();
    }

    public bool HasCommand(string commandId)
    {
        return _commands.ContainsKey(commandId);
    }

    private class Subscription : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public Subscription(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _dispose();
            }
        }
    }
}
