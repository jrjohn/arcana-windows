using Arcana.Plugins.Core;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Plugins.Tests.Core;

public class DependencyResolverTests
{
    private readonly Mock<ILogger<DependencyResolver>> _loggerMock;
    private readonly DependencyResolver _resolver;

    public DependencyResolverTests()
    {
        _loggerMock = new Mock<ILogger<DependencyResolver>>();
        _resolver = new DependencyResolver(_loggerMock.Object);
    }

    #region Basic Resolution Tests

    [Fact]
    public void Resolve_NoDependencies_ShouldSucceed()
    {
        // Arrange
        _resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"));
        _resolver.Register("plugin-b", SemanticVersion.Parse("1.0.0"));

        // Act
        var result = _resolver.Resolve();

        // Assert
        result.Success.Should().BeTrue();
        result.ResolvedOrder.Should().Contain("plugin-a");
        result.ResolvedOrder.Should().Contain("plugin-b");
    }

    [Fact]
    public void Resolve_SimpleDependency_ShouldOrderCorrectly()
    {
        // Arrange
        _resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-b@^1.0.0")
        });
        _resolver.Register("plugin-b", SemanticVersion.Parse("1.0.0"));

        // Act
        var result = _resolver.Resolve();

        // Assert
        result.Success.Should().BeTrue();
        var order = result.ResolvedOrder.ToList();
        order.IndexOf("plugin-b").Should().BeLessThan(order.IndexOf("plugin-a"));
    }

    [Fact]
    public void Resolve_ChainedDependencies_ShouldOrderCorrectly()
    {
        // Arrange
        // C depends on B, B depends on A
        _resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"));
        _resolver.Register("plugin-b", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-a@^1.0.0")
        });
        _resolver.Register("plugin-c", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-b@^1.0.0")
        });

        // Act
        var result = _resolver.Resolve();

        // Assert
        result.Success.Should().BeTrue();
        var order = result.ResolvedOrder.ToList();
        order.IndexOf("plugin-a").Should().BeLessThan(order.IndexOf("plugin-b"));
        order.IndexOf("plugin-b").Should().BeLessThan(order.IndexOf("plugin-c"));
    }

    [Fact]
    public void Resolve_DiamondDependency_ShouldSucceed()
    {
        // Arrange
        // D depends on B and C, both B and C depend on A
        _resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"));
        _resolver.Register("plugin-b", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-a@^1.0.0")
        });
        _resolver.Register("plugin-c", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-a@^1.0.0")
        });
        _resolver.Register("plugin-d", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-b@^1.0.0"),
            PluginDependency.Parse("plugin-c@^1.0.0")
        });

        // Act
        var result = _resolver.Resolve();

        // Assert
        result.Success.Should().BeTrue();
        var order = result.ResolvedOrder.ToList();
        order.IndexOf("plugin-a").Should().BeLessThan(order.IndexOf("plugin-b"));
        order.IndexOf("plugin-a").Should().BeLessThan(order.IndexOf("plugin-c"));
        order.IndexOf("plugin-b").Should().BeLessThan(order.IndexOf("plugin-d"));
        order.IndexOf("plugin-c").Should().BeLessThan(order.IndexOf("plugin-d"));
    }

    #endregion

    #region Version Constraint Tests

    [Fact]
    public void Resolve_SatisfiedVersionConstraint_ShouldSucceed()
    {
        // Arrange
        _resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-b@>=1.0.0")
        });
        _resolver.Register("plugin-b", SemanticVersion.Parse("1.5.0"));

        // Act
        var result = _resolver.Resolve();

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Resolve_UnsatisfiedVersionConstraint_ShouldFail()
    {
        // Arrange
        _resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-b@>=2.0.0")
        });
        _resolver.Register("plugin-b", SemanticVersion.Parse("1.5.0"));

        // Act
        var result = _resolver.Resolve();

        // Assert
        result.Success.Should().BeFalse();
        result.Conflicts.Should().NotBeEmpty();
    }

    [Fact]
    public void Resolve_ConflictingVersionRequirements_ShouldFail()
    {
        // Arrange
        // Both A and B depend on C, but with incompatible version requirements
        _resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-c@>=2.0.0")
        });
        _resolver.Register("plugin-b", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-c@<2.0.0")
        });
        _resolver.Register("plugin-c", SemanticVersion.Parse("1.5.0"));

        // Act
        var result = _resolver.Resolve();

        // Assert
        result.Success.Should().BeFalse();
        result.Conflicts.Should().NotBeEmpty();
    }

    [Fact]
    public void Resolve_CaretVersionConstraint_ShouldAllowMinorUpdates()
    {
        // Arrange
        _resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-b@^1.0.0")
        });
        _resolver.Register("plugin-b", SemanticVersion.Parse("1.9.9"));

        // Act
        var result = _resolver.Resolve();

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Resolve_CaretVersionConstraint_ShouldRejectMajorUpdates()
    {
        // Arrange
        _resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-b@^1.0.0")
        });
        _resolver.Register("plugin-b", SemanticVersion.Parse("2.0.0"));

        // Act
        var result = _resolver.Resolve();

        // Assert
        result.Success.Should().BeFalse();
        result.Conflicts.Should().NotBeEmpty();
    }

    #endregion

    #region Missing Dependency Tests

    [Fact]
    public void Resolve_MissingDependency_ShouldFail()
    {
        // Arrange
        _resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-b@^1.0.0")
        });
        // plugin-b is not registered

        // Act
        var result = _resolver.Resolve();

        // Assert
        result.Success.Should().BeFalse();
        result.MissingDependencies.Should().NotBeEmpty();
    }

    [Fact]
    public void Resolve_OptionalMissingDependency_ShouldSucceed()
    {
        // Arrange
        _resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"), new[]
        {
            new PluginDependency
            {
                PluginId = "plugin-b",
                VersionRange = VersionRange.Parse("^1.0.0"),
                Optional = true
            }
        });
        // plugin-b is not registered but optional

        // Act
        var result = _resolver.Resolve();

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Circular Dependency Tests

    [Fact]
    public void Resolve_CircularDependency_ShouldFail()
    {
        // Arrange
        _resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-b@^1.0.0")
        });
        _resolver.Register("plugin-b", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-a@^1.0.0")
        });

        // Act
        var result = _resolver.Resolve();

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public void Resolve_IndirectCircularDependency_ShouldFail()
    {
        // Arrange
        // A -> B -> C -> A
        _resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-b@^1.0.0")
        });
        _resolver.Register("plugin-b", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-c@^1.0.0")
        });
        _resolver.Register("plugin-c", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-a@^1.0.0")
        });

        // Act
        var result = _resolver.Resolve();

        // Assert
        result.Success.Should().BeFalse();
    }

    #endregion

    #region ResolveFor Tests

    [Fact]
    public void ResolveFor_SpecificPlugin_ShouldReturnOnlyRequiredDependencies()
    {
        // Arrange
        _resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"));
        _resolver.Register("plugin-b", SemanticVersion.Parse("1.0.0"), new[]
        {
            PluginDependency.Parse("plugin-a@^1.0.0")
        });
        _resolver.Register("plugin-c", SemanticVersion.Parse("1.0.0")); // Unrelated

        // Act
        var result = _resolver.ResolveFor("plugin-b");

        // Assert
        result.Success.Should().BeTrue();
        result.ResolvedOrder.Should().Contain("plugin-a");
        result.ResolvedOrder.Should().Contain("plugin-b");
        result.ResolvedOrder.Should().NotContain("plugin-c");
    }

    [Fact]
    public void ResolveFor_UnknownPlugin_ShouldFail()
    {
        // Arrange
        _resolver.Register("plugin-a", SemanticVersion.Parse("1.0.0"));

        // Act
        var result = _resolver.ResolveFor("plugin-unknown");

        // Assert
        result.Success.Should().BeFalse();
        result.MissingDependencies.Should().Contain("plugin-unknown");
    }

    #endregion
}
