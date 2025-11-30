namespace Arcana.Plugins.Contracts.Manifest;

/// <summary>
/// Activation event types that trigger plugin loading.
/// </summary>
public static class ActivationEvents
{
    /// <summary>
    /// Plugin activates when the application starts.
    /// Use sparingly - prefer lazy activation.
    /// </summary>
    public const string OnStartup = "onStartup";

    /// <summary>
    /// Plugin activates when a specific command is executed.
    /// Format: "onCommand:commandId"
    /// Example: "onCommand:order.new"
    /// </summary>
    public const string OnCommandPrefix = "onCommand:";

    /// <summary>
    /// Plugin activates when a specific view is requested.
    /// Format: "onView:viewId"
    /// Example: "onView:OrderListPage"
    /// </summary>
    public const string OnViewPrefix = "onView:";

    /// <summary>
    /// Plugin activates when a specific language is selected.
    /// Format: "onLanguage:languageCode"
    /// Example: "onLanguage:zh-TW"
    /// </summary>
    public const string OnLanguagePrefix = "onLanguage:";

    /// <summary>
    /// Plugin activates when a file of specific type is opened.
    /// Format: "onFileType:extension"
    /// Example: "onFileType:.xlsx"
    /// </summary>
    public const string OnFileTypePrefix = "onFileType:";

    /// <summary>
    /// Plugin activates when a specific URI scheme is requested.
    /// Format: "onUri:scheme"
    /// Example: "onUri:arcana-order"
    /// </summary>
    public const string OnUriPrefix = "onUri:";

    /// <summary>
    /// Plugin activates when workspace contains specific files.
    /// Format: "onWorkspaceContains:pattern"
    /// Example: "onWorkspaceContains:**/package.json"
    /// </summary>
    public const string OnWorkspaceContainsPrefix = "onWorkspaceContains:";

    /// <summary>
    /// Plugin activates when a specific configuration is changed.
    /// Format: "onConfiguration:configKey"
    /// Example: "onConfiguration:theme.mode"
    /// </summary>
    public const string OnConfigurationPrefix = "onConfiguration:";

    /// <summary>
    /// Plugin activates when authentication state changes.
    /// Format: "onAuthentication:providerId"
    /// Example: "onAuthentication:oauth2"
    /// </summary>
    public const string OnAuthenticationPrefix = "onAuthentication:";

    /// <summary>
    /// Plugin activates when a specific menu is about to be shown.
    /// Format: "onMenu:menuId"
    /// Example: "onMenu:contextMenu.editor"
    /// </summary>
    public const string OnMenuPrefix = "onMenu:";

    /// <summary>
    /// Wildcard - plugin activates on any event (not recommended).
    /// </summary>
    public const string Star = "*";

    /// <summary>
    /// Parses an activation event string into its type and argument.
    /// </summary>
    public static (ActivationEventType Type, string? Argument) Parse(string activationEvent)
    {
        if (string.IsNullOrEmpty(activationEvent))
            return (ActivationEventType.Unknown, null);

        if (activationEvent == OnStartup)
            return (ActivationEventType.OnStartup, null);

        if (activationEvent == Star)
            return (ActivationEventType.Star, null);

        if (activationEvent.StartsWith(OnCommandPrefix))
            return (ActivationEventType.OnCommand, activationEvent[OnCommandPrefix.Length..]);

        if (activationEvent.StartsWith(OnViewPrefix))
            return (ActivationEventType.OnView, activationEvent[OnViewPrefix.Length..]);

        if (activationEvent.StartsWith(OnLanguagePrefix))
            return (ActivationEventType.OnLanguage, activationEvent[OnLanguagePrefix.Length..]);

        if (activationEvent.StartsWith(OnFileTypePrefix))
            return (ActivationEventType.OnFileType, activationEvent[OnFileTypePrefix.Length..]);

        if (activationEvent.StartsWith(OnUriPrefix))
            return (ActivationEventType.OnUri, activationEvent[OnUriPrefix.Length..]);

        if (activationEvent.StartsWith(OnWorkspaceContainsPrefix))
            return (ActivationEventType.OnWorkspaceContains, activationEvent[OnWorkspaceContainsPrefix.Length..]);

        if (activationEvent.StartsWith(OnConfigurationPrefix))
            return (ActivationEventType.OnConfiguration, activationEvent[OnConfigurationPrefix.Length..]);

        if (activationEvent.StartsWith(OnAuthenticationPrefix))
            return (ActivationEventType.OnAuthentication, activationEvent[OnAuthenticationPrefix.Length..]);

        if (activationEvent.StartsWith(OnMenuPrefix))
            return (ActivationEventType.OnMenu, activationEvent[OnMenuPrefix.Length..]);

        return (ActivationEventType.Unknown, activationEvent);
    }

    /// <summary>
    /// Creates an activation event string for a command.
    /// </summary>
    public static string ForCommand(string commandId) => $"{OnCommandPrefix}{commandId}";

    /// <summary>
    /// Creates an activation event string for a view.
    /// </summary>
    public static string ForView(string viewId) => $"{OnViewPrefix}{viewId}";

    /// <summary>
    /// Creates an activation event string for a language.
    /// </summary>
    public static string ForLanguage(string languageCode) => $"{OnLanguagePrefix}{languageCode}";

    /// <summary>
    /// Creates an activation event string for a file type.
    /// </summary>
    public static string ForFileType(string extension) => $"{OnFileTypePrefix}{extension}";
}

/// <summary>
/// Parsed activation event type.
/// </summary>
public enum ActivationEventType
{
    Unknown,
    OnStartup,
    OnCommand,
    OnView,
    OnLanguage,
    OnFileType,
    OnUri,
    OnWorkspaceContains,
    OnConfiguration,
    OnAuthentication,
    OnMenu,
    Star
}

/// <summary>
/// Service for monitoring and triggering activation events.
/// </summary>
public interface IActivationEventService
{
    /// <summary>
    /// Fires an activation event, potentially activating plugins.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <param name="argument">The event argument (e.g., command ID, view ID).</param>
    /// <returns>Task that completes when all triggered plugins are activated.</returns>
    Task FireAsync(ActivationEventType eventType, string? argument = null);

    /// <summary>
    /// Fires an activation event from a raw string.
    /// </summary>
    Task FireAsync(string activationEvent);

    /// <summary>
    /// Gets plugins that would be activated by an event.
    /// </summary>
    IReadOnlyList<string> GetPluginsForEvent(ActivationEventType eventType, string? argument = null);

    /// <summary>
    /// Checks if a plugin is waiting for activation (has pending activation events).
    /// </summary>
    bool IsPendingActivation(string pluginId);

    /// <summary>
    /// Gets all pending plugins and their activation events.
    /// </summary>
    IReadOnlyDictionary<string, IReadOnlyList<string>> GetPendingPlugins();

    /// <summary>
    /// Event raised when a plugin is activated due to an activation event.
    /// </summary>
    event EventHandler<PluginActivatedEventArgs>? PluginActivated;
}

/// <summary>
/// Event args for plugin activation.
/// </summary>
public class PluginActivatedEventArgs : EventArgs
{
    public required string PluginId { get; init; }
    public required string ActivationEvent { get; init; }
    public TimeSpan ActivationTime { get; init; }
}
