using Arcana.Plugins.Contracts;
using Microsoft.Extensions.Logging;

namespace Arcana.Plugins.Core;

/// <summary>
/// Resolves plugin dependencies with version constraints.
/// 解析帶版本約束的插件依賴
/// </summary>
public class DependencyResolver
{
    private readonly ILogger<DependencyResolver> _logger;
    private readonly Dictionary<string, PluginDependencyInfo> _plugins = new();

    public DependencyResolver(ILogger<DependencyResolver> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a plugin with its dependencies.
    /// </summary>
    public void Register(string pluginId, SemanticVersion version, IEnumerable<PluginDependency>? dependencies = null)
    {
        _plugins[pluginId] = new PluginDependencyInfo
        {
            PluginId = pluginId,
            Version = version,
            Dependencies = dependencies?.ToList() ?? new List<PluginDependency>()
        };
    }

    /// <summary>
    /// Registers a plugin from its metadata.
    /// </summary>
    public void Register(PluginMetadata metadata)
    {
        var dependencies = new List<PluginDependency>();

        if (metadata.Dependencies != null)
        {
            foreach (var dep in metadata.Dependencies)
            {
                dependencies.Add(PluginDependency.Parse(dep));
            }
        }

        Register(
            metadata.Id,
            SemanticVersion.FromVersion(metadata.Version),
            dependencies);
    }

    /// <summary>
    /// Resolves all dependencies and returns the load order.
    /// </summary>
    public DependencyResolutionResult Resolve()
    {
        var conflicts = new List<DependencyConflict>();
        var missing = new List<string>();

        // Check for missing dependencies
        foreach (var (pluginId, info) in _plugins)
        {
            foreach (var dep in info.Dependencies)
            {
                if (!_plugins.TryGetValue(dep.PluginId, out var depInfo))
                {
                    if (!dep.Optional)
                    {
                        missing.Add($"{pluginId} requires {dep.PluginId}");
                    }
                    continue;
                }

                // Check version constraint
                if (!dep.VersionRange.IsSatisfiedBy(depInfo.Version))
                {
                    conflicts.Add(new DependencyConflict
                    {
                        DependencyId = dep.PluginId,
                        RequiredBy1 = pluginId,
                        Requirement1 = dep.VersionRange,
                        RequiredBy2 = dep.PluginId,
                        Requirement2 = new VersionRange { MinVersion = depInfo.Version, MaxVersion = depInfo.Version, MinInclusive = true, MaxInclusive = true },
                        AvailableVersion = depInfo.Version
                    });
                }
            }
        }

        // Check for conflicting version requirements
        var requirements = new Dictionary<string, List<(string RequiredBy, VersionRange Range)>>();

        foreach (var (pluginId, info) in _plugins)
        {
            foreach (var dep in info.Dependencies)
            {
                if (!requirements.ContainsKey(dep.PluginId))
                {
                    requirements[dep.PluginId] = new List<(string, VersionRange)>();
                }
                requirements[dep.PluginId].Add((pluginId, dep.VersionRange));
            }
        }

        foreach (var (depId, reqs) in requirements)
        {
            if (!_plugins.TryGetValue(depId, out var depInfo))
                continue;

            // Check if all requirements can be satisfied by the available version
            var unsatisfied = reqs.Where(r => !r.Range.IsSatisfiedBy(depInfo.Version)).ToList();

            if (unsatisfied.Count > 0)
            {
                foreach (var u in unsatisfied)
                {
                    // Find another requirement that is satisfied
                    var satisfied = reqs.FirstOrDefault(r => r.Range.IsSatisfiedBy(depInfo.Version));

                    conflicts.Add(new DependencyConflict
                    {
                        DependencyId = depId,
                        RequiredBy1 = u.RequiredBy,
                        Requirement1 = u.Range,
                        RequiredBy2 = satisfied.RequiredBy ?? depId,
                        Requirement2 = satisfied.Range ?? new VersionRange { MinVersion = depInfo.Version, MaxVersion = depInfo.Version },
                        AvailableVersion = depInfo.Version
                    });
                }
            }
        }

        if (conflicts.Count > 0 || missing.Count > 0)
        {
            foreach (var conflict in conflicts)
            {
                _logger.LogError(
                    "Dependency conflict: {DepId} required by {Req1} ({Range1}) and {Req2} ({Range2}), available: {Available}",
                    conflict.DependencyId,
                    conflict.RequiredBy1, conflict.Requirement1,
                    conflict.RequiredBy2, conflict.Requirement2,
                    conflict.AvailableVersion);
            }

            foreach (var m in missing)
            {
                _logger.LogError("Missing dependency: {Missing}", m);
            }

            return DependencyResolutionResult.Failed(conflicts, missing);
        }

        // Topological sort
        try
        {
            var order = TopologicalSort();
            return DependencyResolutionResult.Succeeded(order);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Circular dependency detected");
            return DependencyResolutionResult.Failed();
        }
    }

    /// <summary>
    /// Gets the resolved load order for a specific plugin and its dependencies.
    /// </summary>
    public DependencyResolutionResult ResolveFor(string pluginId)
    {
        if (!_plugins.ContainsKey(pluginId))
        {
            return DependencyResolutionResult.Failed(missing: new[] { pluginId });
        }

        // Get transitive dependencies
        var required = new HashSet<string>();
        CollectDependencies(pluginId, required);

        // Filter to only required plugins
        var filteredPlugins = _plugins
            .Where(p => required.Contains(p.Key))
            .ToDictionary(p => p.Key, p => p.Value);

        var resolver = new DependencyResolver(_logger);
        foreach (var (id, info) in filteredPlugins)
        {
            resolver.Register(id, info.Version, info.Dependencies);
        }

        return resolver.Resolve();
    }

    private void CollectDependencies(string pluginId, HashSet<string> collected)
    {
        if (!collected.Add(pluginId))
            return;

        if (_plugins.TryGetValue(pluginId, out var info))
        {
            foreach (var dep in info.Dependencies.Where(d => !d.Optional))
            {
                CollectDependencies(dep.PluginId, collected);
            }
        }
    }

    private List<string> TopologicalSort()
    {
        var sorted = new List<string>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var pluginId in _plugins.Keys)
        {
            Visit(pluginId, visited, visiting, sorted);
        }

        return sorted;
    }

    private void Visit(string pluginId, HashSet<string> visited, HashSet<string> visiting, List<string> sorted)
    {
        if (visited.Contains(pluginId))
            return;

        if (visiting.Contains(pluginId))
            throw new InvalidOperationException($"Circular dependency detected involving {pluginId}");

        visiting.Add(pluginId);

        if (_plugins.TryGetValue(pluginId, out var info))
        {
            foreach (var dep in info.Dependencies)
            {
                if (_plugins.ContainsKey(dep.PluginId))
                {
                    Visit(dep.PluginId, visited, visiting, sorted);
                }
            }
        }

        visiting.Remove(pluginId);
        visited.Add(pluginId);
        sorted.Add(pluginId);
    }

    private class PluginDependencyInfo
    {
        public required string PluginId { get; init; }
        public required SemanticVersion Version { get; init; }
        public List<PluginDependency> Dependencies { get; init; } = new();
    }
}
