using Arcana.Plugins.Contracts;
using Arcana.Plugins.Contracts.Validation;
using Arcana.Plugins.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Plugins.Tests.Services;

public class MenuRegistryTests
{
    private readonly Mock<ILogger<MenuRegistry>> _loggerMock;
    private readonly MenuRegistry _registry;

    public MenuRegistryTests()
    {
        _loggerMock = new Mock<ILogger<MenuRegistry>>();
        _registry = new MenuRegistry(_loggerMock.Object);
    }

    #region RegisterMenuItem Tests

    [Fact]
    public void RegisterMenuItem_ValidItem_ShouldRegisterSuccessfully()
    {
        // Arrange
        var item = CreateValidMenuItem("menu.test");

        // Act
        var subscription = _registry.RegisterMenuItem(item);

        // Assert
        subscription.Should().NotBeNull();
        _registry.GetAllMenuItems().Should().Contain(item);
    }

    [Fact]
    public void RegisterMenuItem_DuplicateId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var item1 = CreateValidMenuItem("menu.test");
        var item2 = CreateValidMenuItem("menu.test");
        _registry.RegisterMenuItem(item1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _registry.RegisterMenuItem(item2));
    }

    [Fact]
    public void RegisterMenuItem_InvalidId_ShouldThrowContributionValidationException()
    {
        // Arrange
        var item = new MenuItemDefinition
        {
            Id = "123invalid", // Starts with number
            Title = "Test",
            Location = MenuLocation.MainMenu
        };

        // Act & Assert
        var exception = Assert.Throws<ContributionValidationException>(() =>
            _registry.RegisterMenuItem(item));
        exception.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public void RegisterMenuItem_EmptyTitle_ShouldThrowContributionValidationException()
    {
        // Arrange
        var item = new MenuItemDefinition
        {
            Id = "menu.test",
            Title = "",
            Location = MenuLocation.MainMenu
        };

        // Act & Assert
        var exception = Assert.Throws<ContributionValidationException>(() =>
            _registry.RegisterMenuItem(item));
        exception.ValidationErrors.Should().Contain(e => e.Contains("Title"));
    }

    [Fact]
    public void RegisterMenuItem_ShouldRaiseMenusChangedEvent()
    {
        // Arrange
        var eventRaised = false;
        _registry.MenusChanged += (sender, args) => eventRaised = true;
        var item = CreateValidMenuItem("menu.test");

        // Act
        _registry.RegisterMenuItem(item);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void RegisterMenuItem_WithWarnings_ShouldLogWarnings()
    {
        // Arrange
        var item = new MenuItemDefinition
        {
            Id = "menu.test",
            Title = "Test",
            Location = MenuLocation.MainMenu,
            Order = -1 // This should trigger a warning
        };

        // Act
        _registry.RegisterMenuItem(item);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("negative order")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region RegisterMenuItems Tests

    [Fact]
    public void RegisterMenuItems_ValidItems_ShouldRegisterAll()
    {
        // Arrange
        var items = new[]
        {
            CreateValidMenuItem("menu.test1"),
            CreateValidMenuItem("menu.test2"),
            CreateValidMenuItem("menu.test3")
        };

        // Act
        var subscription = _registry.RegisterMenuItems(items);

        // Assert
        subscription.Should().NotBeNull();
        _registry.GetAllMenuItems().Should().HaveCount(3);
    }

    [Fact]
    public void RegisterMenuItems_OneInvalidItem_ShouldNotRegisterAny()
    {
        // Arrange
        var items = new[]
        {
            CreateValidMenuItem("menu.test1"),
            new MenuItemDefinition { Id = "123invalid", Title = "Test", Location = MenuLocation.MainMenu },
            CreateValidMenuItem("menu.test3")
        };

        // Act & Assert
        Assert.Throws<ContributionValidationException>(() => _registry.RegisterMenuItems(items));
        _registry.GetAllMenuItems().Should().BeEmpty();
    }

    [Fact]
    public void RegisterMenuItems_ShouldRaiseMenusChangedEventOnce()
    {
        // Arrange
        var eventCount = 0;
        _registry.MenusChanged += (sender, args) => eventCount++;
        var items = new[]
        {
            CreateValidMenuItem("menu.test1"),
            CreateValidMenuItem("menu.test2")
        };

        // Act
        _registry.RegisterMenuItems(items);

        // Assert
        eventCount.Should().Be(1);
    }

    #endregion

    #region Unregister Tests

    [Fact]
    public void Dispose_Subscription_ShouldRemoveMenuItem()
    {
        // Arrange
        var item = CreateValidMenuItem("menu.test");
        var subscription = _registry.RegisterMenuItem(item);

        // Act
        subscription.Dispose();

        // Assert
        _registry.GetAllMenuItems().Should().BeEmpty();
    }

    [Fact]
    public void Dispose_Subscription_ShouldRaiseMenusChangedEvent()
    {
        // Arrange
        var item = CreateValidMenuItem("menu.test");
        var subscription = _registry.RegisterMenuItem(item);
        var eventRaised = false;
        _registry.MenusChanged += (sender, args) => eventRaised = true;

        // Act
        subscription.Dispose();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void Dispose_MultipleItemsSubscription_ShouldRemoveAllItems()
    {
        // Arrange
        var items = new[]
        {
            CreateValidMenuItem("menu.test1"),
            CreateValidMenuItem("menu.test2")
        };
        var subscription = _registry.RegisterMenuItems(items);

        // Act
        subscription.Dispose();

        // Assert
        _registry.GetAllMenuItems().Should().BeEmpty();
    }

    #endregion

    #region GetMenuItems Tests

    [Fact]
    public void GetMenuItems_ByLocation_ShouldFilterCorrectly()
    {
        // Arrange
        _registry.RegisterMenuItems(new[]
        {
            new MenuItemDefinition { Id = "main1", Title = "Main 1", Location = MenuLocation.MainMenu },
            new MenuItemDefinition { Id = "file1", Title = "File 1", Location = MenuLocation.FileMenu },
            new MenuItemDefinition { Id = "main2", Title = "Main 2", Location = MenuLocation.MainMenu }
        });

        // Act
        var mainMenuItems = _registry.GetMenuItems(MenuLocation.MainMenu);
        var fileMenuItems = _registry.GetMenuItems(MenuLocation.FileMenu);

        // Assert
        mainMenuItems.Should().HaveCount(2);
        fileMenuItems.Should().HaveCount(1);
    }

    [Fact]
    public void GetMenuItems_ByLocationAndModuleId_ShouldFilterCorrectly()
    {
        // Arrange
        _registry.RegisterMenuItems(new[]
        {
            new MenuItemDefinition { Id = "mod1.item1", Title = "Item 1", Location = MenuLocation.ModuleQuickAccess, ModuleId = "Module1" },
            new MenuItemDefinition { Id = "mod1.item2", Title = "Item 2", Location = MenuLocation.ModuleQuickAccess, ModuleId = "Module1" },
            new MenuItemDefinition { Id = "mod2.item1", Title = "Item 1", Location = MenuLocation.ModuleQuickAccess, ModuleId = "Module2" }
        });

        // Act
        var module1Items = _registry.GetMenuItems(MenuLocation.ModuleQuickAccess, "Module1");
        var module2Items = _registry.GetMenuItems(MenuLocation.ModuleQuickAccess, "Module2");

        // Assert
        module1Items.Should().HaveCount(2);
        module2Items.Should().HaveCount(1);
    }

    [Fact]
    public void GetMenuItems_ShouldOrderByOrderProperty()
    {
        // Arrange
        _registry.RegisterMenuItems(new[]
        {
            new MenuItemDefinition { Id = "item3", Title = "Third", Location = MenuLocation.MainMenu, Order = 30 },
            new MenuItemDefinition { Id = "item1", Title = "First", Location = MenuLocation.MainMenu, Order = 10 },
            new MenuItemDefinition { Id = "item2", Title = "Second", Location = MenuLocation.MainMenu, Order = 20 }
        });

        // Act
        var items = _registry.GetMenuItems(MenuLocation.MainMenu);

        // Assert
        items[0].Id.Should().Be("item1");
        items[1].Id.Should().Be("item2");
        items[2].Id.Should().Be("item3");
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithoutLogger_ShouldWork()
    {
        // Act
        var registry = new MenuRegistry();

        // Assert
        registry.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithLogger_ShouldWork()
    {
        // Arrange
        var logger = new Mock<ILogger<MenuRegistry>>().Object;

        // Act
        var registry = new MenuRegistry(logger);

        // Assert
        registry.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private static MenuItemDefinition CreateValidMenuItem(string id)
    {
        return new MenuItemDefinition
        {
            Id = id,
            Title = $"Title for {id}",
            Location = MenuLocation.MainMenu
        };
    }

    #endregion
}
