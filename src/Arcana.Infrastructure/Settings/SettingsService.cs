using System.Collections.Concurrent;
using System.Text.Json;

namespace Arcana.Infrastructure.Settings;

/// <summary>
/// Settings service implementation using JSON file storage.
/// 使用 JSON 檔案儲存的設定服務實作
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly ConcurrentDictionary<string, object?> _settings = new();
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public event EventHandler<SettingChangedEventArgs>? SettingChanged;

    public SettingsService()
    {
        _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Arcana",
            "settings.json");

        LoadSettings();
    }

    public T? Get<T>(string key, T? defaultValue = default)
    {
        if (_settings.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
            {
                return typedValue;
            }

            // Try to convert from JsonElement
            if (value is JsonElement element)
            {
                try
                {
                    return element.Deserialize<T>();
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

        return defaultValue;
    }

    public async Task SetAsync<T>(string key, T value)
    {
        var oldValue = _settings.TryGetValue(key, out var existing) ? existing : default;
        _settings[key] = value;

        await SaveSettingsAsync();

        SettingChanged?.Invoke(this, new SettingChangedEventArgs(key, oldValue, value));
    }

    public async Task RemoveAsync(string key)
    {
        if (_settings.TryRemove(key, out var oldValue))
        {
            await SaveSettingsAsync();
            SettingChanged?.Invoke(this, new SettingChangedEventArgs(key, oldValue, null));
        }
    }

    public bool Contains(string key)
    {
        return _settings.ContainsKey(key);
    }

    public IReadOnlyDictionary<string, object?> GetAll()
    {
        return new Dictionary<string, object?>(_settings);
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                if (settings != null)
                {
                    foreach (var kvp in settings)
                    {
                        _settings[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        catch
        {
            // Ignore load errors, use defaults
        }
    }

    private async Task SaveSettingsAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_settingsPath, json);
        }
        finally
        {
            _saveLock.Release();
        }
    }
}
