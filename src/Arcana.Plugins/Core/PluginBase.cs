using System.Text.Json;
using Arcana.Plugins.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcana.Plugins.Core;

/// <summary>
/// Base class for all plugins.
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

    /// <summary>
    /// Gets a localized string for this plugin.
    /// </summary>
    protected string L(string key)
    {
        return Context?.Localization.GetForPlugin(Metadata.Id, key) ?? key;
    }

    /// <summary>
    /// Gets a localized string for this plugin with format arguments.
    /// </summary>
    protected string L(string key, params object[] args)
    {
        return Context?.Localization.GetForPlugin(Metadata.Id, key, args) ?? key;
    }

    /// <summary>
    /// Registers plugin resources for a specific culture.
    /// </summary>
    protected void RegisterResources(string cultureName, IDictionary<string, string> resources)
    {
        Context?.Localization.RegisterPluginResources(Metadata.Id, cultureName, resources);
    }

    /// <summary>
    /// Loads localization resources from external JSON files.
    /// Files should be in the format: locales/{culture}.json (e.g., locales/zh-TW.json)
    /// </summary>
    /// <param name="basePath">Base path for localization files. If null, uses plugin path.</param>
    protected async Task LoadExternalLocalizationAsync(string? basePath = null)
    {
        var localesPath = basePath ?? Path.Combine(Context!.PluginPath, "locales");

        if (!Directory.Exists(localesPath))
        {
            LogWarning("Locales directory not found: {Path}", localesPath);
            return;
        }

        foreach (var file in Directory.GetFiles(localesPath, "*.json"))
        {
            try
            {
                var cultureName = Path.GetFileNameWithoutExtension(file);
                var json = await File.ReadAllTextAsync(file);
                var resources = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                if (resources != null)
                {
                    RegisterResources(cultureName, resources);
                    LogInfo("Loaded {Count} localization resources for {Culture}", resources.Count, cultureName);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Failed to load localization file: {File}", file);
            }
        }
    }

    /// <summary>
    /// Loads localization resources from embedded resource files.
    /// Resource names should match: {Namespace}.Locales.{culture}.json
    /// </summary>
    protected void LoadEmbeddedLocalization()
    {
        var assembly = GetType().Assembly;
        var resourcePrefix = GetType().Namespace + ".Locales.";

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(resourcePrefix) || !resourceName.EndsWith(".json"))
                continue;

            try
            {
                // Extract culture name from resource name
                var cultureName = resourceName
                    .Replace(resourcePrefix, "")
                    .Replace(".json", "");

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) continue;

                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var resources = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                if (resources != null)
                {
                    RegisterResources(cultureName, resources);
                    LogInfo("Loaded {Count} embedded localization resources for {Culture}", resources.Count, cultureName);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Failed to load embedded localization: {Resource}", resourceName);
            }
        }
    }

    /// <summary>
    /// Loads localization from a specific JSON file.
    /// </summary>
    protected async Task LoadLocalizationFileAsync(string cultureName, string filePath)
    {
        if (!File.Exists(filePath))
        {
            LogWarning("Localization file not found: {Path}", filePath);
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var resources = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (resources != null)
            {
                RegisterResources(cultureName, resources);
                LogInfo("Loaded {Count} localization resources for {Culture} from {Path}",
                    resources.Count, cultureName, filePath);
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Failed to load localization file: {Path}", filePath);
        }
    }
}
