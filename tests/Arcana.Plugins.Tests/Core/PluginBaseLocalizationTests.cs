using System.Text.Json;
using Arcana.Plugins.Contracts;
using Arcana.Plugins.Core;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Plugins.Tests.Core;

public class PluginBaseLocalizationTests : IDisposable
{
    private readonly string _tempPath;
    private readonly Mock<IPluginContext> _contextMock;
    private readonly Mock<ILocalizationService> _localizationMock;
    private readonly Mock<ILogger> _loggerMock;
    private readonly TestPlugin _plugin;

    public PluginBaseLocalizationTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"plugin_locale_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempPath);

        _localizationMock = new Mock<ILocalizationService>();
        _loggerMock = new Mock<ILogger>();

        _contextMock = new Mock<IPluginContext>();
        _contextMock.Setup(c => c.PluginPath).Returns(_tempPath);
        _contextMock.Setup(c => c.Localization).Returns(_localizationMock.Object);
        _contextMock.Setup(c => c.Logger).Returns(_loggerMock.Object);
        _contextMock.Setup(c => c.Subscriptions).Returns(new List<IDisposable>());

        _plugin = new TestPlugin();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, recursive: true);
        }
    }

    #region LoadExternalLocalizationAsync Tests

    [Fact]
    public async Task LoadExternalLocalizationAsync_WithValidFiles_ShouldRegisterResources()
    {
        // Arrange
        var localesPath = Path.Combine(_tempPath, "locales");
        Directory.CreateDirectory(localesPath);

        var enResources = new Dictionary<string, string> { ["key1"] = "Value 1", ["key2"] = "Value 2" };
        var zhResources = new Dictionary<string, string> { ["key1"] = "值 1", ["key2"] = "值 2" };

        await File.WriteAllTextAsync(
            Path.Combine(localesPath, "en-US.json"),
            JsonSerializer.Serialize(enResources));
        await File.WriteAllTextAsync(
            Path.Combine(localesPath, "zh-TW.json"),
            JsonSerializer.Serialize(zhResources));

        await _plugin.TestActivateAsync(_contextMock.Object);

        // Act
        await _plugin.TestLoadExternalLocalizationAsync(localesPath);

        // Assert
        _localizationMock.Verify(
            l => l.RegisterPluginResources(
                _plugin.Metadata.Id,
                "en-US",
                It.Is<IDictionary<string, string>>(d => d["key1"] == "Value 1")),
            Times.Once);

        _localizationMock.Verify(
            l => l.RegisterPluginResources(
                _plugin.Metadata.Id,
                "zh-TW",
                It.Is<IDictionary<string, string>>(d => d["key1"] == "值 1")),
            Times.Once);
    }

    [Fact]
    public async Task LoadExternalLocalizationAsync_DirectoryNotExists_ShouldLogWarning()
    {
        // Arrange
        await _plugin.TestActivateAsync(_contextMock.Object);
        var nonExistentPath = Path.Combine(_tempPath, "non_existent_locales");

        // Act
        await _plugin.TestLoadExternalLocalizationAsync(nonExistentPath);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadExternalLocalizationAsync_InvalidJson_ShouldLogError()
    {
        // Arrange
        var localesPath = Path.Combine(_tempPath, "locales");
        Directory.CreateDirectory(localesPath);
        await File.WriteAllTextAsync(Path.Combine(localesPath, "invalid.json"), "{ invalid json }");

        await _plugin.TestActivateAsync(_contextMock.Object);

        // Act
        await _plugin.TestLoadExternalLocalizationAsync(localesPath);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to load")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadExternalLocalizationAsync_EmptyDirectory_ShouldNotRegisterAnything()
    {
        // Arrange
        var localesPath = Path.Combine(_tempPath, "locales");
        Directory.CreateDirectory(localesPath);

        await _plugin.TestActivateAsync(_contextMock.Object);

        // Act
        await _plugin.TestLoadExternalLocalizationAsync(localesPath);

        // Assert
        _localizationMock.Verify(
            l => l.RegisterPluginResources(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task LoadExternalLocalizationAsync_DefaultPath_ShouldUsePluginPathLocales()
    {
        // Arrange
        var localesPath = Path.Combine(_tempPath, "locales");
        Directory.CreateDirectory(localesPath);

        var resources = new Dictionary<string, string> { ["key1"] = "Value 1" };
        await File.WriteAllTextAsync(
            Path.Combine(localesPath, "en-US.json"),
            JsonSerializer.Serialize(resources));

        await _plugin.TestActivateAsync(_contextMock.Object);

        // Act - Call without specifying path
        await _plugin.TestLoadExternalLocalizationAsync(null);

        // Assert
        _localizationMock.Verify(
            l => l.RegisterPluginResources(
                _plugin.Metadata.Id,
                "en-US",
                It.IsAny<IDictionary<string, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadExternalLocalizationAsync_MultipleLanguages_ShouldLoadAll()
    {
        // Arrange
        var localesPath = Path.Combine(_tempPath, "locales");
        Directory.CreateDirectory(localesPath);

        var languages = new[] { "en-US", "zh-TW", "ja-JP", "de-DE", "fr-FR" };
        foreach (var lang in languages)
        {
            var resources = new Dictionary<string, string> { ["greeting"] = $"Hello in {lang}" };
            await File.WriteAllTextAsync(
                Path.Combine(localesPath, $"{lang}.json"),
                JsonSerializer.Serialize(resources));
        }

        await _plugin.TestActivateAsync(_contextMock.Object);

        // Act
        await _plugin.TestLoadExternalLocalizationAsync(localesPath);

        // Assert
        foreach (var lang in languages)
        {
            _localizationMock.Verify(
                l => l.RegisterPluginResources(
                    _plugin.Metadata.Id,
                    lang,
                    It.IsAny<IDictionary<string, string>>()),
                Times.Once);
        }
    }

    #endregion

    #region LoadLocalizationFileAsync Tests

    [Fact]
    public async Task LoadLocalizationFileAsync_ValidFile_ShouldRegisterResources()
    {
        // Arrange
        var filePath = Path.Combine(_tempPath, "custom.json");
        var resources = new Dictionary<string, string>
        {
            ["custom.key1"] = "Custom Value 1",
            ["custom.key2"] = "Custom Value 2"
        };
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(resources));

        await _plugin.TestActivateAsync(_contextMock.Object);

        // Act
        await _plugin.TestLoadLocalizationFileAsync("custom-culture", filePath);

        // Assert
        _localizationMock.Verify(
            l => l.RegisterPluginResources(
                _plugin.Metadata.Id,
                "custom-culture",
                It.Is<IDictionary<string, string>>(d =>
                    d["custom.key1"] == "Custom Value 1" &&
                    d["custom.key2"] == "Custom Value 2")),
            Times.Once);
    }

    [Fact]
    public async Task LoadLocalizationFileAsync_FileNotExists_ShouldLogWarning()
    {
        // Arrange
        await _plugin.TestActivateAsync(_contextMock.Object);

        // Act
        await _plugin.TestLoadLocalizationFileAsync("en-US", "/non/existent/file.json");

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadLocalizationFileAsync_InvalidJson_ShouldLogError()
    {
        // Arrange
        var filePath = Path.Combine(_tempPath, "invalid.json");
        await File.WriteAllTextAsync(filePath, "not valid json");

        await _plugin.TestActivateAsync(_contextMock.Object);

        // Act
        await _plugin.TestLoadLocalizationFileAsync("en-US", filePath);

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to load")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region RegisterResources Tests

    [Fact]
    public async Task RegisterResources_ShouldCallLocalizationService()
    {
        // Arrange
        await _plugin.TestActivateAsync(_contextMock.Object);
        var resources = new Dictionary<string, string>
        {
            ["test.key"] = "Test Value"
        };

        // Act
        _plugin.TestRegisterResources("en-US", resources);

        // Assert
        _localizationMock.Verify(
            l => l.RegisterPluginResources(
                _plugin.Metadata.Id,
                "en-US",
                resources),
            Times.Once);
    }

    #endregion

    #region L() Localization Helper Tests

    [Fact]
    public async Task L_ShouldReturnLocalizedString()
    {
        // Arrange
        _localizationMock
            .Setup(l => l.GetForPlugin(_plugin.Metadata.Id, "test.key"))
            .Returns("Localized Value");

        await _plugin.TestActivateAsync(_contextMock.Object);

        // Act
        var result = _plugin.TestL("test.key");

        // Assert
        result.Should().Be("Localized Value");
    }

    [Fact]
    public async Task L_WithArgs_ShouldReturnFormattedString()
    {
        // Arrange
        _localizationMock
            .Setup(l => l.GetForPlugin(_plugin.Metadata.Id, "test.key", It.IsAny<object[]>()))
            .Returns("Hello, World!");

        await _plugin.TestActivateAsync(_contextMock.Object);

        // Act
        var result = _plugin.TestL("test.key", "World");

        // Assert
        result.Should().Be("Hello, World!");
    }

    [Fact]
    public void L_BeforeActivation_ShouldReturnKey()
    {
        // Act
        var result = _plugin.TestL("test.key");

        // Assert
        result.Should().Be("test.key");
    }

    #endregion

    #region Test Plugin Class

    private class TestPlugin : PluginBase
    {
        public override PluginMetadata Metadata => new()
        {
            Id = "test.plugin",
            Name = "Test Plugin",
            Version = new Version(1, 0, 0),
            Type = PluginType.Module
        };

        public Task TestActivateAsync(IPluginContext context)
        {
            return ActivateAsync(context);
        }

        public Task TestLoadExternalLocalizationAsync(string? basePath)
        {
            return LoadExternalLocalizationAsync(basePath);
        }

        public Task TestLoadLocalizationFileAsync(string cultureName, string filePath)
        {
            return LoadLocalizationFileAsync(cultureName, filePath);
        }

        public void TestRegisterResources(string cultureName, IDictionary<string, string> resources)
        {
            RegisterResources(cultureName, resources);
        }

        public string TestL(string key)
        {
            return L(key);
        }

        public string TestL(string key, params object[] args)
        {
            return L(key, args);
        }
    }

    #endregion
}
