using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcana.Plugins.Contracts.Mvvm;

/// <summary>
/// Base class for ViewModels following the UDF (Unidirectional Data Flow) pattern.
/// Implements Input/Output/Effect separation for predictable state management.
/// </summary>
/// <remarks>
/// UDF Architecture:
///
/// ┌─────────────────────────────────────────────────────────┐
/// │                         VIEW                            │
/// │  ┌─────────────────┐              ┌─────────────────┐  │
/// │  │  User Actions   │              │  UI Rendering   │  │
/// │  └────────┬────────┘              └────────▲────────┘  │
/// └───────────┼────────────────────────────────┼───────────┘
///             │                                │
///             ▼                                │
/// ┌───────────────────┐              ┌─────────────────────┐
/// │      INPUT        │              │      OUTPUT         │
/// │  (Action Methods) │              │  (Readonly State)   │
/// └─────────┬─────────┘              └──────────▲──────────┘
///           │                                   │
///           ▼                                   │
/// ┌─────────────────────────────────────────────────────────┐
/// │                    VIEWMODEL STATE                      │
/// │                  (Private Signals)                      │
/// └─────────────────────────────────────────────────────────┘
///           │
///           ▼
/// ┌───────────────────┐
/// │      EFFECT       │
/// │  (Side Effects)   │
/// │  Navigation, etc  │
/// └───────────────────┘
///
/// Usage with nested classes:
/// <code>
/// public partial class MyViewModel : ReactiveViewModelBase
/// {
///     public Input In => ...;
///     public Output Out => ...;
///     public Effect Fx => ...;
///
///     public sealed class Input : IViewModelInput { ... }
///     public sealed class Output : IViewModelOutput { ... }
///     public sealed class Effect : IViewModelEffect { ... }
/// }
/// </code>
/// </remarks>
public abstract class ReactiveViewModelBase : ObservableObject, IDisposable
{
    private readonly List<IDisposable> _disposables = new();
    private bool _disposed;

    /// <summary>
    /// Called when the view is loaded.
    /// </summary>
    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the view is unloaded.
    /// </summary>
    public virtual Task CleanupAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Registers a disposable to be cleaned up when the ViewModel is disposed.
    /// </summary>
    protected void AddDisposable(IDisposable disposable)
    {
        _disposables.Add(disposable);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
            _disposables.Clear();
        }

        _disposed = true;
    }
}
