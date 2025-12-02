using Microsoft.UI.Xaml;

namespace Arcana.Plugin.FlowChart.TestHost;

/// <summary>
/// FlowChart Plugin Test Host Application.
/// This app allows testing the FlowChart plugin in isolation without the main Arcana app.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
