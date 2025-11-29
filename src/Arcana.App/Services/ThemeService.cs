using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Arcana.App.Services;

/// <summary>
/// Theme definition.
/// </summary>
public class ThemeDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string NameKey { get; init; }
    public required ElementTheme BaseTheme { get; init; }
    public required Color AccentColor { get; init; }
    public required Color BackgroundColor { get; init; }
    public required Color PaneBackgroundColor { get; init; }
    public required Color TextPrimaryColor { get; init; }
    public required Color TextSecondaryColor { get; init; }
    public Color? GradientStartColor { get; init; }
    public Color? GradientEndColor { get; init; }
}

/// <summary>
/// Theme service for managing application themes.
/// </summary>
public class ThemeService
{
    private static readonly List<ThemeDefinition> _themes =
    [
        new ThemeDefinition
        {
            Id = "System",
            Name = "System Default",
            NameKey = "settings.theme.system",
            BaseTheme = ElementTheme.Default,
            AccentColor = Color.FromArgb(255, 0, 120, 212),
            BackgroundColor = Color.FromArgb(255, 249, 249, 249),
            PaneBackgroundColor = Color.FromArgb(255, 243, 243, 243),
            TextPrimaryColor = Color.FromArgb(255, 0, 0, 0),
            TextSecondaryColor = Color.FromArgb(255, 96, 96, 96)
        },
        new ThemeDefinition
        {
            Id = "Light",
            Name = "Light",
            NameKey = "settings.theme.light",
            BaseTheme = ElementTheme.Light,
            AccentColor = Color.FromArgb(255, 0, 120, 212),
            BackgroundColor = Color.FromArgb(255, 255, 255, 255),
            PaneBackgroundColor = Color.FromArgb(255, 243, 243, 243),
            TextPrimaryColor = Color.FromArgb(255, 0, 0, 0),
            TextSecondaryColor = Color.FromArgb(255, 96, 96, 96)
        },
        new ThemeDefinition
        {
            Id = "Dark",
            Name = "Dark",
            NameKey = "settings.theme.dark",
            BaseTheme = ElementTheme.Dark,
            AccentColor = Color.FromArgb(255, 96, 205, 255),
            BackgroundColor = Color.FromArgb(255, 32, 32, 32),
            PaneBackgroundColor = Color.FromArgb(255, 40, 40, 40),
            TextPrimaryColor = Color.FromArgb(255, 255, 255, 255),
            TextSecondaryColor = Color.FromArgb(255, 180, 180, 180)
        },
        new ThemeDefinition
        {
            Id = "OceanBlue",
            Name = "Ocean Blue",
            NameKey = "settings.theme.oceanblue",
            BaseTheme = ElementTheme.Light,
            AccentColor = Color.FromArgb(255, 0, 120, 212),
            BackgroundColor = Color.FromArgb(255, 240, 248, 255),
            PaneBackgroundColor = Color.FromArgb(255, 220, 238, 251),
            TextPrimaryColor = Color.FromArgb(255, 26, 58, 92),
            TextSecondaryColor = Color.FromArgb(255, 74, 106, 140),
            GradientStartColor = Color.FromArgb(255, 0, 120, 212),
            GradientEndColor = Color.FromArgb(255, 0, 188, 242)
        },
        new ThemeDefinition
        {
            Id = "ForestGreen",
            Name = "Forest Green",
            NameKey = "settings.theme.forestgreen",
            BaseTheme = ElementTheme.Light,
            AccentColor = Color.FromArgb(255, 46, 125, 50),
            BackgroundColor = Color.FromArgb(255, 241, 248, 233),
            PaneBackgroundColor = Color.FromArgb(255, 220, 237, 200),
            TextPrimaryColor = Color.FromArgb(255, 27, 61, 31),
            TextSecondaryColor = Color.FromArgb(255, 62, 107, 67),
            GradientStartColor = Color.FromArgb(255, 46, 125, 50),
            GradientEndColor = Color.FromArgb(255, 129, 199, 132)
        },
        new ThemeDefinition
        {
            Id = "PurpleNight",
            Name = "Purple Night",
            NameKey = "settings.theme.purplenight",
            BaseTheme = ElementTheme.Dark,
            AccentColor = Color.FromArgb(255, 156, 39, 176),
            BackgroundColor = Color.FromArgb(255, 26, 26, 46),
            PaneBackgroundColor = Color.FromArgb(255, 15, 15, 26),
            TextPrimaryColor = Color.FromArgb(255, 232, 232, 240),
            TextSecondaryColor = Color.FromArgb(255, 168, 168, 192),
            GradientStartColor = Color.FromArgb(255, 156, 39, 176),
            GradientEndColor = Color.FromArgb(255, 224, 64, 251)
        },
        new ThemeDefinition
        {
            Id = "SunsetOrange",
            Name = "Sunset Orange",
            NameKey = "settings.theme.sunsetorange",
            BaseTheme = ElementTheme.Light,
            AccentColor = Color.FromArgb(255, 255, 107, 53),
            BackgroundColor = Color.FromArgb(255, 255, 248, 240),
            PaneBackgroundColor = Color.FromArgb(255, 255, 224, 178),
            TextPrimaryColor = Color.FromArgb(255, 74, 44, 23),
            TextSecondaryColor = Color.FromArgb(255, 122, 92, 71),
            GradientStartColor = Color.FromArgb(255, 255, 107, 53),
            GradientEndColor = Color.FromArgb(255, 255, 217, 61)
        },
        new ThemeDefinition
        {
            Id = "RosePink",
            Name = "Rose Pink",
            NameKey = "settings.theme.rosepink",
            BaseTheme = ElementTheme.Light,
            AccentColor = Color.FromArgb(255, 233, 30, 99),
            BackgroundColor = Color.FromArgb(255, 255, 240, 245),
            PaneBackgroundColor = Color.FromArgb(255, 248, 187, 217),
            TextPrimaryColor = Color.FromArgb(255, 74, 28, 50),
            TextSecondaryColor = Color.FromArgb(255, 122, 76, 98),
            GradientStartColor = Color.FromArgb(255, 233, 30, 99),
            GradientEndColor = Color.FromArgb(255, 255, 128, 171)
        },
        new ThemeDefinition
        {
            Id = "MidnightBlue",
            Name = "Midnight Blue",
            NameKey = "settings.theme.midnightblue",
            BaseTheme = ElementTheme.Dark,
            AccentColor = Color.FromArgb(255, 0, 188, 212),
            BackgroundColor = Color.FromArgb(255, 13, 27, 42),
            PaneBackgroundColor = Color.FromArgb(255, 10, 22, 40),
            TextPrimaryColor = Color.FromArgb(255, 224, 247, 250),
            TextSecondaryColor = Color.FromArgb(255, 128, 222, 234),
            GradientStartColor = Color.FromArgb(255, 13, 27, 42),
            GradientEndColor = Color.FromArgb(255, 0, 188, 212)
        }
    ];

    private ThemeDefinition _currentTheme;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public ThemeService()
    {
        _currentTheme = _themes[0]; // Default to System
    }

    public IReadOnlyList<ThemeDefinition> AvailableThemes => _themes.AsReadOnly();

    public ThemeDefinition CurrentTheme => _currentTheme;

    public void ApplyTheme(string themeId, FrameworkElement rootElement)
    {
        var theme = _themes.FirstOrDefault(t => t.Id == themeId) ?? _themes[0];
        var oldTheme = _currentTheme;
        _currentTheme = theme;

        // Clear any previous custom theme overrides first
        ClearCustomThemeResources(rootElement);

        // Apply base theme (Light/Dark)
        rootElement.RequestedTheme = theme.BaseTheme;

        // Apply custom colors for non-standard themes
        if (theme.Id != "System" && theme.Id != "Light" && theme.Id != "Dark")
        {
            ApplyCustomColors(rootElement, theme);
        }

        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme));
    }

    private static readonly string[] ThemeResourceKeys =
    [
        "ApplicationPageBackgroundThemeBrush",
        "NavigationViewDefaultPaneBackground",
        "NavigationViewExpandedPaneBackground",
        "SystemAccentColor",
        "SystemAccentColorLight1",
        "SystemAccentColorDark1",
        "TextFillColorPrimaryBrush",
        "TextFillColorSecondaryBrush",
        "CardBackgroundFillColorDefaultBrush",
        "ControlFillColorDefaultBrush",
        "SubtleFillColorSecondaryBrush",
        "ThemeAccentBrush",
        "ThemeBackgroundBrush",
        "ThemePaneBackgroundBrush",
        "ThemeTextPrimaryBrush",
        "ThemeTextSecondaryBrush",
        "ThemeHeaderGradientBrush"
    ];

    private static void ClearCustomThemeResources(FrameworkElement rootElement)
    {
        // Clear from root element resources
        var resources = rootElement.Resources;
        foreach (var key in ThemeResourceKeys)
        {
            if (resources.ContainsKey(key))
            {
                resources.Remove(key);
            }
        }

        // Also clear from Application resources
        var appResources = Application.Current.Resources;
        foreach (var key in ThemeResourceKeys)
        {
            if (appResources.ContainsKey(key))
            {
                appResources.Remove(key);
            }
        }

        // Reset root panel background to use theme resource
        if (rootElement is Panel panel)
        {
            panel.ClearValue(Panel.BackgroundProperty);
        }
    }

    private static void ApplyCustomColors(FrameworkElement rootElement, ThemeDefinition theme)
    {
        var resources = rootElement.Resources;
        var appResources = Application.Current.Resources;

        // Override system theme resources with custom colors
        var backgroundBrush = new SolidColorBrush(theme.BackgroundColor);
        var paneBrush = new SolidColorBrush(theme.PaneBackgroundColor);
        var accentBrush = new SolidColorBrush(theme.AccentColor);
        var textPrimaryBrush = new SolidColorBrush(theme.TextPrimaryColor);
        var textSecondaryBrush = new SolidColorBrush(theme.TextSecondaryColor);

        // Override WinUI system brushes - apply to both root element and Application resources
        resources["ApplicationPageBackgroundThemeBrush"] = backgroundBrush;
        resources["NavigationViewDefaultPaneBackground"] = paneBrush;
        resources["NavigationViewExpandedPaneBackground"] = paneBrush;
        resources["SystemAccentColor"] = theme.AccentColor;
        resources["SystemAccentColorLight1"] = theme.AccentColor;
        resources["SystemAccentColorDark1"] = theme.AccentColor;

        appResources["ApplicationPageBackgroundThemeBrush"] = backgroundBrush;
        appResources["NavigationViewDefaultPaneBackground"] = paneBrush;
        appResources["NavigationViewExpandedPaneBackground"] = paneBrush;
        appResources["SystemAccentColor"] = theme.AccentColor;
        appResources["SystemAccentColorLight1"] = theme.AccentColor;
        appResources["SystemAccentColorDark1"] = theme.AccentColor;

        // Text colors
        resources["TextFillColorPrimaryBrush"] = textPrimaryBrush;
        resources["TextFillColorSecondaryBrush"] = textSecondaryBrush;
        appResources["TextFillColorPrimaryBrush"] = textPrimaryBrush;
        appResources["TextFillColorSecondaryBrush"] = textSecondaryBrush;

        // Card and control backgrounds
        resources["CardBackgroundFillColorDefaultBrush"] = new SolidColorBrush(theme.BackgroundColor);
        resources["ControlFillColorDefaultBrush"] = paneBrush;
        resources["SubtleFillColorSecondaryBrush"] = paneBrush;
        appResources["CardBackgroundFillColorDefaultBrush"] = new SolidColorBrush(theme.BackgroundColor);
        appResources["ControlFillColorDefaultBrush"] = paneBrush;
        appResources["SubtleFillColorSecondaryBrush"] = paneBrush;

        // Custom theme brushes
        resources["ThemeAccentBrush"] = accentBrush;
        resources["ThemeBackgroundBrush"] = backgroundBrush;
        resources["ThemePaneBackgroundBrush"] = paneBrush;
        resources["ThemeTextPrimaryBrush"] = textPrimaryBrush;
        resources["ThemeTextSecondaryBrush"] = textSecondaryBrush;
        appResources["ThemeAccentBrush"] = accentBrush;
        appResources["ThemeBackgroundBrush"] = backgroundBrush;
        appResources["ThemePaneBackgroundBrush"] = paneBrush;
        appResources["ThemeTextPrimaryBrush"] = textPrimaryBrush;
        appResources["ThemeTextSecondaryBrush"] = textSecondaryBrush;

        // Apply to root element directly if it's a Panel (Grid, StackPanel, etc.)
        if (rootElement is Panel panel)
        {
            panel.Background = backgroundBrush;
        }

        // Apply gradient if available
        if (theme.GradientStartColor.HasValue && theme.GradientEndColor.HasValue)
        {
            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };
            gradientBrush.GradientStops.Add(new GradientStop { Color = theme.GradientStartColor.Value, Offset = 0 });
            gradientBrush.GradientStops.Add(new GradientStop { Color = theme.GradientEndColor.Value, Offset = 1 });
            resources["ThemeHeaderGradientBrush"] = gradientBrush;
            appResources["ThemeHeaderGradientBrush"] = gradientBrush;
        }
    }
}

public class ThemeChangedEventArgs : EventArgs
{
    public ThemeDefinition OldTheme { get; }
    public ThemeDefinition NewTheme { get; }

    public ThemeChangedEventArgs(ThemeDefinition oldTheme, ThemeDefinition newTheme)
    {
        OldTheme = oldTheme;
        NewTheme = newTheme;
    }
}
