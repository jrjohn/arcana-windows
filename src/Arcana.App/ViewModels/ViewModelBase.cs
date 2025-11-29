using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arcana.App.ViewModels;

/// <summary>
/// Base class for all ViewModels.
/// 所有 ViewModel 的基類
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

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
    /// Sets an error message.
    /// </summary>
    protected void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    /// <summary>
    /// Clears the error.
    /// </summary>
    protected void ClearError()
    {
        ErrorMessage = null;
        HasError = false;
    }

    /// <summary>
    /// Executes an async operation with loading state.
    /// </summary>
    protected async Task ExecuteWithLoadingAsync(Func<Task> operation, string? errorMessage = null)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsLoading = true;
            ClearError();

            await operation();
        }
        catch (Exception ex)
        {
            SetError(errorMessage ?? ex.Message);
        }
        finally
        {
            IsBusy = false;
            IsLoading = false;
        }
    }

    /// <summary>
    /// Executes an async operation with loading state and returns result.
    /// </summary>
    protected async Task<T?> ExecuteWithLoadingAsync<T>(Func<Task<T>> operation, string? errorMessage = null)
    {
        if (IsBusy) return default;

        try
        {
            IsBusy = true;
            IsLoading = true;
            ClearError();

            return await operation();
        }
        catch (Exception ex)
        {
            SetError(errorMessage ?? ex.Message);
            return default;
        }
        finally
        {
            IsBusy = false;
            IsLoading = false;
        }
    }
}
