using System.Collections.Concurrent;
using System.Diagnostics;
using Arcana.Plugins.Contracts.Manifest;
using Microsoft.Extensions.Logging;

namespace Arcana.Plugins.Services;

/// <summary>
/// Service for monitoring and triggering plugin activation events.
/// </summary>
public class ActivationEventService : IActivationEventService
{
    private readonly ManifestService _manifestService;
    private readonly ILogger<ActivationEventService> _logger;

    // Plugins waiting for activation (not yet loaded)
    private readonly ConcurrentDictionary<string, List<string>> _pendingPlugins = new();

    // Plugin activation callback
    private Func<string, Task>? _activatePluginCallback;

    // Track which plugins have been activated
    private readonly ConcurrentDictionary<string, bool> _activatedPlugins = new();

    public event EventHandler<PluginActivatedEventArgs>? PluginActivated;

    public ActivationEventService(
        ManifestService manifestService,
        ILogger<ActivationEventService> logger)
    {
        _manifestService = manifestService;
        _logger = logger;
    }

    /// <summary>
    /// Sets the callback for activating plugins.
    /// Called by PluginManager during initialization.
    /// </summary>
    public void SetActivationCallback(Func<string, Task> callback)
    {
        _activatePluginCallback = callback;
    }

    /// <summary>
    /// Registers a plugin as pending activation.
    /// </summary>
    public void RegisterPendingPlugin(string pluginId, IEnumerable<string> activationEvents)
    {
        var events = activationEvents.ToList();
        if (events.Count > 0)
        {
            _pendingPlugins[pluginId] = events;
            _logger.LogDebug("Registered pending plugin {PluginId} with events: {Events}",
                pluginId, string.Join(", ", events));
        }
    }

    /// <summary>
    /// Marks a plugin as activated (no longer pending).
    /// </summary>
    public void MarkActivated(string pluginId)
    {
        _pendingPlugins.TryRemove(pluginId, out _);
        _activatedPlugins[pluginId] = true;
    }

    /// <inheritdoc />
    public async Task FireAsync(ActivationEventType eventType, string? argument = null)
    {
        var eventString = eventType switch
        {
            ActivationEventType.OnStartup => ActivationEvents.OnStartup,
            ActivationEventType.OnCommand => $"{ActivationEvents.OnCommandPrefix}{argument}",
            ActivationEventType.OnView => $"{ActivationEvents.OnViewPrefix}{argument}",
            ActivationEventType.OnLanguage => $"{ActivationEvents.OnLanguagePrefix}{argument}",
            ActivationEventType.OnFileType => $"{ActivationEvents.OnFileTypePrefix}{argument}",
            ActivationEventType.OnUri => $"{ActivationEvents.OnUriPrefix}{argument}",
            ActivationEventType.OnConfiguration => $"{ActivationEvents.OnConfigurationPrefix}{argument}",
            ActivationEventType.OnAuthentication => $"{ActivationEvents.OnAuthenticationPrefix}{argument}",
            ActivationEventType.OnMenu => $"{ActivationEvents.OnMenuPrefix}{argument}",
            _ => null
        };

        if (eventString != null)
        {
            await FireAsync(eventString);
        }
    }

    /// <inheritdoc />
    public async Task FireAsync(string activationEvent)
    {
        _logger.LogDebug("Firing activation event: {Event}", activationEvent);

        var (eventType, argument) = ActivationEvents.Parse(activationEvent);
        var pluginsToActivate = GetPluginsToActivate(eventType, argument);

        if (pluginsToActivate.Count == 0)
        {
            _logger.LogDebug("No plugins to activate for event: {Event}", activationEvent);
            return;
        }

        _logger.LogInformation("Activating {Count} plugins for event: {Event}",
            pluginsToActivate.Count, activationEvent);

        foreach (var pluginId in pluginsToActivate)
        {
            await ActivatePluginAsync(pluginId, activationEvent);
        }
    }

    private List<string> GetPluginsToActivate(ActivationEventType eventType, string? argument)
    {
        var result = new List<string>();

        foreach (var (pluginId, events) in _pendingPlugins)
        {
            // Skip already activated plugins
            if (_activatedPlugins.ContainsKey(pluginId))
                continue;

            foreach (var evt in events)
            {
                var (type, arg) = ActivationEvents.Parse(evt);

                // Wildcard matches everything
                if (type == ActivationEventType.Star)
                {
                    result.Add(pluginId);
                    break;
                }

                // Match event type
                if (type == eventType)
                {
                    // For parameterized events, match the argument
                    if (arg == null || argument == null || arg == argument)
                    {
                        result.Add(pluginId);
                        break;
                    }
                }
            }
        }

        return result;
    }

    private async Task ActivatePluginAsync(string pluginId, string activationEvent)
    {
        if (_activatePluginCallback == null)
        {
            _logger.LogWarning("No activation callback set, cannot activate plugin: {PluginId}", pluginId);
            return;
        }

        if (_activatedPlugins.ContainsKey(pluginId))
        {
            _logger.LogDebug("Plugin already activated: {PluginId}", pluginId);
            return;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Activating plugin {PluginId} due to event: {Event}", pluginId, activationEvent);

            await _activatePluginCallback(pluginId);

            sw.Stop();
            MarkActivated(pluginId);

            _logger.LogInformation("Plugin {PluginId} activated in {ElapsedMs}ms", pluginId, sw.ElapsedMilliseconds);

            PluginActivated?.Invoke(this, new PluginActivatedEventArgs
            {
                PluginId = pluginId,
                ActivationEvent = activationEvent,
                ActivationTime = sw.Elapsed
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Failed to activate plugin {PluginId}", pluginId);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetPluginsForEvent(ActivationEventType eventType, string? argument = null)
    {
        return GetPluginsToActivate(eventType, argument);
    }

    /// <inheritdoc />
    public bool IsPendingActivation(string pluginId)
    {
        return _pendingPlugins.ContainsKey(pluginId) && !_activatedPlugins.ContainsKey(pluginId);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetPendingPlugins()
    {
        return _pendingPlugins
            .Where(p => !_activatedPlugins.ContainsKey(p.Key))
            .ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.AsReadOnly());
    }
}
