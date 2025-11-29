using Arcana.Plugins.Contracts;

namespace Arcana.Plugins.Core;

/// <summary>
/// Plugin information implementation.
/// </summary>
public class PluginInfo : IPluginInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required Version Version { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public PluginType Type { get; init; }
    public PluginState State { get; set; }
    public string? IconPath { get; init; }
    public required string InstallPath { get; init; }
    public DateTime InstalledAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastActivatedAt { get; set; }
    public bool IsBuiltIn { get; init; }
    public bool CanUninstall => !IsBuiltIn;
    public bool CanUpgrade => !IsBuiltIn;
    public string[]? Dependencies { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// The actual plugin instance.
    /// </summary>
    public IPlugin? PluginInstance { get; set; }

    /// <summary>
    /// Error count for health tracking.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Last error time.
    /// </summary>
    public DateTime? LastErrorAt { get; set; }

    /// <summary>
    /// Last error message.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Memory usage in bytes.
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    public static PluginInfo FromPlugin(IPlugin plugin, string installPath, bool isBuiltIn = false)
    {
        return new PluginInfo
        {
            Id = plugin.Metadata.Id,
            Name = plugin.Metadata.Name,
            Version = plugin.Metadata.Version,
            Description = plugin.Metadata.Description,
            Author = plugin.Metadata.Author,
            Type = plugin.Metadata.Type,
            State = plugin.State,
            IconPath = plugin.Metadata.IconPath,
            InstallPath = installPath,
            IsBuiltIn = isBuiltIn,
            Dependencies = plugin.Metadata.Dependencies,
            PluginInstance = plugin
        };
    }
}
