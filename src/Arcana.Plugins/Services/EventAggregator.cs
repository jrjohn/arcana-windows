using System.Collections.Concurrent;
using Arcana.Plugins.Contracts;

namespace Arcana.Plugins.Services;

/// <summary>
/// Event aggregator implementation for application-wide events.
/// </summary>
public class EventAggregator : IEventAggregator
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();
    private readonly object _lock = new();

    public void Publish<TEvent>(TEvent @event) where TEvent : IApplicationEvent
    {
        if (!_subscribers.TryGetValue(typeof(TEvent), out var subscribers))
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
            try
            {
                if (subscriber is Action<TEvent> handler)
                {
                    handler(@event);
                }
            }
            catch
            {
                // Log error but continue with other subscribers
            }
        }
    }

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IApplicationEvent
    {
        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (!_subscribers.TryGetValue(eventType, out var subscribers))
            {
                subscribers = new List<Delegate>();
                _subscribers[eventType] = subscribers;
            }
            subscribers.Add(handler);
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                if (_subscribers.TryGetValue(eventType, out var subscribers))
                {
                    subscribers.Remove(handler);
                }
            }
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
