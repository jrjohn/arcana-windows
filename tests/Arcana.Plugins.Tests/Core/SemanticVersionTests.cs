using Arcana.Plugins.Core;
using FluentAssertions;
using Xunit;

namespace Arcana.Plugins.Tests.Core;

public class SemanticVersionTests
{
    #region Parsing Tests

    [Theory]
    [InlineData("1.0.0", 1, 0, 0, null, null)]
    [InlineData("2.1.3", 2, 1, 3, null, null)]
    [InlineData("0.0.1", 0, 0, 1, null, null)]
    [InlineData("10.20.30", 10, 20, 30, null, null)]
    [InlineData("1.0.0-alpha", 1, 0, 0, "alpha", null)]
    [InlineData("1.0.0-alpha.1", 1, 0, 0, "alpha.1", null)]
    [InlineData("1.0.0-beta.2", 1, 0, 0, "beta.2", null)]
    [InlineData("1.0.0-rc.1", 1, 0, 0, "rc.1", null)]
    [InlineData("1.0.0+build123", 1, 0, 0, null, "build123")]
    [InlineData("1.0.0-alpha+build", 1, 0, 0, "alpha", "build")]
    public void Parse_ValidVersionString_ShouldParseCorrectly(
        string input, int major, int minor, int patch, string? prerelease, string? build)
    {
        // Act
        var version = SemanticVersion.Parse(input);

        // Assert
        version.Major.Should().Be(major);
        version.Minor.Should().Be(minor);
        version.Patch.Should().Be(patch);
        version.Prerelease.Should().Be(prerelease);
        version.BuildMetadata.Should().Be(build);
    }

    [Theory]
    [InlineData("1.0", 1, 0, 0)] // Simple version without patch
    public void Parse_SimpleVersion_ShouldParseAsSemVer(string input, int major, int minor, int patch)
    {
        // Act
        var result = SemanticVersion.TryParse(input, out var version);

        // Assert
        result.Should().BeTrue();
        version.Major.Should().Be(major);
        version.Minor.Should().Be(minor);
        version.Patch.Should().Be(patch);
    }

    [Theory]
    [InlineData("1")] // Major only - not valid without minor
    public void Parse_MajorOnly_ShouldReturnFalse(string input)
    {
        // Act
        var result = SemanticVersion.TryParse(input, out _);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryParse_EmptyOrNull_ShouldReturnFalse(string? input)
    {
        // Act
        var result = SemanticVersion.TryParse(input, out _);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Parse_InvalidVersion_ShouldThrowFormatException()
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => SemanticVersion.Parse("not-a-version"));
    }

    #endregion

    #region Comparison Tests

    [Theory]
    [InlineData("1.0.0", "2.0.0", -1)]
    [InlineData("2.0.0", "1.0.0", 1)]
    [InlineData("1.0.0", "1.0.0", 0)]
    [InlineData("1.0.0", "1.1.0", -1)]
    [InlineData("1.1.0", "1.0.0", 1)]
    [InlineData("1.0.0", "1.0.1", -1)]
    [InlineData("1.0.1", "1.0.0", 1)]
    public void CompareTo_DifferentVersions_ShouldReturnCorrectOrder(
        string version1, string version2, int expected)
    {
        // Arrange
        var v1 = SemanticVersion.Parse(version1);
        var v2 = SemanticVersion.Parse(version2);

        // Act
        var result = v1.CompareTo(v2);

        // Assert
        Math.Sign(result).Should().Be(expected);
    }

    [Theory]
    [InlineData("1.0.0-alpha", "1.0.0", -1)] // Prerelease < Release
    [InlineData("1.0.0", "1.0.0-alpha", 1)]  // Release > Prerelease
    [InlineData("1.0.0-alpha", "1.0.0-beta", -1)] // alpha < beta
    [InlineData("1.0.0-alpha.1", "1.0.0-alpha.2", -1)]
    public void CompareTo_PrereleaseVersions_ShouldCompareCorrectly(
        string version1, string version2, int expected)
    {
        // Arrange
        var v1 = SemanticVersion.Parse(version1);
        var v2 = SemanticVersion.Parse(version2);

        // Act
        var result = v1.CompareTo(v2);

        // Assert
        Math.Sign(result).Should().Be(expected);
    }

    [Fact]
    public void CompareTo_Null_ShouldReturnPositive()
    {
        // Arrange
        var version = SemanticVersion.Parse("1.0.0");

        // Act
        var result = version.CompareTo(null);

        // Assert
        result.Should().Be(1);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameVersion_ShouldBeTrue()
    {
        // Arrange
        var v1 = SemanticVersion.Parse("1.2.3");
        var v2 = SemanticVersion.Parse("1.2.3");

        // Act & Assert
        v1.Equals(v2).Should().BeTrue();
        (v1 == v2).Should().BeTrue();
        (v1 != v2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentVersion_ShouldBeFalse()
    {
        // Arrange
        var v1 = SemanticVersion.Parse("1.2.3");
        var v2 = SemanticVersion.Parse("1.2.4");

        // Act & Assert
        v1.Equals(v2).Should().BeFalse();
        (v1 == v2).Should().BeFalse();
        (v1 != v2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SameVersion_ShouldBeSame()
    {
        // Arrange
        var v1 = SemanticVersion.Parse("1.2.3");
        var v2 = SemanticVersion.Parse("1.2.3");

        // Act & Assert
        v1.GetHashCode().Should().Be(v2.GetHashCode());
    }

    #endregion

    #region Operator Tests

    [Theory]
    [InlineData("1.0.0", "2.0.0", true)]
    [InlineData("2.0.0", "1.0.0", false)]
    [InlineData("1.0.0", "1.0.0", false)]
    public void LessThanOperator_ShouldWorkCorrectly(string v1, string v2, bool expected)
    {
        var version1 = SemanticVersion.Parse(v1);
        var version2 = SemanticVersion.Parse(v2);

        (version1 < version2).Should().Be(expected);
    }

    [Theory]
    [InlineData("1.0.0", "2.0.0", true)]
    [InlineData("2.0.0", "1.0.0", false)]
    [InlineData("1.0.0", "1.0.0", true)]
    public void LessThanOrEqualOperator_ShouldWorkCorrectly(string v1, string v2, bool expected)
    {
        var version1 = SemanticVersion.Parse(v1);
        var version2 = SemanticVersion.Parse(v2);

        (version1 <= version2).Should().Be(expected);
    }

    [Theory]
    [InlineData("2.0.0", "1.0.0", true)]
    [InlineData("1.0.0", "2.0.0", false)]
    [InlineData("1.0.0", "1.0.0", false)]
    public void GreaterThanOperator_ShouldWorkCorrectly(string v1, string v2, bool expected)
    {
        var version1 = SemanticVersion.Parse(v1);
        var version2 = SemanticVersion.Parse(v2);

        (version1 > version2).Should().Be(expected);
    }

    #endregion

    #region Conversion Tests

    [Fact]
    public void FromVersion_ShouldConvertCorrectly()
    {
        // Arrange
        var netVersion = new Version(1, 2, 3);

        // Act
        var semVer = SemanticVersion.FromVersion(netVersion);

        // Assert
        semVer.Major.Should().Be(1);
        semVer.Minor.Should().Be(2);
        semVer.Patch.Should().Be(3);
    }

    [Fact]
    public void ToVersion_ShouldConvertCorrectly()
    {
        // Arrange
        var semVer = new SemanticVersion(1, 2, 3);

        // Act
        var netVersion = semVer.ToVersion();

        // Assert
        netVersion.Major.Should().Be(1);
        netVersion.Minor.Should().Be(2);
        netVersion.Build.Should().Be(3);
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        // Arrange & Act & Assert
        new SemanticVersion(1, 2, 3).ToString().Should().Be("1.2.3");
        new SemanticVersion(1, 2, 3, "alpha").ToString().Should().Be("1.2.3-alpha");
        new SemanticVersion(1, 2, 3, null, "build").ToString().Should().Be("1.2.3+build");
        new SemanticVersion(1, 2, 3, "alpha", "build").ToString().Should().Be("1.2.3-alpha+build");
    }

    #endregion

    #region Constructor Validation Tests

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void Constructor_NegativeValues_ShouldThrowArgumentException(int major, int minor, int patch)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SemanticVersion(major, minor, patch));
    }

    #endregion
}
