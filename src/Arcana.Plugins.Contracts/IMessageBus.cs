namespace Arcana.Plugins.Contracts;

/// <summary>
/// Message bus for plugin-to-plugin communication.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publishes a message to all subscribers.
    /// </summary>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class;

    /// <summary>
    /// Subscribes to messages of a specific type.
    /// </summary>
    IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class;

    /// <summary>
    /// Subscribes to messages of a specific type with async handler.
    /// </summary>
    IDisposable Subscribe<TMessage>(Func<TMessage, Task> handler) where TMessage : class;

    /// <summary>
    /// Sends a request and waits for a response.
    /// </summary>
    Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// Registers a handler for requests.
    /// </summary>
    IDisposable RegisterHandler<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler)
        where TRequest : class
        where TResponse : class;
}

/// <summary>
/// Event aggregator for application-wide events.
/// </summary>
public interface IEventAggregator
{
    /// <summary>
    /// Publishes an event.
    /// </summary>
    void Publish<TEvent>(TEvent @event) where TEvent : IApplicationEvent;

    /// <summary>
    /// Subscribes to an event type.
    /// </summary>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IApplicationEvent;
}

/// <summary>
/// Marker interface for application events.
/// </summary>
public interface IApplicationEvent
{
    DateTime Timestamp { get; }
    string? SourcePluginId { get; }
}

/// <summary>
/// Base class for application events.
/// </summary>
public abstract record ApplicationEventBase : IApplicationEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string? SourcePluginId { get; init; }
}

/// <summary>
/// Shared state store for cross-plugin state.
/// </summary>
public interface ISharedStateStore
{
    /// <summary>
    /// Gets a value from the shared state.
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// Sets a value in the shared state.
    /// </summary>
    void Set<T>(string key, T value);

    /// <summary>
    /// Removes a value from the shared state.
    /// </summary>
    bool Remove(string key);

    /// <summary>
    /// Checks if a key exists.
    /// </summary>
    bool ContainsKey(string key);

    /// <summary>
    /// Subscribes to state changes for a key.
    /// </summary>
    IDisposable OnChange<T>(string key, Action<T?> handler);
}
