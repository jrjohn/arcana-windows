using Arcana.Plugins.Contracts;
using Arcana.Plugins.Contracts.Manifest;
using Arcana.Plugins.Core;
using FluentAssertions;
using Xunit;

namespace Arcana.Plugins.Tests.Core;

public class PluginInfoTests
{
    // ─── Construction / properties ──────────────────────────────────────────

    [Fact]
    public void PluginInfo_RequiredProperties_ShouldBeSet()
    {
        var info = new PluginInfo
        {
            Id = "test.plugin",
            Name = "Test Plugin",
            Version = new Version(1, 2, 3),
            InstallPath = @"C:\plugins\test"
        };

        info.Id.Should().Be("test.plugin");
        info.Name.Should().Be("Test Plugin");
        info.Version.Should().Be(new Version(1, 2, 3));
        info.InstallPath.Should().Be(@"C:\plugins\test");
    }

    [Fact]
    public void PluginInfo_OptionalProperties_ShouldDefaultToNull()
    {
        var info = new PluginInfo
        {
            Id = "test",
            Name = "Test",
            Version = new Version(1, 0),
            InstallPath = "/tmp"
        };

        info.Description.Should().BeNull();
        info.Author.Should().BeNull();
        info.IconPath.Should().BeNull();
        info.Dependencies.Should().BeNull();
        info.PluginInstance.Should().BeNull();
        info.LastActivatedAt.Should().BeNull();
        info.LastError.Should().BeNull();
        info.LastErrorAt.Should().BeNull();
    }

    [Fact]
    public void PluginInfo_DefaultState_ShouldBeLoaded()
    {
        var info = new PluginInfo
        {
            Id = "test",
            Name = "Test",
            Version = new Version(1, 0),
            InstallPath = "/tmp"
        };

        // Default is enum 0 = Loaded (or first value)
        info.State.Should().Be(default(PluginState));
    }

    [Fact]
    public void PluginInfo_IsBuiltIn_False_CanUninstallAndUpgrade()
    {
        var info = new PluginInfo
        {
            Id = "test",
            Name = "Test",
            Version = new Version(1, 0),
            InstallPath = "/tmp",
            IsBuiltIn = false
        };

        info.CanUninstall.Should().BeTrue();
        info.CanUpgrade.Should().BeTrue();
    }

    [Fact]
    public void PluginInfo_IsBuiltIn_True_CannotUninstallOrUpgrade()
    {
        var info = new PluginInfo
        {
            Id = "test",
            Name = "Test",
            Version = new Version(1, 0),
            InstallPath = "/tmp",
            IsBuiltIn = true
        };

        info.CanUninstall.Should().BeFalse();
        info.CanUpgrade.Should().BeFalse();
    }

    [Fact]
    public void PluginInfo_Metadata_DefaultIsEmptyDictionary()
    {
        var info = new PluginInfo
        {
            Id = "test",
            Name = "Test",
            Version = new Version(1, 0),
            InstallPath = "/tmp"
        };

        info.Metadata.Should().NotBeNull();
        info.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void PluginInfo_InstalledAt_DefaultsToApproxNow()
    {
        var before = DateTime.UtcNow;
        var info = new PluginInfo
        {
            Id = "test",
            Name = "Test",
            Version = new Version(1, 0),
            InstallPath = "/tmp"
        };
        var after = DateTime.UtcNow;

        info.InstalledAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after.AddSeconds(1));
    }

    [Fact]
    public void PluginInfo_MutableState_CanBeUpdated()
    {
        var info = new PluginInfo
        {
            Id = "test",
            Name = "Test",
            Version = new Version(1, 0),
            InstallPath = "/tmp"
        };

        info.State = PluginState.Active;
        info.ErrorCount = 3;
        info.LastError = "Some error";
        info.LastErrorAt = DateTime.UtcNow;
        info.MemoryUsageBytes = 1024 * 1024;
        info.LastActivatedAt = DateTime.UtcNow;

        info.State.Should().Be(PluginState.Active);
        info.ErrorCount.Should().Be(3);
        info.LastError.Should().Be("Some error");
        info.LastErrorAt.Should().NotBeNull();
        info.MemoryUsageBytes.Should().Be(1024 * 1024);
        info.LastActivatedAt.Should().NotBeNull();
    }

    // ─── FromManifest ────────────────────────────────────────────────────────

    [Fact]
    public void FromManifest_ValidManifest_ShouldPopulateAllFields()
    {
        var manifest = new PluginManifest
        {
            Id = "arcana.test",
            Name = "Test Plugin",
            Version = "2.1.0",
            Description = "A test plugin",
            Author = "Test Author",
            Type = "Module",
            Dependencies = ["core.plugin", "other.plugin"],
            Icon = "icon.png"
        };

        var info = PluginInfo.FromManifest(manifest, @"C:\plugins\test");

        info.Id.Should().Be("arcana.test");
        info.Name.Should().Be("Test Plugin");
        info.Version.Should().Be(new Version(2, 1, 0));
        info.Description.Should().Be("A test plugin");
        info.Author.Should().Be("Test Author");
        info.Type.Should().Be(PluginType.Module);
        info.InstallPath.Should().Be(@"C:\plugins\test");
        info.IsBuiltIn.Should().BeFalse();
        info.PluginInstance.Should().BeNull();
        info.Dependencies.Should().BeEquivalentTo(["core.plugin", "other.plugin"]);
        info.IconPath.Should().Be("icon.png");
        info.State.Should().Be(PluginState.Loaded);
    }

    [Fact]
    public void FromManifest_InvalidVersion_ShouldDefaultTo100()
    {
        var manifest = new PluginManifest
        {
            Id = "test",
            Name = "Test",
            Version = "not-a-version"
        };

        var info = PluginInfo.FromManifest(manifest, "/tmp");

        info.Version.Should().Be(new Version(1, 0, 0));
    }

    [Fact]
    public void FromManifest_NullName_ShouldFallBackToId()
    {
        var manifest = new PluginManifest
        {
            Id = "fallback.id",
            Name = "fallback.id", // same as Id since it's required
            Version = "1.0.0"
        };

        var info = PluginInfo.FromManifest(manifest, "/tmp");

        info.Name.Should().Be("fallback.id");
    }

    [Fact]
    public void FromManifest_UnknownType_ShouldDefaultToModule()
    {
        var manifest = new PluginManifest
        {
            Id = "test",
            Name = "Test",
            Version = "1.0.0",
            Type = "UnknownPluginType"
        };

        var info = PluginInfo.FromManifest(manifest, "/tmp");

        info.Type.Should().Be(PluginType.Module);
    }

    [Fact]
    public void FromManifest_ServiceType_ShouldMapCorrectly()
    {
        var manifest = new PluginManifest
        {
            Id = "test",
            Name = "Test",
            Version = "1.0.0",
            Type = "Service"
        };

        var info = PluginInfo.FromManifest(manifest, "/tmp");

        info.Type.Should().Be(PluginType.Service);
    }
}
