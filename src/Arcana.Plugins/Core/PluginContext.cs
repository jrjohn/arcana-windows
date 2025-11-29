using Arcana.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcana.Plugins.Core;

/// <summary>
/// Plugin context implementation.
/// </summary>
public class PluginContext : IPluginContext
{
    private readonly IServiceProvider _serviceProvider;

    public string PluginId { get; }
    public string PluginPath { get; }
    public string DataPath { get; }
    public ILogger Logger { get; }
    public ICommandService Commands { get; }
    public IWindowService Window { get; }
    public IMessageBus MessageBus { get; }
    public IEventAggregator Events { get; }
    public ISharedStateStore SharedState { get; }
    public IMenuRegistry Menus { get; }
    public IViewRegistry Views { get; }
    public INavigationService Navigation { get; }
    public ILocalizationService Localization { get; }
    public IList<IDisposable> Subscriptions { get; } = new List<IDisposable>();

    public PluginContext(
        string pluginId,
        string pluginPath,
        string dataPath,
        IServiceProvider serviceProvider)
    {
        PluginId = pluginId;
        PluginPath = pluginPath;
        DataPath = dataPath;
        _serviceProvider = serviceProvider;

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        Logger = loggerFactory.CreateLogger($"Plugin.{pluginId}");

        Commands = serviceProvider.GetRequiredService<ICommandService>();
        Window = serviceProvider.GetRequiredService<IWindowService>();
        MessageBus = serviceProvider.GetRequiredService<IMessageBus>();
        Events = serviceProvider.GetRequiredService<IEventAggregator>();
        SharedState = serviceProvider.GetRequiredService<ISharedStateStore>();
        Menus = serviceProvider.GetRequiredService<IMenuRegistry>();
        Views = serviceProvider.GetRequiredService<IViewRegistry>();
        Navigation = serviceProvider.GetRequiredService<INavigationService>();
        Localization = serviceProvider.GetRequiredService<ILocalizationService>();
    }

    public T? GetService<T>() where T : class
    {
        return _serviceProvider.GetService<T>();
    }

    public T GetRequiredService<T>() where T : class
    {
        return _serviceProvider.GetRequiredService<T>();
    }
}
