using System.Collections.Concurrent;
using Arcana.Plugins.Contracts;

namespace Arcana.Plugins.Services;

/// <summary>
/// Message bus implementation for plugin-to-plugin communication.
/// 插件間通訊的訊息匯流排實作
/// </summary>
public class MessageBus : IMessageBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();
    private readonly ConcurrentDictionary<Type, Delegate> _handlers = new();
    private readonly object _lock = new();

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class
    {
        if (!_subscribers.TryGetValue(typeof(TMessage), out var subscribers))
        {
            return;
        }

        List<Delegate> subscribersCopy;
        lock (_lock)
        {
            subscribersCopy = subscribers.ToList();
        }

        foreach (var subscriber in subscribersCopy)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (subscriber is Action<TMessage> syncHandler)
                {
                    syncHandler(message);
                }
                else if (subscriber is Func<TMessage, Task> asyncHandler)
                {
                    await asyncHandler(message);
                }
            }
            catch
            {
                // Log error but continue with other subscribers
            }
        }
    }

    public IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class
    {
        return SubscribeInternal<TMessage>(handler);
    }

    public IDisposable Subscribe<TMessage>(Func<TMessage, Task> handler) where TMessage : class
    {
        return SubscribeInternal<TMessage>(handler);
    }

    private IDisposable SubscribeInternal<TMessage>(Delegate handler) where TMessage : class
    {
        var messageType = typeof(TMessage);

        lock (_lock)
        {
            if (!_subscribers.TryGetValue(messageType, out var subscribers))
            {
                subscribers = new List<Delegate>();
                _subscribers[messageType] = subscribers;
            }
            subscribers.Add(handler);
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                if (_subscribers.TryGetValue(messageType, out var subscribers))
                {
                    subscribers.Remove(handler);
                }
            }
        });
    }

    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        var key = typeof((TRequest, TResponse));

        if (!_handlers.TryGetValue(key, out var handler))
        {
            return default;
        }

        var typedHandler = (Func<TRequest, Task<TResponse>>)handler;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeout.HasValue)
        {
            cts.CancelAfter(timeout.Value);
        }

        try
        {
            return await typedHandler(request);
        }
        catch (OperationCanceledException)
        {
            return default;
        }
    }

    public IDisposable RegisterHandler<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler)
        where TRequest : class
        where TResponse : class
    {
        var key = typeof((TRequest, TResponse));
        _handlers[key] = handler;

        return new Subscription(() =>
        {
            _handlers.TryRemove(key, out _);
        });
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
