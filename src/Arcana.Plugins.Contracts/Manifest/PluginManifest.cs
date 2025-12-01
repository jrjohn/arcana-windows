using System.Text.Json.Serialization;

namespace Arcana.Plugins.Contracts.Manifest;

/// <summary>
/// Plugin manifest definition for declarative contributions.
/// This allows the host to know plugin contributions without loading the assembly.
/// </summary>
public class PluginManifest
{
    /// <summary>
    /// Unique plugin identifier (e.g., "arcana.module.order").
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Display name of the plugin.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Semantic version (e.g., "1.0.0").
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; set; }

    /// <summary>
    /// Plugin description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Plugin author.
    /// </summary>
    [JsonPropertyName("author")]
    public string? Author { get; set; }

    /// <summary>
    /// Main assembly file name (e.g., "MyPlugin.dll").
    /// </summary>
    [JsonPropertyName("main")]
    public string? Main { get; set; }

    /// <summary>
    /// Full type name of the plugin class (e.g., "MyNamespace.MyPlugin").
    /// </summary>
    [JsonPropertyName("pluginClass")]
    public string? PluginClass { get; set; }

    /// <summary>
    /// Plugin type (Module, Service, Theme, etc.).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Module";

    /// <summary>
    /// Plugin dependencies (other plugin IDs).
    /// </summary>
    [JsonPropertyName("dependencies")]
    public string[]? Dependencies { get; set; }

    /// <summary>
    /// Events that trigger plugin activation.
    /// If empty or contains "onStartup", plugin activates at startup.
    /// </summary>
    [JsonPropertyName("activationEvents")]
    public string[]? ActivationEvents { get; set; }

    /// <summary>
    /// Declarative contributions (views, menus, commands).
    /// </summary>
    [JsonPropertyName("contributes")]
    public ManifestContributions? Contributes { get; set; }

    /// <summary>
    /// Localization file paths by culture.
    /// </summary>
    [JsonPropertyName("l10n")]
    public Dictionary<string, string>? L10n { get; set; }

    /// <summary>
    /// Icon path relative to plugin directory.
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}

/// <summary>
/// All contributions declared in the manifest.
/// </summary>
public class ManifestContributions
{
    /// <summary>
    /// View contributions.
    /// </summary>
    [JsonPropertyName("views")]
    public List<ManifestViewDefinition>? Views { get; set; }

    /// <summary>
    /// Menu contributions.
    /// </summary>
    [JsonPropertyName("menus")]
    public List<ManifestMenuDefinition>? Menus { get; set; }

    /// <summary>
    /// Command contributions.
    /// </summary>
    [JsonPropertyName("commands")]
    public List<ManifestCommandDefinition>? Commands { get; set; }

    /// <summary>
    /// Toolbar contributions.
    /// </summary>
    [JsonPropertyName("toolbars")]
    public List<ManifestToolbarDefinition>? Toolbars { get; set; }

    /// <summary>
    /// Keybinding contributions.
    /// </summary>
    [JsonPropertyName("keybindings")]
    public List<ManifestKeybindingDefinition>? Keybindings { get; set; }

    /// <summary>
    /// Configuration contributions.
    /// </summary>
    [JsonPropertyName("configuration")]
    public ManifestConfigurationDefinition? Configuration { get; set; }
}

/// <summary>
/// View definition in manifest.
/// </summary>
public class ManifestViewDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Localization key for the title.
    /// </summary>
    [JsonPropertyName("titleKey")]
    public required string TitleKey { get; set; }

    /// <summary>
    /// Fallback title if localization not found.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// View type: Page, Dialog, Panel, Widget, Flyout.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Page";

    /// <summary>
    /// Full type name of the view class.
    /// </summary>
    [JsonPropertyName("viewClass")]
    public string? ViewClass { get; set; }

    /// <summary>
    /// Full type name of the view model class.
    /// </summary>
    [JsonPropertyName("viewModelClass")]
    public string? ViewModelClass { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("categoryKey")]
    public string? CategoryKey { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("canHaveMultipleInstances")]
    public bool CanHaveMultipleInstances { get; set; }

    /// <summary>
    /// Module ID for nested tab modules.
    /// </summary>
    [JsonPropertyName("moduleId")]
    public string? ModuleId { get; set; }

    /// <summary>
    /// Whether this is a default tab in the module.
    /// </summary>
    [JsonPropertyName("isModuleDefaultTab")]
    public bool IsModuleDefaultTab { get; set; }

    /// <summary>
    /// Order within the module tab strip.
    /// </summary>
    [JsonPropertyName("moduleTabOrder")]
    public int ModuleTabOrder { get; set; }
}

/// <summary>
/// Menu item definition in manifest.
/// </summary>
public class ManifestMenuDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Localization key for the title.
    /// </summary>
    [JsonPropertyName("titleKey")]
    public string? TitleKey { get; set; }

    /// <summary>
    /// Fallback title if localization not found.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Menu location: MainMenu, FileMenu, EditMenu, ViewMenu, ToolsMenu,
    /// HelpMenu, ContextMenu, Toolbar, StatusBar, FunctionTree,
    /// QuickAccess, ModuleQuickAccess.
    /// </summary>
    [JsonPropertyName("location")]
    public required string Location { get; set; }

    [JsonPropertyName("parentId")]
    public string? ParentId { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("shortcut")]
    public string? Shortcut { get; set; }

    /// <summary>
    /// Command ID to execute when clicked.
    /// </summary>
    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("group")]
    public string? Group { get; set; }

    /// <summary>
    /// Conditional expression for visibility.
    /// </summary>
    [JsonPropertyName("when")]
    public string? When { get; set; }

    [JsonPropertyName("isSeparator")]
    public bool IsSeparator { get; set; }

    /// <summary>
    /// Module ID for ModuleQuickAccess location.
    /// </summary>
    [JsonPropertyName("moduleId")]
    public string? ModuleId { get; set; }

    /// <summary>
    /// Nested menu items.
    /// </summary>
    [JsonPropertyName("children")]
    public List<ManifestMenuDefinition>? Children { get; set; }
}

/// <summary>
/// Command definition in manifest.
/// </summary>
public class ManifestCommandDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("titleKey")]
    public string? TitleKey { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Category for command palette grouping.
    /// </summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// Whether this command should appear in command palette.
    /// </summary>
    [JsonPropertyName("enablement")]
    public string? Enablement { get; set; }
}

/// <summary>
/// Toolbar definition in manifest.
/// </summary>
public class ManifestToolbarDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("titleKey")]
    public string? TitleKey { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("items")]
    public List<ManifestToolbarItemDefinition>? Items { get; set; }
}

/// <summary>
/// Toolbar item definition.
/// </summary>
public class ManifestToolbarItemDefinition
{
    [JsonPropertyName("command")]
    public required string Command { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("when")]
    public string? When { get; set; }

    [JsonPropertyName("group")]
    public string? Group { get; set; }
}

/// <summary>
/// Keybinding definition in manifest.
/// </summary>
public class ManifestKeybindingDefinition
{
    [JsonPropertyName("command")]
    public required string Command { get; set; }

    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [JsonPropertyName("when")]
    public string? When { get; set; }

    [JsonPropertyName("mac")]
    public string? Mac { get; set; }

    [JsonPropertyName("win")]
    public string? Win { get; set; }

    [JsonPropertyName("linux")]
    public string? Linux { get; set; }
}

/// <summary>
/// Configuration contribution definition.
/// </summary>
public class ManifestConfigurationDefinition
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("titleKey")]
    public string? TitleKey { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, ManifestConfigurationProperty>? Properties { get; set; }
}

/// <summary>
/// Configuration property definition.
/// </summary>
public class ManifestConfigurationProperty
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("default")]
    public object? Default { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("descriptionKey")]
    public string? DescriptionKey { get; set; }

    [JsonPropertyName("enum")]
    public List<object>? Enum { get; set; }

    [JsonPropertyName("enumDescriptions")]
    public List<string>? EnumDescriptions { get; set; }

    [JsonPropertyName("minimum")]
    public double? Minimum { get; set; }

    [JsonPropertyName("maximum")]
    public double? Maximum { get; set; }
}
