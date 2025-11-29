using Arcana.Plugins.Core;
using FluentAssertions;
using Xunit;

namespace Arcana.Plugins.Tests.Core;

public class VersionRangeTests
{
    #region Exact Version Tests

    [Fact]
    public void Parse_ExactVersion_ShouldMatchOnlyThatVersion()
    {
        // Arrange
        var range = VersionRange.Parse("1.2.3");

        // Act & Assert
        range.IsSatisfiedBy(SemanticVersion.Parse("1.2.3")).Should().BeTrue();
        range.IsSatisfiedBy(SemanticVersion.Parse("1.2.4")).Should().BeFalse();
        range.IsSatisfiedBy(SemanticVersion.Parse("1.2.2")).Should().BeFalse();
    }

    #endregion

    #region Caret Range Tests (^)

    [Theory]
    [InlineData("^1.0.0", "1.0.0", true)]
    [InlineData("^1.0.0", "1.5.0", true)]
    [InlineData("^1.0.0", "1.9.9", true)]
    [InlineData("^1.0.0", "2.0.0", false)]
    [InlineData("^1.0.0", "0.9.9", false)]
    [InlineData("^1.2.3", "1.2.3", true)]
    [InlineData("^1.2.3", "1.3.0", true)]
    [InlineData("^1.2.3", "1.2.2", false)]
    [InlineData("^0.1.0", "0.1.5", true)]
    [InlineData("^0.1.0", "1.0.0", false)]
    public void Parse_CaretRange_ShouldAllowCompatibleVersions(string range, string version, bool expected)
    {
        // Arrange
        var versionRange = VersionRange.Parse(range);
        var semVer = SemanticVersion.Parse(version);

        // Act
        var result = versionRange.IsSatisfiedBy(semVer);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Tilde Range Tests (~)

    [Theory]
    [InlineData("~1.2.0", "1.2.0", true)]
    [InlineData("~1.2.0", "1.2.5", true)]
    [InlineData("~1.2.0", "1.2.99", true)]
    [InlineData("~1.2.0", "1.3.0", false)]
    [InlineData("~1.2.0", "1.1.9", false)]
    [InlineData("~1.2.3", "1.2.3", true)]
    [InlineData("~1.2.3", "1.2.4", true)]
    [InlineData("~1.2.3", "1.2.2", false)]
    public void Parse_TildeRange_ShouldAllowPatchUpdates(string range, string version, bool expected)
    {
        // Arrange
        var versionRange = VersionRange.Parse(range);
        var semVer = SemanticVersion.Parse(version);

        // Act
        var result = versionRange.IsSatisfiedBy(semVer);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Hyphen Range Tests

    [Theory]
    [InlineData("1.0.0 - 2.0.0", "1.0.0", true)]
    [InlineData("1.0.0 - 2.0.0", "1.5.0", true)]
    [InlineData("1.0.0 - 2.0.0", "2.0.0", true)]
    [InlineData("1.0.0 - 2.0.0", "0.9.9", false)]
    [InlineData("1.0.0 - 2.0.0", "2.0.1", false)]
    public void Parse_HyphenRange_ShouldBeInclusive(string range, string version, bool expected)
    {
        // Arrange
        var versionRange = VersionRange.Parse(range);
        var semVer = SemanticVersion.Parse(version);

        // Act
        var result = versionRange.IsSatisfiedBy(semVer);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region Comparison Operator Tests

    [Theory]
    [InlineData(">=1.0.0", "1.0.0", true)]
    [InlineData(">=1.0.0", "2.0.0", true)]
    [InlineData(">=1.0.0", "0.9.9", false)]
    public void Parse_GreaterThanOrEqual_ShouldWorkCorrectly(string range, string version, bool expected)
    {
        var versionRange = VersionRange.Parse(range);
        var semVer = SemanticVersion.Parse(version);

        versionRange.IsSatisfiedBy(semVer).Should().Be(expected);
    }

    [Theory]
    [InlineData(">1.0.0", "1.0.0", false)]
    [InlineData(">1.0.0", "1.0.1", true)]
    [InlineData(">1.0.0", "2.0.0", true)]
    [InlineData(">1.0.0", "0.9.9", false)]
    public void Parse_GreaterThan_ShouldWorkCorrectly(string range, string version, bool expected)
    {
        var versionRange = VersionRange.Parse(range);
        var semVer = SemanticVersion.Parse(version);

        versionRange.IsSatisfiedBy(semVer).Should().Be(expected);
    }

    [Theory]
    [InlineData("<=2.0.0", "2.0.0", true)]
    [InlineData("<=2.0.0", "1.0.0", true)]
    [InlineData("<=2.0.0", "2.0.1", false)]
    public void Parse_LessThanOrEqual_ShouldWorkCorrectly(string range, string version, bool expected)
    {
        var versionRange = VersionRange.Parse(range);
        var semVer = SemanticVersion.Parse(version);

        versionRange.IsSatisfiedBy(semVer).Should().Be(expected);
    }

    [Theory]
    [InlineData("<2.0.0", "2.0.0", false)]
    [InlineData("<2.0.0", "1.9.9", true)]
    [InlineData("<2.0.0", "1.0.0", true)]
    [InlineData("<2.0.0", "2.0.1", false)]
    public void Parse_LessThan_ShouldWorkCorrectly(string range, string version, bool expected)
    {
        var versionRange = VersionRange.Parse(range);
        var semVer = SemanticVersion.Parse(version);

        versionRange.IsSatisfiedBy(semVer).Should().Be(expected);
    }

    #endregion

    #region Wildcard Tests

    [Theory]
    [InlineData("1.*", "1.0.0", true)]
    [InlineData("1.*", "1.9.9", true)]
    [InlineData("1.*", "2.0.0", false)]
    [InlineData("1.*", "0.9.9", false)]
    public void Parse_MajorWildcard_ShouldMatchMajorVersion(string range, string version, bool expected)
    {
        var versionRange = VersionRange.Parse(range);
        var semVer = SemanticVersion.Parse(version);

        versionRange.IsSatisfiedBy(semVer).Should().Be(expected);
    }

    [Theory]
    [InlineData("1.2.*", "1.2.0", true)]
    [InlineData("1.2.*", "1.2.99", true)]
    [InlineData("1.2.*", "1.3.0", false)]
    [InlineData("1.2.*", "1.1.9", false)]
    public void Parse_MinorWildcard_ShouldMatchMinorVersion(string range, string version, bool expected)
    {
        var versionRange = VersionRange.Parse(range);
        var semVer = SemanticVersion.Parse(version);

        versionRange.IsSatisfiedBy(semVer).Should().Be(expected);
    }

    #endregion

    #region Empty Range Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyRange_ShouldMatchAnyVersion(string range)
    {
        var versionRange = VersionRange.Parse(range);

        versionRange.IsSatisfiedBy(SemanticVersion.Parse("0.0.1")).Should().BeTrue();
        versionRange.IsSatisfiedBy(SemanticVersion.Parse("99.99.99")).Should().BeTrue();
    }

    #endregion

    #region Invalid Range Tests

    [Fact]
    public void Parse_InvalidRange_ShouldThrowFormatException()
    {
        Assert.Throws<FormatException>(() => VersionRange.Parse("invalid"));
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ExactVersion_ShouldReturnVersion()
    {
        var range = VersionRange.Parse("1.2.3");
        range.ToString().Should().Be("1.2.3");
    }

    [Fact]
    public void ToString_Range_ShouldReturnReadableFormat()
    {
        var range = new VersionRange
        {
            MinVersion = SemanticVersion.Parse("1.0.0"),
            MaxVersion = SemanticVersion.Parse("2.0.0"),
            MinInclusive = true,
            MaxInclusive = false
        };

        range.ToString().Should().Be(">=1.0.0 <2.0.0");
    }

    [Fact]
    public void ToString_AnyVersion_ShouldReturnStar()
    {
        var range = new VersionRange();
        range.ToString().Should().Be("*");
    }

    #endregion
}
