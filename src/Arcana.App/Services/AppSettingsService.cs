using System.Diagnostics;
using System.Text.Json;

namespace Arcana.App.Services;

/// <summary>
/// Application settings model.
/// 應用程式設定模型
/// </summary>
public class AppSettings
{
    public string ThemeId { get; set; } = "System";
    public string LanguageCode { get; set; } = "zh-TW";
    public bool AutoSyncEnabled { get; set; } = true;
    public int SyncFrequencyMinutes { get; set; } = 5;
}

/// <summary>
/// Application settings service for persisting user preferences.
/// 應用程式設定服務，用於保存使用者偏好設定
/// </summary>
public class AppSettingsService
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Arcana",
        "settings.json");

    private AppSettings _settings;
    private bool _isInitialized;

    public event EventHandler<AppSettings>? SettingsChanged;

    public AppSettingsService()
    {
        _settings = new AppSettings();
    }

    private void EnsureInitialized()
    {
        if (_isInitialized) return;

        _settings = LoadSettings();
        _isInitialized = true;
    }

    public AppSettings Settings
    {
        get
        {
            EnsureInitialized();
            return _settings;
        }
    }

    public string ThemeId
    {
        get
        {
            EnsureInitialized();
            return _settings.ThemeId;
        }
        set
        {
            EnsureInitialized();
            if (_settings.ThemeId != value)
            {
                _settings.ThemeId = value;
                SaveSettings();
            }
        }
    }

    public string LanguageCode
    {
        get
        {
            EnsureInitialized();
            return _settings.LanguageCode;
        }
        set
        {
            EnsureInitialized();
            if (_settings.LanguageCode != value)
            {
                _settings.LanguageCode = value;
                SaveSettings();
            }
        }
    }

    public bool AutoSyncEnabled
    {
        get
        {
            EnsureInitialized();
            return _settings.AutoSyncEnabled;
        }
        set
        {
            EnsureInitialized();
            if (_settings.AutoSyncEnabled != value)
            {
                _settings.AutoSyncEnabled = value;
                SaveSettings();
            }
        }
    }

    public int SyncFrequencyMinutes
    {
        get
        {
            EnsureInitialized();
            return _settings.SyncFrequencyMinutes;
        }
        set
        {
            EnsureInitialized();
            if (_settings.SyncFrequencyMinutes != value)
            {
                _settings.SyncFrequencyMinutes = value;
                SaveSettings();
            }
        }
    }

    private static AppSettings LoadSettings()
    {
        try
        {
            Debug.WriteLine($"[AppSettingsService] Loading settings from: {SettingsFilePath}");

            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                Debug.WriteLine($"[AppSettingsService] Settings JSON: {json}");

                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    Debug.WriteLine($"[AppSettingsService] Loaded - Theme: {settings.ThemeId}, Language: {settings.LanguageCode}");
                    return settings;
                }
            }
            else
            {
                Debug.WriteLine("[AppSettingsService] Settings file does not exist, using defaults");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppSettingsService] Error loading settings: {ex.Message}");
        }
        return new AppSettings();
    }

    private void SaveSettings()
    {
        try
        {
            Debug.WriteLine($"[AppSettingsService] Saving settings - Theme: {_settings.ThemeId}, Language: {_settings.LanguageCode}");

            // Ensure directory exists
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Debug.WriteLine($"[AppSettingsService] Created directory: {directory}");
            }

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(SettingsFilePath, json);
            Debug.WriteLine($"[AppSettingsService] Settings saved to: {SettingsFilePath}");

            SettingsChanged?.Invoke(this, _settings);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AppSettingsService] Error saving settings: {ex.Message}");
        }
    }
}
