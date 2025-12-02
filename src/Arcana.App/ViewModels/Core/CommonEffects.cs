using Arcana.Core.Common;

namespace Arcana.App.ViewModels.Core;

/// <summary>
/// Common effect subjects that can be reused across ViewModels.
/// </summary>
public class CommonEffects : IViewModelEffect, IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Emitted when navigation back is requested.
    /// </summary>
    public EffectSubject NavigateBack { get; } = new();

    /// <summary>
    /// Emitted when navigation to a specific route is requested.
    /// </summary>
    public EffectSubject<NavigationRequest> NavigateTo { get; } = new();

    /// <summary>
    /// Emitted when an error should be displayed to the user.
    /// </summary>
    public EffectSubject<AppError> ShowError { get; } = new();

    /// <summary>
    /// Emitted when a success message should be displayed.
    /// </summary>
    public EffectSubject<string> ShowSuccess { get; } = new();

    /// <summary>
    /// Emitted when a warning message should be displayed.
    /// </summary>
    public EffectSubject<string> ShowWarning { get; } = new();

    /// <summary>
    /// Emitted when an info message should be displayed.
    /// </summary>
    public EffectSubject<string> ShowInfo { get; } = new();

    /// <summary>
    /// Emitted when a confirmation dialog should be shown.
    /// </summary>
    public EffectSubject<ConfirmationRequest> ShowConfirmation { get; } = new();

    /// <summary>
    /// Emitted when the view should be closed.
    /// </summary>
    public EffectSubject CloseView { get; } = new();

    /// <summary>
    /// Emitted when the view should refresh.
    /// </summary>
    public EffectSubject RefreshView { get; } = new();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        NavigateBack.Dispose();
        NavigateTo.Dispose();
        ShowError.Dispose();
        ShowSuccess.Dispose();
        ShowWarning.Dispose();
        ShowInfo.Dispose();
        ShowConfirmation.Dispose();
        CloseView.Dispose();
        RefreshView.Dispose();
    }
}

/// <summary>
/// Represents a navigation request.
/// </summary>
public record NavigationRequest(string Route, object? Parameter = null);

/// <summary>
/// Represents a confirmation dialog request.
/// </summary>
public record ConfirmationRequest(
    string Title,
    string Message,
    Action OnConfirm,
    Action? OnCancel = null,
    string ConfirmText = "Confirm",
    string CancelText = "Cancel"
);
