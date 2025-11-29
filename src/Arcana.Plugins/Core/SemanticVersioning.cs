using System.Text.RegularExpressions;

namespace Arcana.Plugins.Core;

/// <summary>
/// Semantic version with comparison support.
/// 語義化版本，支援比較
/// </summary>
public partial class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string? Prerelease { get; }
    public string? BuildMetadata { get; }

    public SemanticVersion(int major, int minor, int patch, string? prerelease = null, string? buildMetadata = null)
    {
        if (major < 0) throw new ArgumentException("Major version must be non-negative", nameof(major));
        if (minor < 0) throw new ArgumentException("Minor version must be non-negative", nameof(minor));
        if (patch < 0) throw new ArgumentException("Patch version must be non-negative", nameof(patch));

        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
        BuildMetadata = buildMetadata;
    }

    public static SemanticVersion Parse(string version)
    {
        if (!TryParse(version, out var result))
        {
            throw new FormatException($"Invalid semantic version: {version}");
        }
        return result;
    }

    public static bool TryParse(string? version, out SemanticVersion result)
    {
        result = default!;

        if (string.IsNullOrWhiteSpace(version))
            return false;

        var match = SemVerRegex().Match(version);
        if (!match.Success)
        {
            // Try to parse as simple version
            if (Version.TryParse(version, out var simpleVersion))
            {
                result = new SemanticVersion(
                    simpleVersion.Major,
                    simpleVersion.Minor >= 0 ? simpleVersion.Minor : 0,
                    simpleVersion.Build >= 0 ? simpleVersion.Build : 0);
                return true;
            }
            return false;
        }

        var major = int.Parse(match.Groups["major"].Value);
        var minor = int.Parse(match.Groups["minor"].Value);
        var patch = int.Parse(match.Groups["patch"].Value);
        var prerelease = match.Groups["prerelease"].Success ? match.Groups["prerelease"].Value : null;
        var build = match.Groups["build"].Success ? match.Groups["build"].Value : null;

        result = new SemanticVersion(major, minor, patch, prerelease, build);
        return true;
    }

    public static SemanticVersion FromVersion(Version version)
    {
        return new SemanticVersion(
            version.Major,
            version.Minor >= 0 ? version.Minor : 0,
            version.Build >= 0 ? version.Build : 0);
    }

    public Version ToVersion()
    {
        return new Version(Major, Minor, Patch);
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other == null) return 1;

        var result = Major.CompareTo(other.Major);
        if (result != 0) return result;

        result = Minor.CompareTo(other.Minor);
        if (result != 0) return result;

        result = Patch.CompareTo(other.Patch);
        if (result != 0) return result;

        // Prerelease versions have lower precedence
        if (Prerelease == null && other.Prerelease != null) return 1;
        if (Prerelease != null && other.Prerelease == null) return -1;
        if (Prerelease != null && other.Prerelease != null)
        {
            return string.Compare(Prerelease, other.Prerelease, StringComparison.Ordinal);
        }

        return 0;
    }

    public bool Equals(SemanticVersion? other)
    {
        if (other == null) return false;
        return CompareTo(other) == 0;
    }

    public override bool Equals(object? obj) => Equals(obj as SemanticVersion);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, Prerelease);

    public override string ToString()
    {
        var result = $"{Major}.{Minor}.{Patch}";
        if (!string.IsNullOrEmpty(Prerelease))
            result += $"-{Prerelease}";
        if (!string.IsNullOrEmpty(BuildMetadata))
            result += $"+{BuildMetadata}";
        return result;
    }

    public static bool operator ==(SemanticVersion? left, SemanticVersion? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(SemanticVersion? left, SemanticVersion? right) =>
        !(left == right);

    public static bool operator <(SemanticVersion? left, SemanticVersion? right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(SemanticVersion? left, SemanticVersion? right) =>
        left is null || left.CompareTo(right) <= 0;

    public static bool operator >(SemanticVersion? left, SemanticVersion? right) =>
        left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(SemanticVersion? left, SemanticVersion? right) =>
        left is null ? right is null : left.CompareTo(right) >= 0;

    [GeneratedRegex(@"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<prerelease>[0-9A-Za-z\-\.]+))?(?:\+(?<build>[0-9A-Za-z\-\.]+))?$")]
    private static partial Regex SemVerRegex();
}

/// <summary>
/// Version range for dependency constraints.
/// 依賴約束的版本範圍
/// </summary>
public class VersionRange
{
    public SemanticVersion? MinVersion { get; init; }
    public SemanticVersion? MaxVersion { get; init; }
    public bool MinInclusive { get; init; } = true;
    public bool MaxInclusive { get; init; } = false;

    /// <summary>
    /// Checks if a version satisfies this range.
    /// </summary>
    public bool IsSatisfiedBy(SemanticVersion version)
    {
        if (MinVersion != null)
        {
            var cmp = version.CompareTo(MinVersion);
            if (MinInclusive ? cmp < 0 : cmp <= 0)
                return false;
        }

        if (MaxVersion != null)
        {
            var cmp = version.CompareTo(MaxVersion);
            if (MaxInclusive ? cmp > 0 : cmp >= 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Parses a version range string.
    /// Supports formats: 1.0.0, >=1.0.0, >1.0.0 &lt;2.0.0, ~1.0.0, ^1.0.0, 1.0.0 - 2.0.0
    /// </summary>
    public static VersionRange Parse(string rangeString)
    {
        if (string.IsNullOrWhiteSpace(rangeString))
        {
            return new VersionRange(); // Any version
        }

        rangeString = rangeString.Trim();

        // Exact version: 1.0.0
        if (SemanticVersion.TryParse(rangeString, out var exact))
        {
            return new VersionRange
            {
                MinVersion = exact,
                MaxVersion = exact,
                MinInclusive = true,
                MaxInclusive = true
            };
        }

        // Caret range: ^1.0.0 (compatible with)
        if (rangeString.StartsWith('^'))
        {
            var version = SemanticVersion.Parse(rangeString[1..]);
            return new VersionRange
            {
                MinVersion = version,
                MaxVersion = new SemanticVersion(version.Major + 1, 0, 0),
                MinInclusive = true,
                MaxInclusive = false
            };
        }

        // Tilde range: ~1.0.0 (approximately equivalent)
        if (rangeString.StartsWith('~'))
        {
            var version = SemanticVersion.Parse(rangeString[1..]);
            return new VersionRange
            {
                MinVersion = version,
                MaxVersion = new SemanticVersion(version.Major, version.Minor + 1, 0),
                MinInclusive = true,
                MaxInclusive = false
            };
        }

        // Hyphen range: 1.0.0 - 2.0.0
        if (rangeString.Contains(" - "))
        {
            var parts = rangeString.Split(" - ");
            return new VersionRange
            {
                MinVersion = SemanticVersion.Parse(parts[0].Trim()),
                MaxVersion = SemanticVersion.Parse(parts[1].Trim()),
                MinInclusive = true,
                MaxInclusive = true
            };
        }

        // Comparison operators
        if (rangeString.StartsWith(">="))
        {
            return new VersionRange
            {
                MinVersion = SemanticVersion.Parse(rangeString[2..].Trim()),
                MinInclusive = true
            };
        }

        if (rangeString.StartsWith('>'))
        {
            return new VersionRange
            {
                MinVersion = SemanticVersion.Parse(rangeString[1..].Trim()),
                MinInclusive = false
            };
        }

        if (rangeString.StartsWith("<="))
        {
            return new VersionRange
            {
                MaxVersion = SemanticVersion.Parse(rangeString[2..].Trim()),
                MaxInclusive = true
            };
        }

        if (rangeString.StartsWith('<'))
        {
            return new VersionRange
            {
                MaxVersion = SemanticVersion.Parse(rangeString[1..].Trim()),
                MaxInclusive = false
            };
        }

        // Wildcard: 1.* or 1.0.*
        if (rangeString.Contains('*'))
        {
            var parts = rangeString.Replace("*", "0").Split('.');
            var major = int.Parse(parts[0]);
            var minor = parts.Length > 1 ? int.Parse(parts[1]) : 0;

            if (rangeString.EndsWith(".*"))
            {
                if (parts.Length == 2)
                {
                    // 1.* means >=1.0.0 <2.0.0
                    return new VersionRange
                    {
                        MinVersion = new SemanticVersion(major, 0, 0),
                        MaxVersion = new SemanticVersion(major + 1, 0, 0),
                        MinInclusive = true,
                        MaxInclusive = false
                    };
                }
                else
                {
                    // 1.0.* means >=1.0.0 <1.1.0
                    return new VersionRange
                    {
                        MinVersion = new SemanticVersion(major, minor, 0),
                        MaxVersion = new SemanticVersion(major, minor + 1, 0),
                        MinInclusive = true,
                        MaxInclusive = false
                    };
                }
            }
        }

        throw new FormatException($"Invalid version range: {rangeString}");
    }

    public override string ToString()
    {
        if (MinVersion != null && MaxVersion != null && MinVersion.Equals(MaxVersion))
        {
            return MinVersion.ToString();
        }

        var parts = new List<string>();

        if (MinVersion != null)
        {
            parts.Add($"{(MinInclusive ? ">=" : ">")}{MinVersion}");
        }

        if (MaxVersion != null)
        {
            parts.Add($"{(MaxInclusive ? "<=" : "<")}{MaxVersion}");
        }

        return parts.Count > 0 ? string.Join(" ", parts) : "*";
    }
}

/// <summary>
/// Plugin dependency with version constraint.
/// </summary>
public record PluginDependency
{
    /// <summary>
    /// The plugin ID of the dependency.
    /// </summary>
    public required string PluginId { get; init; }

    /// <summary>
    /// The version constraint for the dependency.
    /// </summary>
    public required VersionRange VersionRange { get; init; }

    /// <summary>
    /// Whether this dependency is optional.
    /// </summary>
    public bool Optional { get; init; }

    /// <summary>
    /// Parses a dependency string in format "pluginId@versionRange" or just "pluginId".
    /// </summary>
    public static PluginDependency Parse(string dependencyString)
    {
        var parts = dependencyString.Split('@', 2);
        var pluginId = parts[0].Trim();
        var versionRange = parts.Length > 1 ? VersionRange.Parse(parts[1].Trim()) : new VersionRange();

        return new PluginDependency
        {
            PluginId = pluginId,
            VersionRange = versionRange
        };
    }

    public override string ToString()
    {
        return $"{PluginId}@{VersionRange}";
    }
}

/// <summary>
/// Result of dependency resolution.
/// </summary>
public record DependencyResolutionResult
{
    public bool Success { get; init; }
    public IReadOnlyList<string> ResolvedOrder { get; init; } = Array.Empty<string>();
    public IReadOnlyList<DependencyConflict> Conflicts { get; init; } = Array.Empty<DependencyConflict>();
    public IReadOnlyList<string> MissingDependencies { get; init; } = Array.Empty<string>();

    public static DependencyResolutionResult Succeeded(IReadOnlyList<string> order) =>
        new() { Success = true, ResolvedOrder = order };

    public static DependencyResolutionResult Failed(
        IReadOnlyList<DependencyConflict>? conflicts = null,
        IReadOnlyList<string>? missing = null) =>
        new()
        {
            Success = false,
            Conflicts = conflicts ?? Array.Empty<DependencyConflict>(),
            MissingDependencies = missing ?? Array.Empty<string>()
        };
}

/// <summary>
/// Represents a version conflict between dependencies.
/// </summary>
public record DependencyConflict
{
    public required string DependencyId { get; init; }
    public required string RequiredBy1 { get; init; }
    public required VersionRange Requirement1 { get; init; }
    public required string RequiredBy2 { get; init; }
    public required VersionRange Requirement2 { get; init; }
    public SemanticVersion? AvailableVersion { get; init; }
}
