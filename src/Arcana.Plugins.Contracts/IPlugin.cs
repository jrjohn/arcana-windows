using Microsoft.Extensions.DependencyInjection;

namespace Arcana.Plugins.Contracts;

/// <summary>
/// Plugin type enumeration.
/// </summary>
public enum PluginType
{
    Menu,           // Menu plugin
    FunctionTree,   // Function tree plugin
    View,           // View plugin
    Widget,         // Widget plugin
    Theme,          // Theme plugin
    Module,         // Business module plugin
    Service,        // Service plugin
    DataSource,     // Data source plugin
    Export,         // Export plugin
    Import,         // Import plugin
    Print,          // Print plugin
    Auth,           // Authentication plugin
    Sync,           // Synchronization plugin
    Analytics,      // Analytics plugin
    Notification,   // Notification plugin
    EntityExtension,// Entity extension plugin
    ViewExtension,  // View extension plugin
    Workflow        // Workflow plugin
}

/// <summary>
/// Plugin state enumeration.
/// </summary>
public enum PluginState
{
    Unknown,
    NotLoaded,
    Loaded,
    Activating,
    Active,
    Deactivating,
    Deactivated,
    Uninstalled,
    Error
}

/// <summary>
/// Base plugin interface.
/// </summary>
public interface IPlugin : IAsyncDisposable
{
    /// <summary>
    /// Plugin metadata.
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Current plugin state.
    /// </summary>
    PluginState State { get; }

    /// <summary>
    /// Activates the plugin.
    /// </summary>
    Task ActivateAsync(IPluginContext context);

    /// <summary>
    /// Deactivates the plugin.
    /// </summary>
    Task DeactivateAsync();

    /// <summary>
    /// Registers plugin services with DI container.
    /// </summary>
    void ConfigureServices(IServiceCollection services);
}

/// <summary>
/// Plugin metadata.
/// </summary>
public record PluginMetadata
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required Version Version { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public PluginType Type { get; init; }
    public string? IconPath { get; init; }
    public string[]? Dependencies { get; init; }
    public string[]? ActivationEvents { get; init; }
}
