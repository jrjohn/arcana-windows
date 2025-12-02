using System.Globalization;
using System.Text.Json;
using Arcana.Plugin.FlowChart.Views;
using Arcana.Plugins.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Arcana.Plugin.FlowChart.TestHost;

/// <summary>
/// Main window for the FlowChart plugin test host.
/// Provides a standalone environment for testing the plugin without the main Arcana app.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly MockLocalizationService _localizationService;
    private FlowChartEditorPage? _flowChartPage;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize mock localization service
        _localizationService = new MockLocalizationService();

        // Load the FlowChart page
        LoadFlowChartPage();

        // Set default language
        LanguageComboBox.SelectedIndex = 0;
    }

    private void LoadFlowChartPage()
    {
        _flowChartPage = new FlowChartEditorPage();
        _flowChartPage.SetLocalizationService(_localizationService);
        ContentFrame.Content = _flowChartPage;
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is ComboBoxItem item && item.Tag is string cultureName)
        {
            _localizationService.SetCulture(cultureName);
        }
    }

    private void OnLoadSampleClick(object sender, RoutedEventArgs e)
    {
        _flowChartPage?.HandleNavigationParameters(new Dictionary<string, object>
        {
            ["action"] = "sample"
        });
    }

    private void OnNewDiagramClick(object sender, RoutedEventArgs e)
    {
        // Recreate the page for a new diagram
        LoadFlowChartPage();
    }
}

/// <summary>
/// Mock localization service for standalone testing.
/// Loads localization strings from the plugin's locale files.
/// </summary>
public class MockLocalizationService : ILocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _resources = new();
    private readonly List<CultureInfo> _availableCultures;
    private CultureInfo _currentCulture;

    public CultureInfo CurrentCulture => _currentCulture;
    public IReadOnlyList<CultureInfo> AvailableCultures => _availableCultures.AsReadOnly();

    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    public MockLocalizationService()
    {
        _availableCultures =
        [
            new CultureInfo("en-US"),
            new CultureInfo("zh-TW"),
            new CultureInfo("ja-JP")
        ];

        _currentCulture = _availableCultures[0];

        LoadLocalizationFiles();
    }

    private void LoadLocalizationFiles()
    {
        var localesPath = Path.Combine(AppContext.BaseDirectory, "locales");

        foreach (var culture in _availableCultures)
        {
            var filePath = Path.Combine(localesPath, $"{culture.Name}.json");
            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var resources = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (resources != null)
                    {
                        _resources[culture.Name] = resources;
                        System.Diagnostics.Debug.WriteLine($"[TestHost] Loaded {resources.Count} strings for {culture.Name}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TestHost] Failed to load locales for {culture.Name}: {ex.Message}");
                    _resources[culture.Name] = new Dictionary<string, string>();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[TestHost] Locale file not found: {filePath}");
                _resources[culture.Name] = new Dictionary<string, string>();
            }
        }
    }

    public string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;

        if (_resources.TryGetValue(_currentCulture.Name, out var resources) &&
            resources.TryGetValue(key, out var value))
        {
            return value;
        }

        // Fallback to English
        if (_currentCulture.Name != "en-US" &&
            _resources.TryGetValue("en-US", out var enResources) &&
            enResources.TryGetValue(key, out var enValue))
        {
            return enValue;
        }

        return key;
    }

    public string Get(string key, params object[] args)
    {
        var template = Get(key);
        try
        {
            return string.Format(template, args);
        }
        catch
        {
            return template;
        }
    }

    public string GetForPlugin(string pluginId, string key) => Get(key);

    public string GetForPlugin(string pluginId, string key, params object[] args) => Get(key, args);

    public string GetFromAnyPlugin(string key) => Get(key);

    public void SetCulture(string cultureName)
    {
        var newCulture = _availableCultures.FirstOrDefault(c => c.Name == cultureName);
        if (newCulture == null || _currentCulture.Name == newCulture.Name) return;

        var oldCulture = _currentCulture;
        _currentCulture = newCulture;

        CultureInfo.CurrentCulture = newCulture;
        CultureInfo.CurrentUICulture = newCulture;

        CultureChanged?.Invoke(this, new CultureChangedEventArgs(oldCulture, newCulture));
    }

    public void RegisterPluginResources(string pluginId, string cultureName, IDictionary<string, string> resources)
    {
        // Merge plugin resources
        if (!_resources.ContainsKey(cultureName))
        {
            _resources[cultureName] = new Dictionary<string, string>();
        }

        foreach (var kvp in resources)
        {
            _resources[cultureName][kvp.Key] = kvp.Value;
        }
    }
}
