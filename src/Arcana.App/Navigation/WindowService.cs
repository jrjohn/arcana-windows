using Arcana.Plugins.Contracts;
using Microsoft.UI.Xaml.Controls;

namespace Arcana.App.Navigation;

/// <summary>
/// Window service implementation for WinUI dialogs.
/// </summary>
public class WindowService : IWindowService
{
    public async Task<string?> ShowInfoAsync(string message, params string[] actions)
    {
        var dialog = new ContentDialog
        {
            Title = "Information",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = GetXamlRoot()
        };

        if (actions.Length > 0)
        {
            dialog.PrimaryButtonText = actions[0];
        }
        if (actions.Length > 1)
        {
            dialog.SecondaryButtonText = actions[1];
        }

        var result = await dialog.ShowAsync();
        return result switch
        {
            ContentDialogResult.Primary => actions.Length > 0 ? actions[0] : null,
            ContentDialogResult.Secondary => actions.Length > 1 ? actions[1] : null,
            _ => null
        };
    }

    public async Task<string?> ShowWarningAsync(string message, params string[] actions)
    {
        var dialog = new ContentDialog
        {
            Title = "Warning",
            Content = message,
            CloseButtonText = "Cancel",
            XamlRoot = GetXamlRoot()
        };

        if (actions.Length > 0)
        {
            dialog.PrimaryButtonText = actions[0];
        }

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary && actions.Length > 0 ? actions[0] : null;
    }

    public async Task<string?> ShowErrorAsync(string message, params string[] actions)
    {
        var dialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = GetXamlRoot()
        };

        await dialog.ShowAsync();
        return null;
    }

    public async Task<string?> ShowInputAsync(InputOptions options)
    {
        var textBox = new TextBox
        {
            PlaceholderText = options.Placeholder,
            Text = options.DefaultValue ?? string.Empty
        };

        if (options.IsPassword)
        {
            // Use PasswordBox instead
        }

        var dialog = new ContentDialog
        {
            Title = options.Title ?? "Input",
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = options.Prompt },
                    textBox
                }
            },
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            XamlRoot = GetXamlRoot()
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? textBox.Text : null;
    }

    public async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            XamlRoot = GetXamlRoot()
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public Task<string[]?> ShowOpenFileDialogAsync(FileDialogOptions options)
    {
        // Implement using Windows.Storage.Pickers
        return Task.FromResult<string[]?>(null);
    }

    public Task<string?> ShowSaveFileDialogAsync(FileDialogOptions options)
    {
        // Implement using Windows.Storage.Pickers
        return Task.FromResult<string?>(null);
    }

    public Task<string?> ShowFolderPickerAsync(string? title = null)
    {
        // Implement using Windows.Storage.Pickers
        return Task.FromResult<string?>(null);
    }

    public Task<IProgressDialog> ShowProgressAsync(string title, string? message = null, bool isCancellable = false)
    {
        // Implement progress dialog
        return Task.FromResult<IProgressDialog>(new ProgressDialogImpl(title, message, isCancellable));
    }

    public IStatusBarItem CreateStatusBarItem(StatusBarAlignment alignment = StatusBarAlignment.Left, int priority = 0)
    {
        return new StatusBarItemImpl();
    }

    private static Microsoft.UI.Xaml.XamlRoot? GetXamlRoot()
    {
        // Get XamlRoot from main window's content
        if (App.MainWindow?.Content is Microsoft.UI.Xaml.FrameworkElement element)
        {
            return element.XamlRoot;
        }
        return null;
    }
}

internal class ProgressDialogImpl : IProgressDialog
{
    public string? Title { get; set; }
    public string? Message { get; set; }
    public double? Progress { get; set; }
    public bool IsCancelled { get; private set; }
    public CancellationToken CancellationToken { get; }

    private readonly CancellationTokenSource _cts;

    public ProgressDialogImpl(string title, string? message, bool isCancellable)
    {
        Title = title;
        Message = message;
        _cts = new CancellationTokenSource();
        CancellationToken = _cts.Token;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}

internal class StatusBarItemImpl : IStatusBarItem
{
    public string? Text { get; set; }
    public string? Tooltip { get; set; }
    public string? Icon { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? Command { get; set; }

    public void Dispose() { }
}
