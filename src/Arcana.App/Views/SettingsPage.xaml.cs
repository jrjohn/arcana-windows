using Arcana.App.Services;
using Arcana.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace Arcana.App.Views;

/// <summary>
/// Theme item for display in GridView.
/// </summary>
public class ThemeItem
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Color AccentColor { get; set; }
    public Color BackgroundColor { get; set; }
    public Color PaneColor { get; set; }
    public Color TextColor { get; set; }
}

/// <summary>
/// Settings page.
/// </summary>
public sealed partial class SettingsPage : Page
{
    private readonly ILocalizationService _localization;
    private readonly ThemeService _themeService;
    private readonly AppSettingsService _settingsService;
    private bool _isInitializing = true;

    public List<ThemeItem> ThemeItems { get; } = [];

    public SettingsPage()
    {
        this.InitializeComponent();
        _localization = App.Services.GetRequiredService<ILocalizationService>();
        _themeService = App.Services.GetRequiredService<ThemeService>();
        _settingsService = App.Services.GetRequiredService<AppSettingsService>();

        // Subscribe to culture changes
        _localization.CultureChanged += OnCultureChanged;

        // Initialize theme items
        InitializeThemeItems();

        // Initialize UI
        InitializeLanguageSelection();
        InitializeThemeSelection();
        InitializeSyncSettings();
        UpdateLocalizedStrings();

        _isInitializing = false;
    }

    private void InitializeThemeItems()
    {
        foreach (var theme in _themeService.AvailableThemes)
        {
            ThemeItems.Add(new ThemeItem
            {
                Id = theme.Id,
                DisplayName = GetThemeDisplayName(theme),
                AccentColor = theme.AccentColor,
                BackgroundColor = theme.BackgroundColor,
                PaneColor = theme.PaneBackgroundColor,
                TextColor = theme.TextPrimaryColor
            });
        }
    }

    private string GetThemeDisplayName(ThemeDefinition theme)
    {
        return theme.Id switch
        {
            "System" => _localization.Get("settings.theme.system"),
            "Light" => _localization.Get("settings.theme.light"),
            "Dark" => _localization.Get("settings.theme.dark"),
            "OceanBlue" => "Ocean Blue",
            "ForestGreen" => "Forest Green",
            "PurpleNight" => "Purple Night",
            "SunsetOrange" => "Sunset Orange",
            "RosePink" => "Rose Pink",
            "MidnightBlue" => "Midnight Blue",
            _ => theme.Name
        };
    }

    private void InitializeLanguageSelection()
    {
        // Select saved language
        var currentLanguage = _settingsService.LanguageCode;
        foreach (ComboBoxItem item in LanguageComboBox.Items)
        {
            if (item.Tag?.ToString() == currentLanguage)
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
    }

    private void InitializeThemeSelection()
    {
        // Select saved theme
        var savedThemeId = _settingsService.ThemeId;
        for (int i = 0; i < ThemeItems.Count; i++)
        {
            if (ThemeItems[i].Id == savedThemeId)
            {
                ThemeGridView.SelectedIndex = i;
                break;
            }
        }

        // Default to first item if not found
        if (ThemeGridView.SelectedItem == null && ThemeItems.Count > 0)
        {
            ThemeGridView.SelectedIndex = 0;
        }
    }

    private void InitializeSyncSettings()
    {
        AutoSyncToggle.IsOn = _settingsService.AutoSyncEnabled;

        var frequencyMinutes = _settingsService.SyncFrequencyMinutes;
        SyncFrequencyComboBox.SelectedIndex = frequencyMinutes switch
        {
            5 => 0,
            15 => 1,
            30 => 2,
            60 => 3,
            _ => 0
        };
    }

    private void UpdateLocalizedStrings()
    {
        PageTitle.Text = _localization.Get("settings.title");
        AppearanceTitle.Text = _localization.Get("settings.general");
        ThemeLabel.Text = _localization.Get("settings.theme");
        LanguageComboBox.Header = _localization.Get("settings.language");

        SyncTitle.Text = _localization.Get("settings.sync");
        AboutTitle.Text = _localization.Get("settings.about");

        // Update theme display names
        for (int i = 0; i < ThemeItems.Count && i < _themeService.AvailableThemes.Count; i++)
        {
            ThemeItems[i].DisplayName = GetThemeDisplayName(_themeService.AvailableThemes[i]);
        }
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
            _settingsService.LanguageCode = cultureName;
        }
    }

    private void ThemeGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (ThemeGridView.SelectedItem is ThemeItem themeItem)
        {
            if (App.MainWindow?.Content is FrameworkElement rootElement)
            {
                _themeService.ApplyTheme(themeItem.Id, rootElement);
                _settingsService.ThemeId = themeItem.Id;
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
