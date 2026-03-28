using Arcana.Plugins.Services;
using FluentAssertions;
using Xunit;

namespace Arcana.Plugins.Tests.Services;

public class MessageBusTests
{
    private readonly MessageBus _bus;

    public MessageBusTests()
    {
        _bus = new MessageBus();
    }

    // Test message types
    private record PingMessage(string Text);
    private record PongMessage(string Reply);
    private record CounterMessage(int Value);

    // ─── PublishAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_NoSubscribers_ShouldNotThrow()
    {
        var act = async () => await _bus.PublishAsync(new PingMessage("hello"));
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_SyncSubscriber_ShouldReceiveMessage()
    {
        PingMessage? received = null;
        _bus.Subscribe<PingMessage>(m => received = m);

        await _bus.PublishAsync(new PingMessage("hi"));

        received.Should().NotBeNull();
        received!.Text.Should().Be("hi");
    }

    [Fact]
    public async Task PublishAsync_AsyncSubscriber_ShouldReceiveMessage()
    {
        PingMessage? received = null;
        _bus.Subscribe<PingMessage>(m =>
        {
            received = m;
            return Task.CompletedTask;
        });

        await _bus.PublishAsync(new PingMessage("async"));

        received.Should().NotBeNull();
        received!.Text.Should().Be("async");
    }

    [Fact]
    public async Task PublishAsync_MultipleSubscribers_ShouldInvokeAll()
    {
        var results = new List<string>();
        _bus.Subscribe<PingMessage>(m => results.Add("sync:" + m.Text));
        _bus.Subscribe<PingMessage>(m =>
        {
            results.Add("async:" + m.Text);
            return Task.CompletedTask;
        });

        await _bus.PublishAsync(new PingMessage("test"));

        results.Should().HaveCount(2);
        results.Should().Contain("sync:test");
        results.Should().Contain("async:test");
    }

    [Fact]
    public async Task PublishAsync_WrongMessageType_ShouldNotDeliverToOtherSubscriber()
    {
        PongMessage? received = null;
        _bus.Subscribe<PongMessage>(m => received = m);

        await _bus.PublishAsync(new PingMessage("ping"));

        received.Should().BeNull();
    }

    [Fact]
    public async Task PublishAsync_HandlerThrows_ShouldContinueToNextSubscriber()
    {
        int secondCalled = 0;
        _bus.Subscribe<PingMessage>(_ => throw new InvalidOperationException("boom"));
        _bus.Subscribe<PingMessage>(_ => secondCalled++);

        var act = async () => await _bus.PublishAsync(new PingMessage("test"));
        await act.Should().NotThrowAsync();
        secondCalled.Should().Be(1);
    }

    [Fact]
    public async Task PublishAsync_CancellationRequested_ShouldThrowOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _bus.Subscribe<PingMessage>(_ => { });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _bus.PublishAsync(new PingMessage("test"), cts.Token));
    }

    [Fact]
    public async Task PublishAsync_PublishMultipleTimes_ShouldDeliverEachTime()
    {
        int count = 0;
        _bus.Subscribe<PingMessage>(_ => count++);

        await _bus.PublishAsync(new PingMessage("1"));
        await _bus.PublishAsync(new PingMessage("2"));
        await _bus.PublishAsync(new PingMessage("3"));

        count.Should().Be(3);
    }

    // ─── Subscribe / Unsubscribe ─────────────────────────────────────────────

    [Fact]
    public void Subscribe_SyncHandler_ReturnsDisposable()
    {
        var sub = _bus.Subscribe<PingMessage>(_ => { });
        sub.Should().NotBeNull();
        sub.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void Subscribe_AsyncHandler_ReturnsDisposable()
    {
        var sub = _bus.Subscribe<PingMessage>(_ => Task.CompletedTask);
        sub.Should().NotBeNull();
        sub.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public async Task Subscribe_ThenDispose_ShouldStopReceivingMessages()
    {
        int count = 0;
        var sub = _bus.Subscribe<PingMessage>(_ => count++);

        await _bus.PublishAsync(new PingMessage("before"));
        sub.Dispose();
        await _bus.PublishAsync(new PingMessage("after"));

        count.Should().Be(1);
    }

    [Fact]
    public async Task Subscribe_DisposeOnlyRemovesOwnSubscription()
    {
        int sub1Count = 0;
        int sub2Count = 0;

        var sub1 = _bus.Subscribe<PingMessage>(_ => sub1Count++);
        _bus.Subscribe<PingMessage>(_ => sub2Count++);

        sub1.Dispose();
        await _bus.PublishAsync(new PingMessage("test"));

        sub1Count.Should().Be(0);
        sub2Count.Should().Be(1);
    }

    [Fact]
    public void Subscribe_DisposeCalledTwice_ShouldNotThrow()
    {
        var sub = _bus.Subscribe<PingMessage>(_ => { });
        sub.Dispose();
        var act = () => sub.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Subscribe_Multiple_ThenDisposeAll_ShouldReceiveNothing()
    {
        int count = 0;
        var subs = Enumerable.Range(0, 5)
            .Select(_ => _bus.Subscribe<PingMessage>(_ => count++))
            .ToList();

        foreach (var s in subs) s.Dispose();

        await _bus.PublishAsync(new PingMessage("test"));
        count.Should().Be(0);
    }

    [Fact]
    public async Task Subscribe_TwoDifferentMessageTypes_EachOnlyReceivesOwn()
    {
        PingMessage? receivedPing = null;
        PongMessage? receivedPong = null;

        _bus.Subscribe<PingMessage>(m => receivedPing = m);
        _bus.Subscribe<PongMessage>(m => receivedPong = m);

        await _bus.PublishAsync(new PingMessage("ping-only"));

        receivedPing.Should().NotBeNull();
        receivedPong.Should().BeNull();
    }

    // ─── RequestAsync / RegisterHandler ─────────────────────────────────────

    [Fact]
    public async Task RequestAsync_NoHandler_ShouldReturnNull()
    {
        var result = await _bus.RequestAsync<PingMessage, PongMessage>(new PingMessage("test"));
        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterHandler_ThenRequest_ShouldReturnResponse()
    {
        _bus.RegisterHandler<PingMessage, PongMessage>(req =>
            Task.FromResult<PongMessage>(new PongMessage("pong:" + req.Text)));

        var result = await _bus.RequestAsync<PingMessage, PongMessage>(new PingMessage("hello"));

        result.Should().NotBeNull();
        result!.Reply.Should().Be("pong:hello");
    }

    [Fact]
    public async Task RegisterHandler_ThenDispose_ShouldReturnNull()
    {
        var handler = _bus.RegisterHandler<PingMessage, PongMessage>(
            req => Task.FromResult<PongMessage>(new PongMessage("pong")));

        handler.Dispose();

        var result = await _bus.RequestAsync<PingMessage, PongMessage>(new PingMessage("test"));
        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterHandler_Timeout_ShouldReturnDefault()
    {
        _bus.RegisterHandler<PingMessage, PongMessage>(async req =>
        {
            await Task.Delay(5000); // much longer than timeout
            return new PongMessage("too-late");
        });

        var result = await _bus.RequestAsync<PingMessage, PongMessage>(
            new PingMessage("test"),
            timeout: TimeSpan.FromMilliseconds(50));

        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterHandler_WithTimeout_ShouldReturnResponse_WhenFast()
    {
        _bus.RegisterHandler<PingMessage, PongMessage>(
            req => Task.FromResult<PongMessage>(new PongMessage("fast")));

        var result = await _bus.RequestAsync<PingMessage, PongMessage>(
            new PingMessage("test"),
            timeout: TimeSpan.FromSeconds(5));

        result.Should().NotBeNull();
        result!.Reply.Should().Be("fast");
    }

    // ─── Thread safety ───────────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_FromMultipleThreadsConcurrently_ShouldNotThrow()
    {
        int count = 0;
        _bus.Subscribe<CounterMessage>(m => Interlocked.Add(ref count, m.Value));

        var tasks = Enumerable.Range(0, 50)
            .Select(i => Task.Run(() => _bus.PublishAsync(new CounterMessage(1))))
            .ToArray();

        var act = async () => await Task.WhenAll(tasks);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Subscribe_ConcurrentlyFromMultipleThreads_ShouldNotThrow()
    {
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => _bus.Subscribe<CounterMessage>(_ => { })))
            .ToArray();

        var act = async () => await Task.WhenAll(tasks);
        await act.Should().NotThrowAsync();
    }
}
