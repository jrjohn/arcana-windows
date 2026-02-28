using System.Text.Json;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Contracts.Manifest;
using Microsoft.Extensions.Logging;

namespace Arcana.Plugins.Services;

/// <summary>
/// Service that registers contributions from manifests without loading plugins.
/// When a lazy contribution is accessed, it triggers plugin activation.
/// </summary>
public class LazyContributionService
{
    private readonly ManifestService _manifestService;
    private readonly IMenuRegistry _menuRegistry;
    private readonly IViewRegistry _viewRegistry;
    private readonly CommandService _commandService;
    private readonly ActivationEventService _activationEventService;
    private readonly LocalizationService _localizationService;
    private readonly ILogger<LazyContributionService> _logger;

    // Track which contributions came from manifests (for lazy activation)
    private readonly Dictionary<string, string> _viewToPlugin = new();
    private readonly Dictionary<string, string> _commandToPlugin = new();
    private readonly Dictionary<string, string> _menuToPlugin = new();

    // Track registered disposables for cleanup
    private readonly Dictionary<string, List<IDisposable>> _pluginDisposables = new();

    public LazyContributionService(
        ManifestService manifestService,
        IMenuRegistry menuRegistry,
        IViewRegistry viewRegistry,
        CommandService commandService,
        ActivationEventService activationEventService,
        LocalizationService localizationService,
        ILogger<LazyContributionService> logger)
    {
        _manifestService = manifestService;
        _menuRegistry = menuRegistry;
        _viewRegistry = viewRegistry;
        _commandService = commandService;
        _activationEventService = activationEventService;
        _localizationService = localizationService;
        _logger = logger;
    }

    /// <summary>
    /// Registers all contributions from a manifest.
    /// This allows menus and views to appear without loading the plugin.
    /// </summary>
    public async Task RegisterManifestContributionsAsync(PluginManifest manifest, string pluginDirectory)
    {
        var pluginId = manifest.Id;
        var disposables = new List<IDisposable>();

        try
        {
            // Load localization resources first
            await LoadLocalizationAsync(manifest, pluginDirectory);

            // Register views
            if (manifest.Contributes?.Views != null)
            {
                foreach (var viewDef in manifest.Contributes.Views)
                {
                    var disposable = RegisterLazyView(pluginId, viewDef);
                    if (disposable != null)
                    {
                        disposables.Add(disposable);
                        _viewToPlugin[viewDef.Id] = pluginId;
                    }
                }
            }

            // Register menus
            if (manifest.Contributes?.Menus != null)
            {
                foreach (var menuDef in manifest.Contributes.Menus)
                {
                    var disposable = RegisterLazyMenu(pluginId, menuDef);
                    if (disposable != null)
                    {
                        disposables.Add(disposable);
                        _menuToPlugin[menuDef.Id] = pluginId;
                    }
                }
            }

            // Register commands (with lazy activation handlers)
            if (manifest.Contributes?.Commands != null)
            {
                foreach (var cmdDef in manifest.Contributes.Commands)
                {
                    var disposable = RegisterLazyCommand(pluginId, cmdDef);
                    if (disposable != null)
                    {
                        disposables.Add(disposable);
                        _commandToPlugin[cmdDef.Id] = pluginId;
                    }
                }
            }

            // Register keybindings
            if (manifest.Contributes?.Keybindings != null)
            {
                foreach (var keybinding in manifest.Contributes.Keybindings)
                {
                    // Keybindings are registered separately
                    // For now, just log them
                    _logger.LogDebug("Manifest keybinding: {Command} -> {Key}", keybinding.Command, keybinding.Key);
                }
            }

            _pluginDisposables[pluginId] = disposables;
            _logger.LogInformation("Registered {Count} lazy contributions from manifest: {PluginId}",
                disposables.Count, pluginId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering manifest contributions for: {PluginId}", pluginId);

            // Cleanup on error
            foreach (var d in disposables)
            {
                d.Dispose();
            }
        }
    }

    private async Task LoadLocalizationAsync(PluginManifest manifest, string pluginDirectory)
    {
        if (manifest.L10n == null || manifest.L10n.Count == 0)
            return;

        foreach (var (culture, relativePath) in manifest.L10n)
        {
            var filePath = Path.Combine(pluginDirectory, relativePath);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Localization file not found: {Path}", filePath);
                continue;
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var resources = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                if (resources != null)
                {
                    _localizationService.RegisterPluginResources(manifest.Id, culture, resources);
                    _logger.LogDebug("Loaded {Count} localization resources for {Culture} from {Plugin}",
                        resources.Count, culture, manifest.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading localization file: {Path}", filePath);
            }
        }
    }

    private IDisposable? RegisterLazyView(string pluginId, ManifestViewDefinition viewDef)
    {
        try
        {
            // Get localized title
            var title = !string.IsNullOrEmpty(viewDef.TitleKey)
                ? _localizationService.GetFromAnyPlugin(viewDef.TitleKey)
                : viewDef.Title ?? viewDef.Id;

            // Parse view type
            var viewType = Enum.TryParse<ViewType>(viewDef.Type, true, out var vt)
                ? vt
                : ViewType.Page;

            var definition = new ViewDefinition
            {
                Id = viewDef.Id,
                Title = title,
                TitleKey = viewDef.TitleKey,
                Icon = viewDef.Icon,
                Type = viewType,
                // ViewClass is null for lazy views - will be resolved when plugin loads
                ViewClass = null,
                // Store the class name from manifest for lazy loading
                ViewClassName = viewDef.ViewClass,
                ViewModelType = null,
                CanHaveMultipleInstances = viewDef.CanHaveMultipleInstances,
                Category = viewDef.Category,
                Order = viewDef.Order,
                ModuleId = viewDef.ModuleId,
                IsModuleDefaultTab = viewDef.IsModuleDefaultTab,
                ModuleTabOrder = viewDef.ModuleTabOrder
            };

            // Register a factory that triggers plugin activation
            var viewDisposable = _viewRegistry.RegisterView(definition);
            var factoryDisposable = _viewRegistry.RegisterViewFactory(viewDef.Id, () =>
            {
                // This triggers when someone tries to create the view
                // Fire activation event synchronously (blocking)
                _activationEventService.FireAsync(ActivationEventType.OnView, viewDef.Id).Wait();

                // After activation, the real view class should be registered
                // Return null here - the caller should retry after activation
                return null!;
            });

            return new CompositeDisposable(viewDisposable, factoryDisposable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering lazy view: {ViewId}", viewDef.Id);
            return null;
        }
    }

    private IDisposable? RegisterLazyMenu(string pluginId, ManifestMenuDefinition menuDef)
    {
        try
        {
            // Get localized title
            var title = !string.IsNullOrEmpty(menuDef.TitleKey)
                ? _localizationService.GetFromAnyPlugin(menuDef.TitleKey)
                : menuDef.Title ?? menuDef.Id;

            // Parse menu location
            var location = Enum.TryParse<MenuLocation>(menuDef.Location, true, out var loc)
                ? loc
                : MenuLocation.MainMenu;

            var definition = new MenuItemDefinition
            {
                Id = menuDef.Id,
                Title = title,
                Location = location,
                ParentId = menuDef.ParentId,
                Icon = menuDef.Icon,
                Tooltip = null,
                Shortcut = menuDef.Shortcut,
                Command = menuDef.Command,
                Order = menuDef.Order,
                Group = menuDef.Group,
                When = menuDef.When,
                IsSeparator = menuDef.IsSeparator,
                ModuleId = menuDef.ModuleId,
                Children = menuDef.Children?.Select(c => ConvertMenuChild(c)).ToList()
            };

            return _menuRegistry.RegisterMenuItem(definition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering lazy menu: {MenuId}", menuDef.Id);
            return null;
        }
    }

    private MenuItemDefinition ConvertMenuChild(ManifestMenuDefinition menuDef)
    {
        var title = !string.IsNullOrEmpty(menuDef.TitleKey)
            ? _localizationService.GetFromAnyPlugin(menuDef.TitleKey)
            : menuDef.Title ?? menuDef.Id;

        var location = Enum.TryParse<MenuLocation>(menuDef.Location, true, out var loc)
            ? loc
            : MenuLocation.MainMenu;

        return new MenuItemDefinition
        {
            Id = menuDef.Id,
            Title = title,
            Location = location,
            ParentId = menuDef.ParentId,
            Icon = menuDef.Icon,
            Shortcut = menuDef.Shortcut,
            Command = menuDef.Command,
            Order = menuDef.Order,
            Group = menuDef.Group,
            When = menuDef.When,
            IsSeparator = menuDef.IsSeparator,
            ModuleId = menuDef.ModuleId,
            Children = menuDef.Children?.Select(c => ConvertMenuChild(c)).ToList()
        };
    }

    private IDisposable? RegisterLazyCommand(string pluginId, ManifestCommandDefinition cmdDef)
    {
        try
        {
            // Register a command handler that triggers plugin activation
            return _commandService.RegisterCommand(cmdDef.Id, async (args) =>
            {
                _logger.LogInformation("Lazy command triggered: {CommandId}, activating plugin: {PluginId}",
                    cmdDef.Id, pluginId);

                // Fire activation event
                await _activationEventService.FireAsync(ActivationEventType.OnCommand, cmdDef.Id);

                // After activation, re-execute the command
                // The real handler should now be registered
                _logger.LogInformation("Plugin activated, checking for real command handler: {CommandId}", cmdDef.Id);

                if (_commandService.HasCommand(cmdDef.Id))
                {
                    _logger.LogInformation("Re-executing command with real handler: {CommandId}", cmdDef.Id);
                    try
                    {
                        await _commandService.ExecuteAsync(cmdDef.Id, args);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing command after plugin activation: {CommandId}", cmdDef.Id);
                    }
                }
                else
                {
                    _logger.LogWarning("Command not found after plugin activation: {CommandId}", cmdDef.Id);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering lazy command: {CommandId}", cmdDef.Id);
            return null;
        }
    }

    /// <summary>
    /// Updates contributions when a plugin is fully activated.
    /// The plugin's real registrations will replace the lazy ones.
    /// </summary>
    public void OnPluginActivated(string pluginId)
    {
        // Dispose lazy contributions - plugin has registered real ones
        if (_pluginDisposables.TryGetValue(pluginId, out var disposables))
        {
            _logger.LogDebug("Disposing {Count} lazy contributions for activated plugin: {PluginId}",
                disposables.Count, pluginId);

            foreach (var d in disposables)
            {
                d.Dispose();
            }

            _pluginDisposables.Remove(pluginId);
        }

        // Clean up tracking dictionaries
        var viewsToRemove = _viewToPlugin.Where(kvp => kvp.Value == pluginId).Select(kvp => kvp.Key).ToList();
        foreach (var viewId in viewsToRemove)
        {
            _viewToPlugin.Remove(viewId);
        }

        var commandsToRemove = _commandToPlugin.Where(kvp => kvp.Value == pluginId).Select(kvp => kvp.Key).ToList();
        foreach (var cmdId in commandsToRemove)
        {
            _commandToPlugin.Remove(cmdId);
        }

        var menusToRemove = _menuToPlugin.Where(kvp => kvp.Value == pluginId).Select(kvp => kvp.Key).ToList();
        foreach (var menuId in menusToRemove)
        {
            _menuToPlugin.Remove(menuId);
        }
    }

    /// <summary>
    /// Gets the plugin ID that provides a view.
    /// </summary>
    public string? GetPluginForView(string viewId)
    {
        return _viewToPlugin.TryGetValue(viewId, out var pluginId) ? pluginId : null;
    }

    /// <summary>
    /// Gets the plugin ID that provides a command.
    /// </summary>
    public string? GetPluginForCommand(string commandId)
    {
        return _commandToPlugin.TryGetValue(commandId, out var pluginId) ? pluginId : null;
    }

    /// <summary>
    /// Composite disposable helper.
    /// </summary>
    private class CompositeDisposable : IDisposable
    {
        private readonly IDisposable[] _disposables;
        private bool _disposed;

        public CompositeDisposable(params IDisposable[] disposables)
        {
            _disposables = disposables;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var d in _disposables)
            {
                d.Dispose();
            }
        }
    }
}
