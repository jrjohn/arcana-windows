using Microsoft.Extensions.DependencyInjection;

namespace Arcana.Plugins.Contracts;

/// <summary>
/// Plugin type enumeration.
/// 插件類型
/// </summary>
public enum PluginType
{
    Menu,           // 菜單插件
    FunctionTree,   // 功能樹插件
    View,           // 視圖插件
    Widget,         // 小工具插件
    Theme,          // 主題插件
    Module,         // 業務模組插件
    Service,        // 服務插件
    DataSource,     // 資料來源插件
    Export,         // 匯出插件
    Import,         // 匯入插件
    Print,          // 列印插件
    Auth,           // 認證插件
    Sync,           // 同步插件
    Analytics,      // 分析插件
    Notification,   // 通知插件
    EntityExtension,// 實體擴展插件
    ViewExtension,  // 視圖擴展插件
    Workflow        // 工作流插件
}

/// <summary>
/// Plugin state enumeration.
/// 插件狀態
/// </summary>
public enum PluginState
{
    NotLoaded,
    Loaded,
    Activating,
    Active,
    Deactivating,
    Deactivated,
    Error
}

/// <summary>
/// Base plugin interface.
/// 插件基礎介面
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
/// 插件元資料
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
