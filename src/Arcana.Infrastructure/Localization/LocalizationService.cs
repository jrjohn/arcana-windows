using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Arcana.Plugins.Contracts;
using Microsoft.Extensions.Logging;

namespace Arcana.Infrastructure.Localization;

/// <summary>
/// Localization service implementation.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ILogger<LocalizationService> _logger;
    private readonly string _resourcesPath;
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _coreResources = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Dictionary<string, string>>> _pluginResources = new();
    private readonly List<CultureInfo> _availableCultures;
    private CultureInfo _currentCulture;

    public CultureInfo CurrentCulture => _currentCulture;
    public IReadOnlyList<CultureInfo> AvailableCultures => _availableCultures.AsReadOnly();

    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    public LocalizationService(ILogger<LocalizationService> logger, string? resourcesPath = null)
    {
        _logger = logger;
        _resourcesPath = resourcesPath ?? Path.Combine(AppContext.BaseDirectory, "Resources", "Strings");

        // Default available cultures (en-US first as fallback default)
        _availableCultures =
        [
            new CultureInfo("en-US"),
            new CultureInfo("zh-TW"),
            new CultureInfo("ja-JP")
        ];

        // Set default culture based on system, fallback to English
        var systemCulture = CultureInfo.CurrentUICulture;
        _currentCulture = _availableCultures.FirstOrDefault(c =>
            c.TwoLetterISOLanguageName == systemCulture.TwoLetterISOLanguageName)
            ?? _availableCultures[0]; // en-US as default

        LoadCoreResources();
    }

    public string Get(string key)
    {
        return GetString(_currentCulture.Name, key) ?? key;
    }

    public string Get(string key, params object[] args)
    {
        var template = Get(key);
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    public string GetForPlugin(string pluginId, string key)
    {
        return GetPluginString(pluginId, _currentCulture.Name, key) ?? Get(key);
    }

    public string GetForPlugin(string pluginId, string key, params object[] args)
    {
        var template = GetForPlugin(pluginId, key);
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    public void SetCulture(string cultureName)
    {
        var newCulture = _availableCultures.FirstOrDefault(c => c.Name == cultureName);
        if (newCulture == null)
        {
            _logger.LogWarning("Culture not available: {CultureName}", cultureName);
            return;
        }

        if (_currentCulture.Name == newCulture.Name)
        {
            return;
        }

        var oldCulture = _currentCulture;
        _currentCulture = newCulture;

        // Update thread culture
        CultureInfo.CurrentCulture = newCulture;
        CultureInfo.CurrentUICulture = newCulture;

        _logger.LogInformation("Culture changed from {OldCulture} to {NewCulture}",
            oldCulture.Name, newCulture.Name);

        CultureChanged?.Invoke(this, new CultureChangedEventArgs(oldCulture, newCulture));
    }

    public void RegisterPluginResources(string pluginId, string cultureName, IDictionary<string, string> resources)
    {
        var pluginDict = _pluginResources.GetOrAdd(pluginId, _ => new ConcurrentDictionary<string, Dictionary<string, string>>());
        pluginDict[cultureName] = new Dictionary<string, string>(resources);
        _logger.LogDebug("Registered {Count} resources for plugin {PluginId} culture {Culture}",
            resources.Count, pluginId, cultureName);
    }

    public string GetFromAnyPlugin(string key)
    {
        // First try core resources
        var coreResult = GetString(_currentCulture.Name, key);
        if (coreResult != null)
        {
            return coreResult;
        }

        // Then search all plugin resources
        foreach (var pluginEntry in _pluginResources)
        {
            var result = GetPluginString(pluginEntry.Key, _currentCulture.Name, key);
            if (result != null)
            {
                return result;
            }
        }

        return key;
    }

    private void LoadCoreResources()
    {
        foreach (var culture in _availableCultures)
        {
            var filePath = Path.Combine(_resourcesPath, $"{culture.Name}.json");
            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    var resources = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (resources != null)
                    {
                        _coreResources[culture.Name] = resources;
                        _logger.LogDebug("Loaded {Count} core resources for culture {Culture}",
                            resources.Count, culture.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load resources for culture {Culture}", culture.Name);
                }
            }
            else
            {
                _coreResources[culture.Name] = new Dictionary<string, string>();
            }
        }
    }

    private const string FallbackCulture = "en-US";

    private string? GetString(string cultureName, string key)
    {
        // Try exact culture
        if (_coreResources.TryGetValue(cultureName, out var resources) &&
            resources.TryGetValue(key, out var value))
        {
            return value;
        }

        // Try fallback to English
        if (cultureName != FallbackCulture &&
            _coreResources.TryGetValue(FallbackCulture, out var fallbackResources) &&
            fallbackResources.TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }

        return null;
    }

    private string? GetPluginString(string pluginId, string cultureName, string key)
    {
        if (_pluginResources.TryGetValue(pluginId, out var pluginDict))
        {
            // Try exact culture
            if (pluginDict.TryGetValue(cultureName, out var resources) &&
                resources.TryGetValue(key, out var value))
            {
                return value;
            }

            // Try fallback to English
            if (cultureName != FallbackCulture &&
                pluginDict.TryGetValue(FallbackCulture, out var fallbackResources) &&
                fallbackResources.TryGetValue(key, out var fallbackValue))
            {
                return fallbackValue;
            }
        }

        return null;
    }
}
