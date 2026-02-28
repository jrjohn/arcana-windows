using System.Globalization;

namespace Arcana.Plugins.Contracts;

/// <summary>
/// Localization service interface for i18n support.
/// </summary>
public interface LocalizationService
{
    /// <summary>
    /// Current culture.
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Available cultures.
    /// </summary>
    IReadOnlyList<CultureInfo> AvailableCultures { get; }

    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string, or the key if not found.</returns>
    string Get(string key);

    /// <summary>
    /// Gets a localized string by key with format arguments.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The formatted localized string.</returns>
    string Get(string key, params object[] args);

    /// <summary>
    /// Gets a localized string for a specific plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string.</returns>
    string GetForPlugin(string pluginId, string key);

    /// <summary>
    /// Gets a localized string for a specific plugin with format arguments.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="key">The resource key.</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The formatted localized string.</returns>
    string GetForPlugin(string pluginId, string key, params object[] args);

    /// <summary>
    /// Changes the current culture.
    /// </summary>
    /// <param name="cultureName">Culture name (e.g., "zh-TW", "en-US").</param>
    void SetCulture(string cultureName);

    /// <summary>
    /// Registers resources for a plugin.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="cultureName">Culture name.</param>
    /// <param name="resources">Dictionary of key-value pairs.</param>
    void RegisterPluginResources(string pluginId, string cultureName, IDictionary<string, string> resources);

    /// <summary>
    /// Gets a localized string by searching all plugin resources.
    /// Falls back to core resources if not found in any plugin.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The localized string, or the key if not found.</returns>
    string GetFromAnyPlugin(string key);

    /// <summary>
    /// Event raised when culture changes.
    /// </summary>
    event EventHandler<CultureChangedEventArgs>? CultureChanged;
}

/// <summary>
/// Event arguments for culture change events.
/// </summary>
public class CultureChangedEventArgs : EventArgs
{
    public CultureInfo OldCulture { get; }
    public CultureInfo NewCulture { get; }

    public CultureChangedEventArgs(CultureInfo oldCulture, CultureInfo newCulture)
    {
        OldCulture = oldCulture;
        NewCulture = newCulture;
    }
}
