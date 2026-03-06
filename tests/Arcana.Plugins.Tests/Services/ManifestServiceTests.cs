using Arcana.Plugins.Contracts.Manifest;
using Arcana.Plugins.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Plugins.Tests.Services;

public class ManifestServiceTests
{
    private readonly ManifestServiceImpl _service;
    private readonly Mock<ILogger<ManifestServiceImpl>> _loggerMock;

    public ManifestServiceTests()
    {
        _loggerMock = new Mock<ILogger<ManifestServiceImpl>>();
        _service = new ManifestServiceImpl(_loggerMock.Object);
    }

    private static PluginManifest CreateManifest(
        string id = "test.plugin",
        string name = "Test Plugin",
        string version = "1.0.0")
    {
        return new PluginManifest { Id = id, Name = name, Version = version };
    }

    // ─── RegisterManifest / GetManifest ─────────────────────────────────────

    [Fact]
    public void RegisterManifest_ThenGetManifest_ShouldReturnSame()
    {
        var manifest = CreateManifest();
        _service.RegisterManifest(manifest);

        var result = _service.GetManifest("test.plugin");

        result.Should().NotBeNull();
        result!.Id.Should().Be("test.plugin");
    }

    [Fact]
    public void GetManifest_NotRegistered_ShouldReturnNull()
    {
        var result = _service.GetManifest("nonexistent");
        result.Should().BeNull();
    }

    [Fact]
    public void RegisterManifest_OverwritesExisting()
    {
        var v1 = CreateManifest(version: "1.0.0");
        var v2 = CreateManifest(version: "2.0.0");

        _service.RegisterManifest(v1);
        _service.RegisterManifest(v2);

        var result = _service.GetManifest("test.plugin");
        result!.Version.Should().Be("2.0.0");
    }

    [Fact]
    public void RegisterManifest_WithBasePath_ShouldReturnDirectory()
    {
        var manifest = CreateManifest();
        _service.RegisterManifest(manifest, @"/plugins/test");

        var dir = _service.GetManifestDirectory("test.plugin");
        dir.Should().Be("/plugins/test");
    }

    [Fact]
    public void RegisterManifest_WithoutBasePath_DirectoryShouldBeNull()
    {
        var manifest = CreateManifest();
        _service.RegisterManifest(manifest);

        var dir = _service.GetManifestDirectory("test.plugin");
        dir.Should().BeNull();
    }

    // ─── GetAllManifests ─────────────────────────────────────────────────────

    [Fact]
    public void GetAllManifests_NoManifests_ShouldReturnEmpty()
    {
        var result = _service.GetAllManifests();
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAllManifests_MultipleRegistered_ShouldReturnAll()
    {
        _service.RegisterManifest(CreateManifest("plugin.a", "A", "1.0.0"));
        _service.RegisterManifest(CreateManifest("plugin.b", "B", "1.0.0"));
        _service.RegisterManifest(CreateManifest("plugin.c", "C", "1.0.0"));

        var result = _service.GetAllManifests();
        result.Should().HaveCount(3);
        result.Select(m => m.Id).Should().Contain(["plugin.a", "plugin.b", "plugin.c"]);
    }

    // ─── GetManifestDirectory ────────────────────────────────────────────────

    [Fact]
    public void GetManifestDirectory_NotRegistered_ShouldReturnNull()
    {
        var dir = _service.GetManifestDirectory("unknown");
        dir.Should().BeNull();
    }

    // ─── ShouldActivateOnStartup ─────────────────────────────────────────────

    [Fact]
    public void ShouldActivateOnStartup_UnknownPlugin_ShouldReturnTrue()
    {
        var result = _service.ShouldActivateOnStartup("nonexistent");
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldActivateOnStartup_NullActivationEvents_ShouldReturnTrue()
    {
        var manifest = CreateManifest();
        manifest.ActivationEvents = null;
        _service.RegisterManifest(manifest);

        var result = _service.ShouldActivateOnStartup("test.plugin");
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldActivateOnStartup_EmptyActivationEvents_ShouldReturnTrue()
    {
        var manifest = CreateManifest();
        manifest.ActivationEvents = [];
        _service.RegisterManifest(manifest);

        var result = _service.ShouldActivateOnStartup("test.plugin");
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldActivateOnStartup_WithOnStartupEvent_ShouldReturnTrue()
    {
        var manifest = CreateManifest();
        manifest.ActivationEvents = [ActivationEvents.OnStartup];
        _service.RegisterManifest(manifest);

        var result = _service.ShouldActivateOnStartup("test.plugin");
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldActivateOnStartup_WithStarEvent_ShouldReturnTrue()
    {
        var manifest = CreateManifest();
        manifest.ActivationEvents = [ActivationEvents.Star];
        _service.RegisterManifest(manifest);

        var result = _service.ShouldActivateOnStartup("test.plugin");
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldActivateOnStartup_OnlyCommandEvent_ShouldReturnFalse()
    {
        var manifest = CreateManifest();
        manifest.ActivationEvents = ["onCommand:order.new"];
        _service.RegisterManifest(manifest);

        var result = _service.ShouldActivateOnStartup("test.plugin");
        result.Should().BeFalse();
    }

    // ─── GetActivationEvents ─────────────────────────────────────────────────

    [Fact]
    public void GetActivationEvents_UnknownPlugin_ShouldReturnEmpty()
    {
        var result = _service.GetActivationEvents("nonexistent");
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetActivationEvents_NullEvents_ShouldReturnEmpty()
    {
        var manifest = CreateManifest();
        manifest.ActivationEvents = null;
        _service.RegisterManifest(manifest);

        var result = _service.GetActivationEvents("test.plugin");
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetActivationEvents_WithEvents_ShouldReturnThem()
    {
        var manifest = CreateManifest();
        manifest.ActivationEvents = ["onStartup", "onCommand:test"];
        _service.RegisterManifest(manifest);

        var result = _service.GetActivationEvents("test.plugin");
        result.Should().HaveCount(2);
        result.Should().Contain("onStartup");
        result.Should().Contain("onCommand:test");
    }

    // ─── GetPluginsForActivationEvent ────────────────────────────────────────

    [Fact]
    public void GetPluginsForActivationEvent_NoMatches_ShouldReturnEmpty()
    {
        _service.RegisterManifest(CreateManifest());

        var result = _service.GetPluginsForActivationEvent(ActivationEventType.OnCommand, "test");
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetPluginsForActivationEvent_StarMatch_ShouldReturnAll()
    {
        var manifest = CreateManifest();
        manifest.ActivationEvents = [ActivationEvents.Star];
        _service.RegisterManifest(manifest);

        var result = _service.GetPluginsForActivationEvent(ActivationEventType.OnCommand, "any");
        result.Should().Contain("test.plugin");
    }

    [Fact]
    public void GetPluginsForActivationEvent_CommandMatch_ShouldReturn()
    {
        var manifest = CreateManifest();
        manifest.ActivationEvents = ["onCommand:order.new"];
        _service.RegisterManifest(manifest);

        var result = _service.GetPluginsForActivationEvent(ActivationEventType.OnCommand, "order.new");
        result.Should().Contain("test.plugin");
    }

    [Fact]
    public void GetPluginsForActivationEvent_CommandNoArgMatch_ShouldReturn()
    {
        var manifest = CreateManifest();
        manifest.ActivationEvents = ["onCommand:order.new"];
        _service.RegisterManifest(manifest);

        // null argument should match any command
        var result = _service.GetPluginsForActivationEvent(ActivationEventType.OnCommand, null);
        result.Should().Contain("test.plugin");
    }

    // ─── ValidateManifest ────────────────────────────────────────────────────

    [Fact]
    public void ValidateManifest_ValidManifest_ShouldBeValid()
    {
        var manifest = CreateManifest("test", "Test Plugin", "1.0.0");
        var result = _service.ValidateManifest(manifest);
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateManifest_MissingId_ShouldHaveError()
    {
        var manifest = new PluginManifest { Id = "", Name = "Test", Version = "1.0.0" };
        var result = _service.ValidateManifest(manifest);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("id"));
    }

    [Fact]
    public void ValidateManifest_MissingName_ShouldHaveError()
    {
        var manifest = new PluginManifest { Id = "test", Name = "", Version = "1.0.0" };
        var result = _service.ValidateManifest(manifest);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("name"));
    }

    [Fact]
    public void ValidateManifest_MissingVersion_ShouldHaveError()
    {
        var manifest = new PluginManifest { Id = "test", Name = "Test", Version = "" };
        var result = _service.ValidateManifest(manifest);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("version"));
    }

    [Fact]
    public void ValidateManifest_InvalidVersion_ShouldHaveWarning()
    {
        var manifest = CreateManifest(version: "not-a-version");
        var result = _service.ValidateManifest(manifest);
        result.IsValid.Should().BeTrue(); // only warning, not error
        result.Warnings.Should().Contain(w => w.Contains("version"));
    }

    [Fact]
    public void ValidateManifest_UnknownActivationEvent_ShouldHaveWarning()
    {
        var manifest = CreateManifest();
        manifest.ActivationEvents = ["unknownEvent"];
        var result = _service.ValidateManifest(manifest);
        result.Warnings.Should().Contain(w => w.Contains("Unknown activation event"));
    }

    [Fact]
    public void ValidateManifest_ViewMissingId_ShouldHaveError()
    {
        var manifest = CreateManifest();
        manifest.Contributes = new ManifestContributions
        {
            Views =
            [
                new ManifestViewDefinition { Id = "", TitleKey = "view.title" }
            ]
        };

        var result = _service.ValidateManifest(manifest);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("View"));
    }

    [Fact]
    public void ValidateManifest_MenuMissingLocation_ShouldHaveError()
    {
        var manifest = CreateManifest();
        manifest.Contributes = new ManifestContributions
        {
            Menus =
            [
                new ManifestMenuDefinition { Id = "test.menu", Location = "" }
            ]
        };

        var result = _service.ValidateManifest(manifest);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("location"));
    }

    // ─── LoadManifestAsync (file system) ─────────────────────────────────────

    [Fact]
    public async Task LoadManifestAsync_FileNotFound_ShouldReturnNull()
    {
        var result = await _service.LoadManifestAsync("/tmp/nonexistent.json");
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadManifestAsync_ValidJsonFile_ShouldReturnManifest()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var json = """{"id":"file.plugin","name":"File Plugin","version":"1.0.0"}""";
            await File.WriteAllTextAsync(tempFile, json);

            var result = await _service.LoadManifestAsync(tempFile);

            result.Should().NotBeNull();
            result!.Id.Should().Be("file.plugin");
            result.Name.Should().Be("File Plugin");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadManifestAsync_InvalidJson_ShouldReturnNull()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "{ this is not valid json }");

            var result = await _service.LoadManifestAsync(tempFile);
            result.Should().BeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadManifestAsync_MissingRequiredId_ShouldReturnNull()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var json = """{"name":"No ID Plugin","version":"1.0.0"}""";
            await File.WriteAllTextAsync(tempFile, json);

            var result = await _service.LoadManifestAsync(tempFile);
            result.Should().BeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    // ─── DiscoverManifestsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task DiscoverManifestsAsync_DirectoryNotFound_ShouldReturnEmpty()
    {
        var result = await _service.DiscoverManifestsAsync("/tmp/nonexistent-plugins-dir-xyz");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DiscoverManifestsAsync_DirectoryWithManifests_ShouldLoadAll()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create two plugin subdirectories
            for (int i = 1; i <= 2; i++)
            {
                var pluginDir = Path.Combine(tempDir, $"plugin{i}");
                Directory.CreateDirectory(pluginDir);
                var json = $$$"""{"id":"plugin{{{i}}}","name":"Plugin {{{i}}}","version":"1.0.0"}""";
                await File.WriteAllTextAsync(Path.Combine(pluginDir, "plugin.manifest.json"), json);
            }

            var result = await _service.DiscoverManifestsAsync(tempDir);
            result.Should().HaveCount(2);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
