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
    private readonly IWindowService _windowService;
    private IDisposable? _installSubscription;
    private IDisposable? _upgradeSubscription;

    public PluginManagerPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<PluginManagerViewModel>();
        _windowService = App.Services.GetRequiredService<IWindowService>();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _installSubscription = ViewModel.Fx.RequestFileForInstall.Subscribe(OnRequestFileForInstall);
        _upgradeSubscription = ViewModel.Fx.RequestFileForUpgrade.Subscribe(OnRequestFileForUpgrade);
        await ViewModel.InitializeAsync();
    }

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _installSubscription?.Dispose();
        _upgradeSubscription?.Dispose();
        await ViewModel.CleanupAsync();
    }

    private async void OnRequestFileForInstall()
    {
        var files = await _windowService.ShowOpenFileDialogAsync(new FileDialogOptions
        {
            Title = "Select Plugin Package",
            Filters = [new FileFilter("Plugin Package", "zip")]
        });

        if (files != null && files.Length > 0)
        {
            await ViewModel.In.InstallPlugin(files[0]);
        }
    }

    private async void OnRequestFileForUpgrade(PluginItemViewModel plugin)
    {
        var files = await _windowService.ShowOpenFileDialogAsync(new FileDialogOptions
        {
            Title = $"Select Upgrade Package for {plugin.Name}",
            Filters = [new FileFilter("Plugin Package", "zip")]
        });

        if (files != null && files.Length > 0)
        {
            await ViewModel.In.UpgradePlugin(plugin, files[0]);
        }
    }

    private async void RollbackVersion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PluginVersionInfo version)
        {
            await ViewModel.In.RollbackPlugin(version);
        }
    }
}
