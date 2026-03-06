using Arcana.Plugins.Contracts.Mvvm;
using FluentAssertions;
using Xunit;

namespace Arcana.Plugins.Tests.Mvvm;

public class EffectSubjectGenericTests
{
    // ─── Emit ────────────────────────────────────────────────────────────────

    [Fact]
    public void Emit_NoSubscribers_ShouldNotThrow()
    {
        var subject = new EffectSubject<string>();
        var act = () => subject.Emit("hello");
        act.Should().NotThrow();
    }

    [Fact]
    public void Emit_SingleSubscriber_ShouldReceiveValue()
    {
        var subject = new EffectSubject<string>();
        string? received = null;
        subject.Subscribe(v => received = v);

        subject.Emit("hello");

        received.Should().Be("hello");
    }

    [Fact]
    public void Emit_MultipleSubscribers_ShouldDeliverToAll()
    {
        var subject = new EffectSubject<int>();
        var results = new List<int>();

        subject.Subscribe(v => results.Add(v * 1));
        subject.Subscribe(v => results.Add(v * 2));
        subject.Subscribe(v => results.Add(v * 3));

        subject.Emit(5);

        results.Should().HaveCount(3);
        results.Should().Contain(5);
        results.Should().Contain(10);
        results.Should().Contain(15);
    }

    [Fact]
    public void Emit_CalledMultipleTimes_SubscriberReceivesEach()
    {
        var subject = new EffectSubject<string>();
        var received = new List<string>();
        subject.Subscribe(v => received.Add(v));

        subject.Emit("a");
        subject.Emit("b");
        subject.Emit("c");

        received.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void Emit_SubscriberThrows_ShouldContinueToNextSubscriber()
    {
        var subject = new EffectSubject<string>();
        int secondCount = 0;

        subject.Subscribe(_ => throw new Exception("boom"));
        subject.Subscribe(_ => secondCount++);

        var act = () => subject.Emit("test");
        act.Should().NotThrow();
        secondCount.Should().Be(1);
    }

    [Fact]
    public void Emit_AfterDispose_ShouldNotDeliverToSubscribers()
    {
        var subject = new EffectSubject<string>();
        string? received = null;
        subject.Subscribe(v => received = v);

        subject.Dispose();
        subject.Emit("ignored");

        received.Should().BeNull();
    }

    // ─── Subscribe ───────────────────────────────────────────────────────────

    [Fact]
    public void Subscribe_ReturnsDisposable()
    {
        var subject = new EffectSubject<string>();
        var sub = subject.Subscribe(_ => { });
        sub.Should().NotBeNull();
        sub.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void Subscribe_ThenDispose_ShouldStopReceiving()
    {
        var subject = new EffectSubject<string>();
        string? received = null;
        var sub = subject.Subscribe(v => received = v);

        subject.Emit("first");
        sub.Dispose();
        subject.Emit("second");

        received.Should().Be("first");
    }

    [Fact]
    public void Subscribe_DisposeOnlyRemovesOwnSubscription()
    {
        var subject = new EffectSubject<int>();
        int sub1Count = 0;
        int sub2Count = 0;

        var sub1 = subject.Subscribe(_ => sub1Count++);
        subject.Subscribe(_ => sub2Count++);

        sub1.Dispose();
        subject.Emit(42);

        sub1Count.Should().Be(0);
        sub2Count.Should().Be(1);
    }

    [Fact]
    public void Subscribe_DisposeCalledTwice_ShouldNotThrow()
    {
        var subject = new EffectSubject<string>();
        var sub = subject.Subscribe(_ => { });
        sub.Dispose();
        var act = () => sub.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Subscribe_AfterDisposed_ShouldReturnEmptyDisposable()
    {
        var subject = new EffectSubject<string>();
        subject.Dispose();

        var sub = subject.Subscribe(_ => { });
        sub.Should().NotBeNull(); // EmptyDisposable returned
    }

    [Fact]
    public void Subscribe_WithErrorHandler_ReceivesValueNormally()
    {
        var subject = new EffectSubject<int>();
        int received = 0;
        Exception? caughtEx = null;

        subject.Subscribe(
            v => received = v,
            ex => caughtEx = ex);

        subject.Emit(99);

        received.Should().Be(99);
        caughtEx.Should().BeNull();
    }

    [Fact]
    public void Subscribe_WithErrorHandler_OnNextThrows_ShouldCallOnError()
    {
        var subject = new EffectSubject<int>();
        Exception? caughtEx = null;

        subject.Subscribe(
            _ => throw new InvalidOperationException("test error"),
            ex => caughtEx = ex);

        subject.Emit(1);

        caughtEx.Should().NotBeNull();
        caughtEx.Should().BeOfType<InvalidOperationException>();
        caughtEx!.Message.Should().Be("test error");
    }

    [Fact]
    public void Subscribe_WithErrorHandler_AfterDisposed_ShouldReturnEmptyDisposable()
    {
        var subject = new EffectSubject<string>();
        subject.Dispose();

        var sub = subject.Subscribe(_ => { }, _ => { });
        sub.Should().NotBeNull();
    }

    // ─── Dispose ─────────────────────────────────────────────────────────────

    [Fact]
    public void Dispose_ClearsAllSubscribers()
    {
        var subject = new EffectSubject<string>();
        int count = 0;
        subject.Subscribe(_ => count++);
        subject.Subscribe(_ => count++);

        subject.Dispose();
        subject.Emit("test");

        count.Should().Be(0);
    }

    [Fact]
    public void Dispose_CalledTwice_ShouldNotThrow()
    {
        var subject = new EffectSubject<string>();
        subject.Dispose();
        var act = () => subject.Dispose();
        act.Should().NotThrow();
    }

    // ─── Thread safety ───────────────────────────────────────────────────────

    [Fact]
    public async Task Emit_FromMultipleThreads_ShouldNotThrow()
    {
        var subject = new EffectSubject<int>();
        int count = 0;
        subject.Subscribe(v => Interlocked.Add(ref count, v));

        var tasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => subject.Emit(1)))
            .ToArray();

        var act = async () => await Task.WhenAll(tasks);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Subscribe_ConcurrentlyFromMultipleThreads_ShouldNotThrow()
    {
        var subject = new EffectSubject<string>();

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => subject.Subscribe(_ => { })))
            .ToArray();

        var act = async () => await Task.WhenAll(tasks);
        await act.Should().NotThrowAsync();
    }
}

public class EffectSubjectVoidTests
{
    // ─── Emit (void / unit) ──────────────────────────────────────────────────

    [Fact]
    public void Emit_NoSubscribers_ShouldNotThrow()
    {
        var subject = new EffectSubject();
        var act = () => subject.Emit();
        act.Should().NotThrow();
    }

    [Fact]
    public void Emit_SingleSubscriber_ShouldBeInvoked()
    {
        var subject = new EffectSubject();
        int count = 0;
        subject.Subscribe(() => count++);

        subject.Emit();

        count.Should().Be(1);
    }

    [Fact]
    public void Emit_MultipleSubscribers_ShouldInvokeAll()
    {
        var subject = new EffectSubject();
        int total = 0;
        subject.Subscribe(() => total++);
        subject.Subscribe(() => total++);
        subject.Subscribe(() => total++);

        subject.Emit();

        total.Should().Be(3);
    }

    [Fact]
    public void Emit_SubscriberThrows_ShouldContinueToNext()
    {
        var subject = new EffectSubject();
        int secondCount = 0;
        subject.Subscribe(() => throw new Exception("void-boom"));
        subject.Subscribe(() => secondCount++);

        var act = () => subject.Emit();
        act.Should().NotThrow();
        secondCount.Should().Be(1);
    }

    [Fact]
    public void Emit_AfterDispose_ShouldNotInvokeSubscribers()
    {
        var subject = new EffectSubject();
        int count = 0;
        subject.Subscribe(() => count++);

        subject.Dispose();
        subject.Emit();

        count.Should().Be(0);
    }

    [Fact]
    public void Subscribe_ThenDispose_ShouldStopReceiving()
    {
        var subject = new EffectSubject();
        int count = 0;
        var sub = subject.Subscribe(() => count++);

        subject.Emit();
        sub.Dispose();
        subject.Emit();

        count.Should().Be(1);
    }

    [Fact]
    public void Subscribe_AfterDisposed_ShouldReturnEmptyDisposable()
    {
        var subject = new EffectSubject();
        subject.Dispose();

        var sub = subject.Subscribe(() => { });
        sub.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_CalledTwice_ShouldNotThrow()
    {
        var subject = new EffectSubject();
        subject.Dispose();
        var act = () => subject.Dispose();
        act.Should().NotThrow();
    }

    // ─── Unit struct ─────────────────────────────────────────────────────────

    [Fact]
    public void Unit_Default_ShouldEqualDefault()
    {
        var u1 = Unit.Default;
        var u2 = Unit.Default;
        u1.Should().Be(u2);
    }
}
