using Arcana.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Arcana.Plugins.Core;

/// <summary>
/// Base class for all plugins.
/// 所有插件的基類
/// </summary>
public abstract class PluginBase : IPlugin
{
    private PluginState _state = PluginState.NotLoaded;
    protected IPluginContext? Context { get; private set; }

    public abstract PluginMetadata Metadata { get; }
    public PluginState State => _state;

    public virtual async Task ActivateAsync(IPluginContext context)
    {
        _state = PluginState.Activating;
        Context = context;

        try
        {
            await OnActivateAsync(context);
            RegisterContributions(context);
            _state = PluginState.Active;
        }
        catch
        {
            _state = PluginState.Error;
            throw;
        }
    }

    public virtual async Task DeactivateAsync()
    {
        _state = PluginState.Deactivating;

        try
        {
            await OnDeactivateAsync();

            // Dispose all subscriptions
            if (Context != null)
            {
                foreach (var subscription in Context.Subscriptions)
                {
                    subscription.Dispose();
                }
                Context.Subscriptions.Clear();
            }

            _state = PluginState.Deactivated;
        }
        catch
        {
            _state = PluginState.Error;
            throw;
        }
    }

    public virtual void ConfigureServices(IServiceCollection services)
    {
        // Override in derived classes to register services
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_state == PluginState.Active)
        {
            await DeactivateAsync();
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Called when the plugin is activated.
    /// </summary>
    protected virtual Task OnActivateAsync(IPluginContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the plugin is deactivated.
    /// </summary>
    protected virtual Task OnDeactivateAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override to register menu items, views, commands, etc.
    /// </summary>
    protected virtual void RegisterContributions(IPluginContext context)
    {
        // Override in derived classes
    }

    /// <summary>
    /// Registers a command.
    /// </summary>
    protected IDisposable RegisterCommand(string commandId, Func<Task> handler)
    {
        var sub = Context!.Commands.RegisterCommand(commandId, _ => handler());
        Context.Subscriptions.Add(sub);
        return sub;
    }

    /// <summary>
    /// Registers a command with parameter.
    /// </summary>
    protected IDisposable RegisterCommand<T>(string commandId, Func<T, Task> handler)
    {
        var sub = Context!.Commands.RegisterCommand(commandId, handler);
        Context.Subscriptions.Add(sub);
        return sub;
    }

    /// <summary>
    /// Registers a menu item.
    /// </summary>
    protected IDisposable RegisterMenuItem(MenuItemDefinition item)
    {
        var sub = Context!.Menus.RegisterMenuItem(item);
        Context.Subscriptions.Add(sub);
        return sub;
    }

    /// <summary>
    /// Registers multiple menu items.
    /// </summary>
    protected IDisposable RegisterMenuItems(params MenuItemDefinition[] items)
    {
        var sub = Context!.Menus.RegisterMenuItems(items);
        Context.Subscriptions.Add(sub);
        return sub;
    }

    /// <summary>
    /// Registers a view.
    /// </summary>
    protected IDisposable RegisterView(ViewDefinition view)
    {
        var sub = Context!.Views.RegisterView(view);
        Context.Subscriptions.Add(sub);
        return sub;
    }

    /// <summary>
    /// Subscribes to a message.
    /// </summary>
    protected IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : class
    {
        var sub = Context!.MessageBus.Subscribe(handler);
        Context.Subscriptions.Add(sub);
        return sub;
    }

    /// <summary>
    /// Subscribes to an event.
    /// </summary>
    protected IDisposable SubscribeToEvent<TEvent>(Action<TEvent> handler) where TEvent : IApplicationEvent
    {
        var sub = Context!.Events.Subscribe(handler);
        Context.Subscriptions.Add(sub);
        return sub;
    }

    /// <summary>
    /// Publishes a message.
    /// </summary>
    protected Task PublishAsync<TMessage>(TMessage message) where TMessage : class
    {
        return Context!.MessageBus.PublishAsync(message);
    }

    /// <summary>
    /// Logs information.
    /// </summary>
    protected void LogInfo(string message, params object[] args)
    {
        Context?.Logger.LogInformation(message, args);
    }

    /// <summary>
    /// Logs a warning.
    /// </summary>
    protected void LogWarning(string message, params object[] args)
    {
        Context?.Logger.LogWarning(message, args);
    }

    /// <summary>
    /// Logs an error.
    /// </summary>
    protected void LogError(Exception ex, string message, params object[] args)
    {
        Context?.Logger.LogError(ex, message, args);
    }
}

/// <summary>
/// Helper extension for logging.
/// </summary>
internal static class LoggerExtensions
{
    public static void LogInformation(this Microsoft.Extensions.Logging.ILogger logger, string message, params object[] args)
    {
        logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, message, args);
    }

    public static void LogWarning(this Microsoft.Extensions.Logging.ILogger logger, string message, params object[] args)
    {
        logger.Log(Microsoft.Extensions.Logging.LogLevel.Warning, message, args);
    }

    public static void LogError(this Microsoft.Extensions.Logging.ILogger logger, Exception ex, string message, params object[] args)
    {
        logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, ex, message, args);
    }
}
