using Arcana.Plugins.Contracts;
using Arcana.Plugins.Services;
using FluentAssertions;
using Xunit;

namespace Arcana.Plugins.Tests.Services;

public class EventAggregatorTests
{
    private readonly EventAggregator _aggregator;

    public EventAggregatorTests()
    {
        _aggregator = new EventAggregator();
    }

    // Concrete test event type
    private record TestEvent(string Message) : ApplicationEventBase;
    private record AnotherEvent(int Value) : ApplicationEventBase;

    [Fact]
    public void Subscribe_ThenPublish_ShouldInvokeHandler()
    {
        TestEvent? received = null;
        _aggregator.Subscribe<TestEvent>(e => received = e);

        var @event = new TestEvent("Hello");
        _aggregator.Publish(@event);

        received.Should().Be(@event);
    }

    [Fact]
    public void Publish_NoSubscribers_ShouldNotThrow()
    {
        var act = () => _aggregator.Publish(new TestEvent("ignored"));
        act.Should().NotThrow();
    }

    [Fact]
    public void Publish_MultipleSubscribers_ShouldInvokeAll()
    {
        var received = new List<string>();
        _aggregator.Subscribe<TestEvent>(e => received.Add("sub1:" + e.Message));
        _aggregator.Subscribe<TestEvent>(e => received.Add("sub2:" + e.Message));

        _aggregator.Publish(new TestEvent("Hello"));

        received.Should().HaveCount(2);
        received.Should().Contain("sub1:Hello");
        received.Should().Contain("sub2:Hello");
    }

    [Fact]
    public void Subscribe_ReturnsDisposable()
    {
        var subscription = _aggregator.Subscribe<TestEvent>(_ => { });
        subscription.Should().NotBeNull();
        subscription.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void Dispose_Subscription_ShouldUnsubscribe()
    {
        int callCount = 0;
        var subscription = _aggregator.Subscribe<TestEvent>(_ => callCount++);

        _aggregator.Publish(new TestEvent("first")); // should count
        subscription.Dispose();
        _aggregator.Publish(new TestEvent("second")); // should NOT count

        callCount.Should().Be(1);
    }

    [Fact]
    public void Dispose_CalledTwice_ShouldNotThrow()
    {
        var subscription = _aggregator.Subscribe<TestEvent>(_ => { });
        subscription.Dispose();
        var act = () => subscription.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_OnlyRemovesSpecificSubscription()
    {
        int sub1Count = 0;
        int sub2Count = 0;

        var sub1 = _aggregator.Subscribe<TestEvent>(_ => sub1Count++);
        _aggregator.Subscribe<TestEvent>(_ => sub2Count++);

        sub1.Dispose();
        _aggregator.Publish(new TestEvent("test"));

        sub1Count.Should().Be(0);
        sub2Count.Should().Be(1);
    }

    [Fact]
    public void Publish_DifferentEventTypes_ShouldOnlyTriggerCorrectHandlers()
    {
        TestEvent? receivedTest = null;
        AnotherEvent? receivedAnother = null;

        _aggregator.Subscribe<TestEvent>(e => receivedTest = e);
        _aggregator.Subscribe<AnotherEvent>(e => receivedAnother = e);

        _aggregator.Publish(new TestEvent("test"));

        receivedTest.Should().NotBeNull();
        receivedAnother.Should().BeNull();
    }

    [Fact]
    public void Publish_HandlerThrows_ShouldContinueWithOtherSubscribers()
    {
        int successCount = 0;

        _aggregator.Subscribe<TestEvent>(_ => throw new InvalidOperationException("handler failed"));
        _aggregator.Subscribe<TestEvent>(_ => successCount++);

        var act = () => _aggregator.Publish(new TestEvent("test"));
        act.Should().NotThrow();
        successCount.Should().Be(1);
    }

    [Fact]
    public void Subscribe_AfterFirstPublish_ShouldOnlyReceiveFutureEvents()
    {
        var received = new List<string>();

        _aggregator.Publish(new TestEvent("before-subscribe"));
        _aggregator.Subscribe<TestEvent>(e => received.Add(e.Message));
        _aggregator.Publish(new TestEvent("after-subscribe"));

        received.Should().ContainSingle("after-subscribe");
    }

    [Fact]
    public void Publish_ConcurrentSubscribersModification_ShouldNotThrow()
    {
        // Subscribe during publish (simulates concurrent modification)
        _aggregator.Subscribe<TestEvent>(_ =>
        {
            // Subscribing from inside a handler
            _aggregator.Subscribe<TestEvent>(_ => { });
        });

        var act = () => _aggregator.Publish(new TestEvent("concurrent"));
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Publish_FromMultipleThreads_ShouldNotThrow()
    {
        _aggregator.Subscribe<TestEvent>(_ => { });

        var tasks = Enumerable.Range(0, 20).Select(i =>
            Task.Run(() => _aggregator.Publish(new TestEvent($"event-{i}"))));

        var act = async () => await Task.WhenAll(tasks);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void MultipleSubscribeDispose_ForSameType_ShouldWork()
    {
        int count = 0;
        var subs = Enumerable.Range(0, 5)
            .Select(_ => _aggregator.Subscribe<TestEvent>(_ => count++))
            .ToList();

        _aggregator.Publish(new TestEvent("test"));
        count.Should().Be(5);

        foreach (var sub in subs) sub.Dispose();
        count = 0;
        _aggregator.Publish(new TestEvent("test"));
        count.Should().Be(0);
    }

    [Fact]
    public void Event_Timestamp_ShouldBeSetApproximatelyToNow()
    {
        var before = DateTime.UtcNow;
        var @event = new TestEvent("ts-test");
        var after = DateTime.UtcNow;

        @event.Timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(after.AddSeconds(1));
    }
}
