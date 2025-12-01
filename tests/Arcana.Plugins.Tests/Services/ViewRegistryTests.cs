using Arcana.Plugins.Contracts;
using Arcana.Plugins.Contracts.Validation;
using Arcana.Plugins.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arcana.Plugins.Tests.Services;

public class ViewRegistryTests
{
    private readonly Mock<ILogger<ViewRegistry>> _loggerMock;
    private readonly ViewRegistry _registry;

    public ViewRegistryTests()
    {
        _loggerMock = new Mock<ILogger<ViewRegistry>>();
        _registry = new ViewRegistry(_loggerMock.Object);
    }

    #region RegisterView Tests

    [Fact]
    public void RegisterView_ValidView_ShouldRegisterSuccessfully()
    {
        // Arrange
        var view = CreateValidView("TestPage");

        // Act
        var subscription = _registry.RegisterView(view);

        // Assert
        subscription.Should().NotBeNull();
        _registry.GetView("TestPage").Should().NotBeNull();
    }

    [Fact]
    public void RegisterView_DuplicateId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var view1 = CreateValidView("TestPage");
        var view2 = CreateValidView("TestPage");
        _registry.RegisterView(view1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _registry.RegisterView(view2));
    }

    [Fact]
    public void RegisterView_InvalidId_ShouldThrowContributionValidationException()
    {
        // Arrange
        var view = new ViewDefinition
        {
            Id = "123invalid", // Starts with number
            Title = "Test",
            TitleKey = "test.key",
            ViewClass = typeof(object)
        };

        // Act & Assert
        var exception = Assert.Throws<ContributionValidationException>(() =>
            _registry.RegisterView(view));
        exception.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public void RegisterView_EmptyTitle_ShouldThrowContributionValidationException()
    {
        // Arrange
        var view = new ViewDefinition
        {
            Id = "TestPage",
            Title = "",
            TitleKey = "test.key",
            ViewClass = typeof(object)
        };

        // Act & Assert
        var exception = Assert.Throws<ContributionValidationException>(() =>
            _registry.RegisterView(view));
        exception.ValidationErrors.Should().Contain(e => e.Contains("Title"));
    }

    [Fact]
    public void RegisterView_ShouldRaiseViewsChangedEvent()
    {
        // Arrange
        var eventRaised = false;
        _registry.ViewsChanged += (sender, args) => eventRaised = true;
        var view = CreateValidView("TestPage");

        // Act
        _registry.RegisterView(view);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void RegisterView_WithWarnings_ShouldLogWarnings()
    {
        // Arrange
        var view = new ViewDefinition
        {
            Id = "TestPage",
            Title = "Test",
            TitleKey = null, // This should trigger a warning
            ViewClass = typeof(object)
        };

        // Act
        _registry.RegisterView(view);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TitleKey")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RegisterView_NoViewClass_ShouldLogWarning()
    {
        // Arrange
        var view = new ViewDefinition
        {
            Id = "TestPage",
            Title = "Test",
            TitleKey = "test.key",
            ViewClass = null
        };

        // Act
        _registry.RegisterView(view);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ViewClass")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region RegisterViewFactory Tests

    [Fact]
    public void RegisterViewFactory_ShouldStoreFactory()
    {
        // Arrange
        var view = CreateValidView("TestPage");
        _registry.RegisterView(view);
        var factoryCallCount = 0;

        // Act
        _registry.RegisterViewFactory("TestPage", () =>
        {
            factoryCallCount++;
            return new object();
        });

        // Assert - Factory should be called when creating instance
        _registry.CreateViewInstance("TestPage");
        factoryCallCount.Should().Be(1);
    }

    [Fact]
    public void RegisterViewFactory_Dispose_ShouldRemoveFactory()
    {
        // Arrange
        var view = CreateValidView("TestPage");
        _registry.RegisterView(view);
        var subscription = _registry.RegisterViewFactory("TestPage", () => new object());

        // Act
        subscription.Dispose();

        // Assert - Should fall back to Activator.CreateInstance
        var instance = _registry.CreateViewInstance("TestPage");
        instance.Should().NotBeNull();
    }

    #endregion

    #region GetView Tests

    [Fact]
    public void GetView_ExistingId_ShouldReturnView()
    {
        // Arrange
        var view = CreateValidView("TestPage");
        _registry.RegisterView(view);

        // Act
        var result = _registry.GetView("TestPage");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("TestPage");
    }

    [Fact]
    public void GetView_NonExistingId_ShouldReturnNull()
    {
        // Act
        var result = _registry.GetView("NonExisting");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllViews Tests

    [Fact]
    public void GetAllViews_ShouldReturnAllRegisteredViews()
    {
        // Arrange
        _registry.RegisterView(CreateValidView("Page1"));
        _registry.RegisterView(CreateValidView("Page2"));
        _registry.RegisterView(CreateValidView("Page3"));

        // Act
        var views = _registry.GetAllViews();

        // Assert
        views.Should().HaveCount(3);
    }

    [Fact]
    public void GetAllViews_ShouldOrderByOrderProperty()
    {
        // Arrange
        _registry.RegisterView(new ViewDefinition
        {
            Id = "Page3",
            Title = "Third",
            TitleKey = "page3.title",
            ViewClass = typeof(object),
            Order = 30
        });
        _registry.RegisterView(new ViewDefinition
        {
            Id = "Page1",
            Title = "First",
            TitleKey = "page1.title",
            ViewClass = typeof(object),
            Order = 10
        });
        _registry.RegisterView(new ViewDefinition
        {
            Id = "Page2",
            Title = "Second",
            TitleKey = "page2.title",
            ViewClass = typeof(object),
            Order = 20
        });

        // Act
        var views = _registry.GetAllViews();

        // Assert
        views[0].Id.Should().Be("Page1");
        views[1].Id.Should().Be("Page2");
        views[2].Id.Should().Be("Page3");
    }

    #endregion

    #region GetModuleDefaultTabs Tests

    [Fact]
    public void GetModuleDefaultTabs_ShouldFilterByModuleIdAndIsDefault()
    {
        // Arrange
        _registry.RegisterView(new ViewDefinition
        {
            Id = "OrderListPage",
            Title = "Order List",
            TitleKey = "order.list",
            ViewClass = typeof(object),
            ModuleId = "OrderModule",
            IsModuleDefaultTab = true,
            ModuleTabOrder = 0
        });
        _registry.RegisterView(new ViewDefinition
        {
            Id = "OrderDetailPage",
            Title = "Order Detail",
            TitleKey = "order.detail",
            ViewClass = typeof(object),
            ModuleId = "OrderModule",
            IsModuleDefaultTab = false
        });
        _registry.RegisterView(new ViewDefinition
        {
            Id = "CustomerListPage",
            Title = "Customer List",
            TitleKey = "customer.list",
            ViewClass = typeof(object),
            ModuleId = "CustomerModule",
            IsModuleDefaultTab = true,
            ModuleTabOrder = 0
        });

        // Act
        var orderDefaultTabs = _registry.GetModuleDefaultTabs("OrderModule");
        var customerDefaultTabs = _registry.GetModuleDefaultTabs("CustomerModule");

        // Assert
        orderDefaultTabs.Should().HaveCount(1);
        orderDefaultTabs[0].Id.Should().Be("OrderListPage");
        customerDefaultTabs.Should().HaveCount(1);
        customerDefaultTabs[0].Id.Should().Be("CustomerListPage");
    }

    [Fact]
    public void GetModuleDefaultTabs_ShouldOrderByModuleTabOrder()
    {
        // Arrange
        _registry.RegisterView(new ViewDefinition
        {
            Id = "Tab2",
            Title = "Tab 2",
            TitleKey = "tab2",
            ViewClass = typeof(object),
            ModuleId = "TestModule",
            IsModuleDefaultTab = true,
            ModuleTabOrder = 2
        });
        _registry.RegisterView(new ViewDefinition
        {
            Id = "Tab1",
            Title = "Tab 1",
            TitleKey = "tab1",
            ViewClass = typeof(object),
            ModuleId = "TestModule",
            IsModuleDefaultTab = true,
            ModuleTabOrder = 1
        });

        // Act
        var tabs = _registry.GetModuleDefaultTabs("TestModule");

        // Assert
        tabs[0].Id.Should().Be("Tab1");
        tabs[1].Id.Should().Be("Tab2");
    }

    #endregion

    #region CreateViewInstance Tests

    [Fact]
    public void CreateViewInstance_WithFactory_ShouldUseFactory()
    {
        // Arrange
        var view = CreateValidView("TestPage");
        _registry.RegisterView(view);
        var factoryResult = "Factory Created";
        _registry.RegisterViewFactory("TestPage", () => factoryResult);

        // Act
        var instance = _registry.CreateViewInstance("TestPage");

        // Assert
        instance.Should().Be(factoryResult);
    }

    [Fact]
    public void CreateViewInstance_WithViewClass_ShouldUseActivator()
    {
        // Arrange
        var view = new ViewDefinition
        {
            Id = "TestPage",
            Title = "Test",
            TitleKey = "test.key",
            ViewClass = typeof(TestViewClass)
        };
        _registry.RegisterView(view);

        // Act
        var instance = _registry.CreateViewInstance("TestPage");

        // Assert
        instance.Should().BeOfType<TestViewClass>();
    }

    [Fact]
    public void CreateViewInstance_NoViewClassOrFactory_ShouldReturnNull()
    {
        // Arrange
        var view = new ViewDefinition
        {
            Id = "TestPage",
            Title = "Test",
            TitleKey = "test.key",
            ViewClass = null
        };
        _registry.RegisterView(view);

        // Act
        var instance = _registry.CreateViewInstance("TestPage");

        // Assert
        instance.Should().BeNull();
    }

    [Fact]
    public void CreateViewInstance_NonExistingView_ShouldReturnNull()
    {
        // Act
        var instance = _registry.CreateViewInstance("NonExisting");

        // Assert
        instance.Should().BeNull();
    }

    #endregion

    #region Unregister Tests

    [Fact]
    public void Dispose_Subscription_ShouldRemoveView()
    {
        // Arrange
        var view = CreateValidView("TestPage");
        var subscription = _registry.RegisterView(view);

        // Act
        subscription.Dispose();

        // Assert
        _registry.GetView("TestPage").Should().BeNull();
    }

    [Fact]
    public void Dispose_Subscription_ShouldRaiseViewsChangedEvent()
    {
        // Arrange
        var view = CreateValidView("TestPage");
        var subscription = _registry.RegisterView(view);
        var eventRaised = false;
        _registry.ViewsChanged += (sender, args) => eventRaised = true;

        // Act
        subscription.Dispose();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void Dispose_Subscription_ShouldAlsoRemoveFactory()
    {
        // Arrange
        var view = CreateValidView("TestPage");
        var subscription = _registry.RegisterView(view);
        _registry.RegisterViewFactory("TestPage", () => "Factory");

        // Act
        subscription.Dispose();

        // Assert
        _registry.CreateViewInstance("TestPage").Should().BeNull();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithoutLogger_ShouldWork()
    {
        // Act
        var registry = new ViewRegistry();

        // Assert
        registry.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithLogger_ShouldWork()
    {
        // Arrange
        var logger = new Mock<ILogger<ViewRegistry>>().Object;

        // Act
        var registry = new ViewRegistry(logger);

        // Assert
        registry.Should().NotBeNull();
    }

    #endregion

    #region Helper Classes and Methods

    private static ViewDefinition CreateValidView(string id)
    {
        return new ViewDefinition
        {
            Id = id,
            Title = $"Title for {id}",
            TitleKey = $"{id.ToLower()}.title",
            ViewClass = typeof(object)
        };
    }

    public class TestViewClass
    {
        public TestViewClass() { }
    }

    #endregion
}
