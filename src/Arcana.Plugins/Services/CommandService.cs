using System.Collections.Concurrent;
using Arcana.Plugins.Contracts;

namespace Arcana.Plugins.Services;

/// <summary>
/// Command service implementation.
/// </summary>
public class CommandService : ICommandService
{
    private readonly ConcurrentDictionary<string, Func<object?[], Task>> _commands = new();

    public IDisposable RegisterCommand(string commandId, Func<object?[], Task> handler)
    {
        if (!_commands.TryAdd(commandId, handler))
        {
            throw new InvalidOperationException($"Command already registered: {commandId}");
        }

        return new Subscription(() => _commands.TryRemove(commandId, out _));
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
