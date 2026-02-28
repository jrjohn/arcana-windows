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
    private readonly LocalizationService _localization;
    private readonly ThemeService _themeService;
    private readonly AppSettingsService _settingsService;
    private bool _isInitializing = true;

    public List<ThemeItem> ThemeItems { get; } = [];

    public SettingsPage()
    {
        this.InitializeComponent();
        _localization = App.Services.GetRequiredService<LocalizationService>();
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
            "OceanBlue" => _localization.Get("settings.theme.oceanblue"),
            "ForestGreen" => _localization.Get("settings.theme.forestgreen"),
            "PurpleNight" => _localization.Get("settings.theme.purplenight"),
            "SunsetOrange" => _localization.Get("settings.theme.sunsetorange"),
            "RosePink" => _localization.Get("settings.theme.rosepink"),
            "MidnightBlue" => _localization.Get("settings.theme.midnightblue"),
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
        // Page title
        PageTitle.Text = _localization.Get("settings.title");

        // Appearance section
        AppearanceTitle.Text = _localization.Get("settings.appearance");
        ThemeLabel.Text = _localization.Get("settings.theme");
        LanguageComboBox.Header = _localization.Get("settings.language");

        // Language items
        LangZhTW.Content = "繁體中文";
        LangJaJP.Content = "日本語";

        // Sync section
        SyncTitle.Text = _localization.Get("settings.syncSettings");
        AutoSyncToggle.Header = _localization.Get("settings.autoSync");
        AutoSyncToggle.OnContent = _localization.Get("settings.on");
        AutoSyncToggle.OffContent = _localization.Get("settings.off");
        SyncFrequencyComboBox.Header = _localization.Get("settings.syncFrequency");
        Freq5Min.Content = _localization.Get("settings.every5min");
        Freq15Min.Content = _localization.Get("settings.every15min");
        Freq30Min.Content = _localization.Get("settings.every30min");
        FreqHour.Content = _localization.Get("settings.everyHour");
        SyncNowButton.Content = _localization.Get("settings.syncNow");

        // Data section
        DataTitle.Text = _localization.Get("settings.dataSettings");
        ExportButton.Content = _localization.Get("settings.export");
        ImportButton.Content = _localization.Get("settings.import");
        ClearCacheButton.Content = _localization.Get("settings.clearCache");

        // About section
        AboutTitle.Text = _localization.Get("settings.about");
        AppName.Text = _localization.Get("settings.appName");
        AppVersion.Text = _localization.Get("settings.versionFormat", "1.0.0");

        // Update theme display names
        for (int i = 0; i < ThemeItems.Count && i < _themeService.AvailableThemes.Count; i++)
        {
            ThemeItems[i].DisplayName = GetThemeDisplayName(_themeService.AvailableThemes[i]);
        }

        // Refresh theme grid to show updated names
        ThemeGridView.ItemsSource = null;
        ThemeGridView.ItemsSource = ThemeItems;

        // Re-select the current theme
        var savedThemeId = _settingsService.ThemeId;
        for (int i = 0; i < ThemeItems.Count; i++)
        {
            if (ThemeItems[i].Id == savedThemeId)
            {
                ThemeGridView.SelectedIndex = i;
                break;
            }
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

    private void AutoSyncToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _settingsService.AutoSyncEnabled = AutoSyncToggle.IsOn;
    }

    private void SyncFrequencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        var minutes = SyncFrequencyComboBox.SelectedIndex switch
        {
            0 => 5,
            1 => 15,
            2 => 30,
            3 => 60,
            _ => 5
        };
        _settingsService.SyncFrequencyMinutes = minutes;
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
