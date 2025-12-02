using Arcana.App.ViewModels;
using Arcana.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Arcana.App.Views;

/// <summary>
/// Plugin manager page for viewing and managing plugins.
/// </summary>
public sealed partial class PluginManagerPage : Page
{
    public PluginManagerViewModel ViewModel { get; }

    public PluginManagerPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<PluginManagerViewModel>();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.CleanupAsync();
    }

    private async void RollbackVersion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PluginVersionInfo version)
        {
            await ViewModel.In.RollbackPlugin(version);
        }
    }
}
