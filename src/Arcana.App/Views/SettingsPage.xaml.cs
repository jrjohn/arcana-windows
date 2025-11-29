using Arcana.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Arcana.App.Views;

/// <summary>
/// Settings page.
/// 設定頁面
/// </summary>
public sealed partial class SettingsPage : Page
{
    private readonly ILocalizationService _localization;
    private bool _isInitializing = true;

    public SettingsPage()
    {
        this.InitializeComponent();
        _localization = App.Services.GetRequiredService<ILocalizationService>();

        // Subscribe to culture changes
        _localization.CultureChanged += OnCultureChanged;

        // Initialize UI
        InitializeLanguageSelection();
        UpdateLocalizedStrings();

        _isInitializing = false;
    }

    private void InitializeLanguageSelection()
    {
        // Select current language in ComboBox
        var currentCulture = _localization.CurrentCulture.Name;
        foreach (ComboBoxItem item in LanguageComboBox.Items)
        {
            if (item.Tag?.ToString() == currentCulture)
            {
                LanguageComboBox.SelectedItem = item;
                break;
            }
        }

        // Default to first item if not found
        if (LanguageComboBox.SelectedItem == null && LanguageComboBox.Items.Count > 0)
        {
            LanguageComboBox.SelectedIndex = 0;
        }

        // Default theme selection
        ThemeComboBox.SelectedIndex = 0;
    }

    private void UpdateLocalizedStrings()
    {
        PageTitle.Text = _localization.Get("settings.title");
        AppearanceTitle.Text = _localization.Get("settings.general");
        ThemeComboBox.Header = _localization.Get("settings.theme");
        ThemeSystem.Content = _localization.Get("settings.theme.system");
        ThemeLight.Content = _localization.Get("settings.theme.light");
        ThemeDark.Content = _localization.Get("settings.theme.dark");
        LanguageComboBox.Header = _localization.Get("settings.language");

        SyncTitle.Text = _localization.Get("settings.sync");
        AboutTitle.Text = _localization.Get("settings.about");
    }

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateLocalizedStrings);
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (LanguageComboBox.SelectedItem is ComboBoxItem item && item.Tag is string cultureName)
        {
            _localization.SetCulture(cultureName);
        }
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string themeName)
        {
            // TODO: Apply theme change
            var theme = themeName switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            if (App.MainWindow?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }
        }
    }

    private void SyncNow_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Trigger sync
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Export data
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Import data
    }

    private void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Clear cache
    }
}
