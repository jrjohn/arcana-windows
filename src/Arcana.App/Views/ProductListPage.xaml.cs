using Arcana.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Arcana.App.Views;

/// <summary>
/// Product list page.
/// </summary>
public sealed partial class ProductListPage : Page
{
    private readonly ILocalizationService _localization;

    public ProductListPage()
    {
        this.InitializeComponent();
        _localization = App.Services.GetRequiredService<ILocalizationService>();
        _localization.CultureChanged += OnCultureChanged;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyLocalization();
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(ApplyLocalization);
    }

    private void ApplyLocalization()
    {
        PageTitle.Text = _localization.Get("product.list");
        DevelopingText.Text = _localization.Get("common.developing");
    }
}
