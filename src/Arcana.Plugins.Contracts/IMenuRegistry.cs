namespace Arcana.Plugins.Contracts;

/// <summary>
/// Menu registry for registering menu contributions.
/// </summary>
public interface IMenuRegistry
{
    /// <summary>
    /// Registers a menu item.
    /// </summary>
    IDisposable RegisterMenuItem(MenuItemDefinition item);

    /// <summary>
    /// Registers multiple menu items.
    /// </summary>
    IDisposable RegisterMenuItems(IEnumerable<MenuItemDefinition> items);

    /// <summary>
    /// Gets all menu items for a location.
    /// </summary>
    IReadOnlyList<MenuItemDefinition> GetMenuItems(MenuLocation location);

    /// <summary>
    /// Gets menu items for a location filtered by module ID.
    /// </summary>
    IReadOnlyList<MenuItemDefinition> GetMenuItems(MenuLocation location, string moduleId);

    /// <summary>
    /// Gets all menu items.
    /// </summary>
    IReadOnlyList<MenuItemDefinition> GetAllMenuItems();

    /// <summary>
    /// Event raised when menus change.
    /// </summary>
    event EventHandler? MenusChanged;
}

/// <summary>
/// Menu location enumeration.
/// </summary>
public enum MenuLocation
{
    MainMenu,           // Main menu
    FileMenu,           // File menu
    EditMenu,           // Edit menu
    ViewMenu,           // View menu
    ToolsMenu,          // Tools menu
    HelpMenu,           // Help menu
    ContextMenu,        // Context menu
    Toolbar,            // Toolbar
    StatusBar,          // Status bar
    FunctionTree,       // Function tree
    QuickAccess,        // Quick access (main tab strip)
    ModuleQuickAccess   // Module-level quick access (nested tab strip)
}

/// <summary>
/// Menu item definition.
/// </summary>
public record MenuItemDefinition
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required MenuLocation Location { get; init; }
    public string? ParentId { get; init; }
    public string? Icon { get; init; }
    public string? Tooltip { get; init; }
    public string? Shortcut { get; init; }
    public string? Command { get; init; }
    public int Order { get; init; }
    public string? Group { get; init; }
    public string? When { get; init; }
    public bool IsSeparator { get; init; }
    public IReadOnlyList<MenuItemDefinition>? Children { get; init; }
    /// <summary>
    /// Module ID for module-level items (e.g., ModuleQuickAccess).
    /// Used to filter items by module.
    /// </summary>
    public string? ModuleId { get; init; }
}
