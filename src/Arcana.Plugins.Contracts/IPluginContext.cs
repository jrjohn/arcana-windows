using Microsoft.Extensions.Logging;

namespace Arcana.Plugins.Contracts;

/// <summary>
/// Plugin context interface providing access to host services.
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Plugin ID.
    /// </summary>
    string PluginId { get; }

    /// <summary>
    /// Plugin installation path.
    /// </summary>
    string PluginPath { get; }

    /// <summary>
    /// Plugin data storage path.
    /// </summary>
    string DataPath { get; }

    /// <summary>
    /// Logger for this plugin.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Access to the command service.
    /// </summary>
    CommandService Commands { get; }

    /// <summary>
    /// Access to the window service.
    /// </summary>
    WindowService Window { get; }

    /// <summary>
    /// Access to the message bus for plugin-to-plugin communication.
    /// </summary>
    IMessageBus MessageBus { get; }

    /// <summary>
    /// Access to the event aggregator for application events.
    /// </summary>
    IEventAggregator Events { get; }

    /// <summary>
    /// Access to the shared state store.
    /// </summary>
    ISharedStateStore SharedState { get; }

    /// <summary>
    /// Access to the menu contribution registry.
    /// </summary>
    IMenuRegistry Menus { get; }

    /// <summary>
    /// Access to the view contribution registry.
    /// </summary>
    IViewRegistry Views { get; }

    /// <summary>
    /// Access to the navigation service (low-level).
    /// </summary>
    INavigationService Navigation { get; }

    /// <summary>
    /// Access to the navigation graph (high-level, type-safe).
    /// Plugins can wrap this in their own NavGraph class for stronger typing.
    /// </summary>
    INavGraph NavGraph { get; }

    /// <summary>
    /// Access to the localization service.
    /// </summary>
    LocalizationService Localization { get; }

    /// <summary>
    /// Disposables that will be cleaned up when the plugin is deactivated.
    /// </summary>
    IList<IDisposable> Subscriptions { get; }

    /// <summary>
    /// Gets a service from the DI container.
    /// </summary>
    T? GetService<T>() where T : class;

    /// <summary>
    /// Gets a required service from the DI container.
    /// </summary>
    T GetRequiredService<T>() where T : class;
}
