namespace Arcana.Infrastructure.Settings;

/// <summary>
/// Settings service interface.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets a setting value.
    /// </summary>
    T? Get<T>(string key, T? defaultValue = default);

    /// <summary>
    /// Sets a setting value.
    /// </summary>
    Task SetAsync<T>(string key, T value);

    /// <summary>
    /// Removes a setting.
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Checks if a setting exists.
    /// </summary>
    bool Contains(string key);

    /// <summary>
    /// Gets all settings.
    /// </summary>
    IReadOnlyDictionary<string, object?> GetAll();

    /// <summary>
    /// Event raised when a setting changes.
    /// </summary>
    event EventHandler<SettingChangedEventArgs>? SettingChanged;
}

/// <summary>
/// Event args for setting changes.
/// </summary>
public class SettingChangedEventArgs : EventArgs
{
    public string Key { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }

    public SettingChangedEventArgs(string key, object? oldValue, object? newValue)
    {
        Key = key;
        OldValue = oldValue;
        NewValue = newValue;
    }
}

/// <summary>
/// Application settings keys.
/// </summary>
public static class SettingKeys
{
    public const string Theme = "app.theme";
    public const string Language = "app.language";
    public const string SyncEnabled = "sync.enabled";
    public const string SyncInterval = "sync.interval";
    public const string LastSyncTime = "sync.lastTime";
    public const string WindowWidth = "window.width";
    public const string WindowHeight = "window.height";
    public const string WindowX = "window.x";
    public const string WindowY = "window.y";
    public const string IsMaximized = "window.maximized";
    public const string SidebarWidth = "sidebar.width";
    public const string RecentFiles = "recent.files";
}
