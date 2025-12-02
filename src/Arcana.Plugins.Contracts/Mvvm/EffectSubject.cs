namespace Arcana.Plugins.Contracts.Mvvm;

/// <summary>
/// A subject for emitting one-time effects (side effects) from ViewModels.
/// Similar to RxJS Subject in Angular, used for imperative side effects.
/// </summary>
/// <typeparam name="T">The type of value emitted</typeparam>
public sealed class EffectSubject<T> : IDisposable
{
    private readonly List<Action<T>> _subscribers = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Emits a new value to all subscribers.
    /// </summary>
    public void Emit(T value)
    {
        if (_disposed) return;

        Action<T>[] subscribersCopy;
        lock (_lock)
        {
            subscribersCopy = _subscribers.ToArray();
        }

        foreach (var subscriber in subscribersCopy)
        {
            try
            {
                subscriber(value);
            }
            catch
            {
                // Swallow exceptions to prevent one subscriber from affecting others
            }
        }
    }

    /// <summary>
    /// Subscribes to the effect stream.
    /// </summary>
    public IDisposable Subscribe(Action<T> onNext)
    {
        if (_disposed) return new EmptyDisposable();

        lock (_lock)
        {
            _subscribers.Add(onNext);
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                _subscribers.Remove(onNext);
            }
        });
    }

    /// <summary>
    /// Subscribes to the effect stream with error handling.
    /// </summary>
    public IDisposable Subscribe(Action<T> onNext, Action<Exception> onError)
    {
        if (_disposed) return new EmptyDisposable();

        Action<T> wrappedAction = value =>
        {
            try
            {
                onNext(value);
            }
            catch (Exception ex)
            {
                onError(ex);
            }
        };

        lock (_lock)
        {
            _subscribers.Add(wrappedAction);
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                _subscribers.Remove(wrappedAction);
            }
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            _subscribers.Clear();
        }
    }
}

/// <summary>
/// A subject for emitting void/unit effects (events with no payload).
/// </summary>
public sealed class EffectSubject : IDisposable
{
    private readonly List<Action> _subscribers = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Emits the effect to all subscribers.
    /// </summary>
    public void Emit()
    {
        if (_disposed) return;

        Action[] subscribersCopy;
        lock (_lock)
        {
            subscribersCopy = _subscribers.ToArray();
        }

        foreach (var subscriber in subscribersCopy)
        {
            try
            {
                subscriber();
            }
            catch
            {
                // Swallow exceptions to prevent one subscriber from affecting others
            }
        }
    }

    /// <summary>
    /// Subscribes to the effect stream.
    /// </summary>
    public IDisposable Subscribe(Action onNext)
    {
        if (_disposed) return new EmptyDisposable();

        lock (_lock)
        {
            _subscribers.Add(onNext);
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                _subscribers.Remove(onNext);
            }
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            _subscribers.Clear();
        }
    }
}

/// <summary>
/// Represents void/no value for effect subjects.
/// </summary>
public readonly struct Unit
{
    public static readonly Unit Default = new();
}

/// <summary>
/// Internal class for managing subscriptions.
/// </summary>
internal sealed class Subscription : IDisposable
{
    private Action? _unsubscribe;

    public Subscription(Action unsubscribe)
    {
        _unsubscribe = unsubscribe;
    }

    public void Dispose()
    {
        _unsubscribe?.Invoke();
        _unsubscribe = null;
    }
}

/// <summary>
/// Internal class for empty/no-op disposables.
/// </summary>
internal sealed class EmptyDisposable : IDisposable
{
    public void Dispose() { }
}
