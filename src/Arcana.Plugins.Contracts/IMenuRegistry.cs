namespace Arcana.Plugins.Contracts;

/// <summary>
/// Menu registry for registering menu contributions.
/// 菜單註冊表，用於註冊菜單貢獻
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
    MainMenu,           // 主菜單
    FileMenu,           // 檔案菜單
    EditMenu,           // 編輯菜單
    ViewMenu,           // 檢視菜單
    ToolsMenu,          // 工具菜單
    HelpMenu,           // 說明菜單
    ContextMenu,        // 右鍵菜單
    Toolbar,            // 工具列
    StatusBar,          // 狀態列
    FunctionTree,       // 功能樹
    QuickAccess         // 快速存取
}

/// <summary>
/// Menu item definition.
/// 菜單項目定義
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
}
