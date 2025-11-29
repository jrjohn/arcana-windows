namespace Arcana.Plugins.Contracts;

/// <summary>
/// Window service for UI interactions.
/// 視窗服務，用於 UI 互動
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// Shows an information message.
    /// </summary>
    Task<string?> ShowInfoAsync(string message, params string[] actions);

    /// <summary>
    /// Shows a warning message.
    /// </summary>
    Task<string?> ShowWarningAsync(string message, params string[] actions);

    /// <summary>
    /// Shows an error message.
    /// </summary>
    Task<string?> ShowErrorAsync(string message, params string[] actions);

    /// <summary>
    /// Shows an input dialog.
    /// </summary>
    Task<string?> ShowInputAsync(InputOptions options);

    /// <summary>
    /// Shows a confirmation dialog.
    /// </summary>
    Task<bool> ShowConfirmAsync(string title, string message);

    /// <summary>
    /// Shows a file open dialog.
    /// </summary>
    Task<string[]?> ShowOpenFileDialogAsync(FileDialogOptions options);

    /// <summary>
    /// Shows a file save dialog.
    /// </summary>
    Task<string?> ShowSaveFileDialogAsync(FileDialogOptions options);

    /// <summary>
    /// Shows a folder picker dialog.
    /// </summary>
    Task<string?> ShowFolderPickerAsync(string? title = null);

    /// <summary>
    /// Shows a progress dialog.
    /// </summary>
    Task<IProgressDialog> ShowProgressAsync(string title, string? message = null, bool isCancellable = false);

    /// <summary>
    /// Creates a status bar item.
    /// </summary>
    IStatusBarItem CreateStatusBarItem(StatusBarAlignment alignment = StatusBarAlignment.Left, int priority = 0);
}

/// <summary>
/// Input dialog options.
/// </summary>
public record InputOptions
{
    public string? Title { get; init; }
    public string? Prompt { get; init; }
    public string? Placeholder { get; init; }
    public string? DefaultValue { get; init; }
    public bool IsPassword { get; init; }
    public Func<string, string?>? Validator { get; init; }
}

/// <summary>
/// File dialog options.
/// </summary>
public record FileDialogOptions
{
    public string? Title { get; init; }
    public string? DefaultPath { get; init; }
    public string? DefaultFileName { get; init; }
    public bool AllowMultiple { get; init; }
    public IReadOnlyList<FileFilter>? Filters { get; init; }
}

/// <summary>
/// File filter for file dialogs.
/// </summary>
public record FileFilter(string Name, params string[] Extensions);

/// <summary>
/// Progress dialog interface.
/// </summary>
public interface IProgressDialog : IDisposable
{
    string? Title { get; set; }
    string? Message { get; set; }
    double? Progress { get; set; }
    bool IsCancelled { get; }
    CancellationToken CancellationToken { get; }
}

/// <summary>
/// Status bar item interface.
/// </summary>
public interface IStatusBarItem : IDisposable
{
    string? Text { get; set; }
    string? Tooltip { get; set; }
    string? Icon { get; set; }
    bool IsVisible { get; set; }
    string? Command { get; set; }
}

/// <summary>
/// Status bar alignment.
/// </summary>
public enum StatusBarAlignment
{
    Left,
    Right
}
