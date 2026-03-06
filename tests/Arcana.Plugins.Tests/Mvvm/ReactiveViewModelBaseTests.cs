using Arcana.Plugins.Contracts.Mvvm;
using FluentAssertions;
using Xunit;

namespace Arcana.Plugins.Tests.Mvvm;

public class ReactiveViewModelBaseTests
{
    private class TestViewModel : ReactiveViewModelBase
    {
        public bool InitializeCalled { get; private set; }
        public bool CleanupCalled { get; private set; }

        public override Task InitializeAsync()
        {
            InitializeCalled = true;
            return base.InitializeAsync();
        }

        public override Task CleanupAsync()
        {
            CleanupCalled = true;
            return base.CleanupAsync();
        }

        public void AddTestDisposable(IDisposable d) => AddDisposable(d);
    }

    [Fact]
    public async Task InitializeAsync_ShouldCompleteSuccessfully()
    {
        var vm = new TestViewModel();
        await vm.InitializeAsync();
        vm.InitializeCalled.Should().BeTrue();
    }

    [Fact]
    public async Task CleanupAsync_ShouldCompleteSuccessfully()
    {
        var vm = new TestViewModel();
        await vm.CleanupAsync();
        vm.CleanupCalled.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ShouldDisposeRegisteredDisposables()
    {
        var vm = new TestViewModel();
        var disposed = false;
        var mockDisposable = new DelegateDisposable(() => disposed = true);

        vm.AddTestDisposable(mockDisposable);
        vm.Dispose();

        disposed.Should().BeTrue();
    }

    [Fact]
    public void Dispose_CalledTwice_ShouldNotThrow()
    {
        var vm = new TestViewModel();
        vm.Dispose();
        var act = () => vm.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void AddDisposable_MultipleDisposables_ShouldAllBeDisposed()
    {
        var vm = new TestViewModel();
        var count = 0;
        vm.AddTestDisposable(new DelegateDisposable(() => count++));
        vm.AddTestDisposable(new DelegateDisposable(() => count++));
        vm.AddTestDisposable(new DelegateDisposable(() => count++));

        vm.Dispose();

        count.Should().Be(3);
    }

    private class DelegateDisposable(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
