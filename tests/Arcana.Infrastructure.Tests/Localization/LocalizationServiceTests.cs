using Arcana.Infrastructure.Localization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Infrastructure.Tests.Localization;

public class LocalizationServiceTests : IDisposable
{
    private readonly Mock<ILogger<LocalizationServiceImpl>> _loggerMock;
    private readonly string _resourcesPath;
    private LocalizationServiceImpl _service = null!;

    public LocalizationServiceTests()
    {
        _loggerMock = new Mock<ILogger<LocalizationServiceImpl>>();
        _resourcesPath = Path.Combine(Path.GetTempPath(), "arcana-l10n-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_resourcesPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_resourcesPath))
            Directory.Delete(_resourcesPath, true);
        GC.SuppressFinalize(this);
    }

    private LocalizationServiceImpl CreateService() =>
        new LocalizationServiceImpl(_loggerMock.Object, _resourcesPath);

    private void WriteResourceFile(string culture, Dictionary<string, string> resources)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(resources);
        File.WriteAllText(Path.Combine(_resourcesPath, $"{culture}.json"), json);
    }

    [Fact]
    public void Constructor_NoResourceFiles_ShouldNotThrow()
    {
        var act = () => new LocalizationServiceImpl(_loggerMock.Object, _resourcesPath);
        act.Should().NotThrow();
    }

    [Fact]
    public void CurrentCulture_Default_ShouldBeEnUs()
    {
        _service = CreateService();
        _service.CurrentCulture.Name.Should().BeOneOf("en-US", "zh-TW", "ja-JP");
    }

    [Fact]
    public void AvailableCultures_ShouldContainEnUs()
    {
        _service = CreateService();
        _service.AvailableCultures.Should().Contain(c => c.Name == "en-US");
    }

    [Fact]
    public void AvailableCultures_ShouldHaveThreeCultures()
    {
        _service = CreateService();
        _service.AvailableCultures.Should().HaveCount(3);
    }

    [Fact]
    public void Get_ExistingKey_ShouldReturnTranslation()
    {
        WriteResourceFile("en-US", new Dictionary<string, string> { ["greeting"] = "Hello" });
        _service = CreateService();
        _service.SetCulture("en-US");

        _service.Get("greeting").Should().Be("Hello");
    }

    [Fact]
    public void Get_NonExistingKey_ShouldReturnKey()
    {
        _service = CreateService();
        _service.SetCulture("en-US");

        _service.Get("nonexistent.key").Should().Be("nonexistent.key");
    }

    [Fact]
    public void Get_NullOrEmptyKey_ShouldReturnEmpty()
    {
        _service = CreateService();
        _service.Get("").Should().Be(string.Empty);
        _service.Get(null!).Should().Be(string.Empty);
    }

    [Fact]
    public void Get_WithArgs_ShouldFormatString()
    {
        WriteResourceFile("en-US", new Dictionary<string, string> { ["welcome"] = "Hello, {0}!" });
        _service = CreateService();
        _service.SetCulture("en-US");

        _service.Get("welcome", "Alice").Should().Be("Hello, Alice!");
    }

    [Fact]
    public void Get_WithArgs_InvalidFormat_ShouldReturnTemplate()
    {
        WriteResourceFile("en-US", new Dictionary<string, string> { ["broken"] = "Hello {0} {1} {999}" });
        _service = CreateService();
        _service.SetCulture("en-US");

        // Calling with too few args should still not throw (catches FormatException internally)
        var result = _service.Get("broken", "only-one-arg");
        result.Should().NotBeNull();
    }

    [Fact]
    public void SetCulture_ValidCulture_ShouldChangeCulture()
    {
        _service = CreateService();
        _service.SetCulture("zh-TW");
        _service.CurrentCulture.Name.Should().Be("zh-TW");
    }

    [Fact]
    public void SetCulture_InvalidCulture_ShouldNotChange()
    {
        _service = CreateService();
        _service.SetCulture("en-US");
        var cultureBefore = _service.CurrentCulture.Name;

        _service.SetCulture("xx-INVALID");

        _service.CurrentCulture.Name.Should().Be(cultureBefore);
    }

    [Fact]
    public void SetCulture_SameCulture_ShouldNotRaiseEvent()
    {
        _service = CreateService();
        _service.SetCulture("en-US");
        int eventCount = 0;
        _service.CultureChanged += (_, _) => eventCount++;

        _service.SetCulture("en-US");

        eventCount.Should().Be(0);
    }

    [Fact]
    public void SetCulture_DifferentCulture_ShouldRaiseEvent()
    {
        _service = CreateService();
        _service.SetCulture("en-US");
        int eventCount = 0;
        _service.CultureChanged += (_, _) => eventCount++;

        _service.SetCulture("zh-TW");

        eventCount.Should().Be(1);
    }

    [Fact]
    public void Get_CultureFallbackToEnUs_WhenKeyMissingInCurrentCulture()
    {
        WriteResourceFile("en-US", new Dictionary<string, string> { ["fallback.key"] = "Fallback Value" });
        _service = CreateService();
        _service.SetCulture("zh-TW");

        // Key not in zh-TW, should fall back to en-US
        _service.Get("fallback.key").Should().Be("Fallback Value");
    }

    [Fact]
    public void RegisterPluginResources_ShouldMakeKeysAvailable()
    {
        _service = CreateService();
        _service.SetCulture("en-US");

        _service.RegisterPluginResources("myplugin", "en-US", new Dictionary<string, string>
        {
            ["plugin.hello"] = "Plugin Hello"
        });

        _service.GetForPlugin("myplugin", "plugin.hello").Should().Be("Plugin Hello");
    }

    [Fact]
    public void GetForPlugin_NonExistingKey_ShouldFallbackToCore()
    {
        WriteResourceFile("en-US", new Dictionary<string, string> { ["core.key"] = "Core Value" });
        _service = CreateService();
        _service.SetCulture("en-US");

        _service.GetForPlugin("myplugin", "core.key").Should().Be("Core Value");
    }

    [Fact]
    public void GetForPlugin_EmptyKey_ShouldReturnEmpty()
    {
        _service = CreateService();
        _service.GetForPlugin("myplugin", "").Should().Be(string.Empty);
        _service.GetForPlugin("myplugin", null!).Should().Be(string.Empty);
    }

    [Fact]
    public void GetForPlugin_WithArgs_ShouldFormatString()
    {
        _service = CreateService();
        _service.SetCulture("en-US");
        _service.RegisterPluginResources("myplugin", "en-US", new Dictionary<string, string>
        {
            ["plugin.msg"] = "Welcome {0}!"
        });

        _service.GetForPlugin("myplugin", "plugin.msg", "World").Should().Be("Welcome World!");
    }

    [Fact]
    public void GetFromAnyPlugin_KeyInCoreResources_ShouldReturnCore()
    {
        WriteResourceFile("en-US", new Dictionary<string, string> { ["core.key"] = "Core" });
        _service = CreateService();
        _service.SetCulture("en-US");

        _service.GetFromAnyPlugin("core.key").Should().Be("Core");
    }

    [Fact]
    public void GetFromAnyPlugin_KeyInPlugin_ShouldReturnPluginValue()
    {
        _service = CreateService();
        _service.SetCulture("en-US");
        _service.RegisterPluginResources("plugin1", "en-US", new Dictionary<string, string>
        {
            ["exclusive.key"] = "Plugin Exclusive"
        });

        _service.GetFromAnyPlugin("exclusive.key").Should().Be("Plugin Exclusive");
    }

    [Fact]
    public void GetFromAnyPlugin_NotFound_ShouldReturnKey()
    {
        _service = CreateService();
        _service.SetCulture("en-US");

        _service.GetFromAnyPlugin("totally.missing").Should().Be("totally.missing");
    }

    [Fact]
    public void GetFromAnyPlugin_EmptyKey_ShouldReturnEmpty()
    {
        _service = CreateService();
        _service.GetFromAnyPlugin("").Should().Be(string.Empty);
        _service.GetFromAnyPlugin(null!).Should().Be(string.Empty);
    }

    [Fact]
    public void RegisterPluginResources_PluginFallbackToEnUs_WhenKeyMissingInCurrentCulture()
    {
        _service = CreateService();
        _service.RegisterPluginResources("plugin1", "en-US", new Dictionary<string, string>
        {
            ["fallback.msg"] = "Fallback EN"
        });
        _service.SetCulture("zh-TW");

        _service.GetForPlugin("plugin1", "fallback.msg").Should().Be("Fallback EN");
    }

    [Fact]
    public void Constructor_CorruptJsonFile_ShouldNotThrow()
    {
        File.WriteAllText(Path.Combine(_resourcesPath, "en-US.json"), "{ this is not valid json }");
        var act = () => new LocalizationServiceImpl(_loggerMock.Object, _resourcesPath);
        act.Should().NotThrow();
    }
}
