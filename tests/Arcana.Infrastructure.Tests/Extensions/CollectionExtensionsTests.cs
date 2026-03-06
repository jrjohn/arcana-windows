using Arcana.Core.Extensions;
using FluentAssertions;
using Xunit;

namespace Arcana.Infrastructure.Tests.Extensions;

public class CollectionExtensionsTests
{
    // ─── IsNullOrEmpty ───────────────────────────────────────────────────────

    [Fact]
    public void IsNullOrEmpty_NullCollection_ShouldReturnTrue()
    {
        IEnumerable<int>? source = null;
        source.IsNullOrEmpty().Should().BeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_EmptyCollection_ShouldReturnTrue()
    {
        var source = Enumerable.Empty<string>();
        source.IsNullOrEmpty().Should().BeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_NonEmptyCollection_ShouldReturnFalse()
    {
        var source = new[] { 1, 2, 3 };
        source.IsNullOrEmpty().Should().BeFalse();
    }

    [Fact]
    public void IsNullOrEmpty_SingleElement_ShouldReturnFalse()
    {
        var source = new[] { "hello" };
        source.IsNullOrEmpty().Should().BeFalse();
    }

    // ─── HasItems ────────────────────────────────────────────────────────────

    [Fact]
    public void HasItems_NullCollection_ShouldReturnFalse()
    {
        IEnumerable<int>? source = null;
        source.HasItems().Should().BeFalse();
    }

    [Fact]
    public void HasItems_EmptyCollection_ShouldReturnFalse()
    {
        Enumerable.Empty<string>().HasItems().Should().BeFalse();
    }

    [Fact]
    public void HasItems_NonEmptyCollection_ShouldReturnTrue()
    {
        new[] { 1, 2, 3 }.HasItems().Should().BeTrue();
    }

    [Fact]
    public void HasItems_SingleElement_ShouldReturnTrue()
    {
        new[] { "item" }.HasItems().Should().BeTrue();
    }

    [Fact]
    public void HasItems_IsInverseOfIsNullOrEmpty()
    {
        var sources = new IEnumerable<int>?[] { null, Array.Empty<int>(), new[] { 1 } };
        foreach (var src in sources)
        {
            src.HasItems().Should().Be(!src.IsNullOrEmpty());
        }
    }

    // ─── AddRange ────────────────────────────────────────────────────────────

    [Fact]
    public void AddRange_AddsAllItemsToCollection()
    {
        var list = new List<int> { 1, 2 };
        list.AddRange(new[] { 3, 4, 5 });
        list.Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact]
    public void AddRange_EmptyItems_ShouldNotChangeCollection()
    {
        var list = new List<string> { "a", "b" };
        list.AddRange(Array.Empty<string>());
        list.Should().Equal("a", "b");
    }

    [Fact]
    public void AddRange_ToEmptyCollection_ShouldAddAll()
    {
        var list = new List<int>();
        list.AddRange(new[] { 10, 20, 30 });
        list.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void AddRange_WorksWithHashSet()
    {
        var set = new HashSet<int> { 1, 2 };
        set.AddRange(new[] { 3, 4, 5 });
        set.Should().Contain([1, 2, 3, 4, 5]);
        set.Should().HaveCount(5);
    }

    // ─── OrEmpty ─────────────────────────────────────────────────────────────

    [Fact]
    public void OrEmpty_NullSource_ShouldReturnEmpty()
    {
        IEnumerable<int>? source = null;
        var result = source.OrEmpty();
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void OrEmpty_NonNullSource_ShouldReturnSame()
    {
        var source = new[] { 1, 2, 3 };
        var result = source.OrEmpty();
        result.Should().Equal(source);
    }

    [Fact]
    public void OrEmpty_EmptySource_ShouldReturnEmpty()
    {
        var source = Enumerable.Empty<string>();
        var result = source.OrEmpty();
        result.Should().BeEmpty();
    }

    // ─── ForEach ─────────────────────────────────────────────────────────────

    [Fact]
    public void ForEach_ExecutesActionForEachItem()
    {
        var items = new[] { 1, 2, 3 };
        var results = new List<int>();
        items.ForEach(i => results.Add(i * 2));
        results.Should().Equal(2, 4, 6);
    }

    [Fact]
    public void ForEach_EmptyCollection_ShouldNotExecuteAction()
    {
        int callCount = 0;
        Array.Empty<string>().ForEach(_ => callCount++);
        callCount.Should().Be(0);
    }

    [Fact]
    public void ForEach_SingleElement_ShouldExecuteOnce()
    {
        int count = 0;
        new[] { "x" }.ForEach(_ => count++);
        count.Should().Be(1);
    }

    [Fact]
    public void ForEach_PreservesOrder()
    {
        var order = new List<int>();
        new[] { 5, 3, 1, 4, 2 }.ForEach(i => order.Add(i));
        order.Should().Equal(5, 3, 1, 4, 2);
    }

    // ─── ForEachAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ForEachAsync_ExecutesActionForEachItem()
    {
        var items = new[] { 1, 2, 3 };
        var results = new List<int>();
        await items.ForEachAsync(async i =>
        {
            await Task.Yield();
            results.Add(i);
        });
        results.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task ForEachAsync_EmptyCollection_ShouldNotExecute()
    {
        int count = 0;
        await Array.Empty<int>().ForEachAsync(async _ =>
        {
            await Task.Yield();
            count++;
        });
        count.Should().Be(0);
    }

    // ─── Batch ───────────────────────────────────────────────────────────────

    [Fact]
    public void Batch_ExactMultiple_ShouldSplitEvenly()
    {
        var items = Enumerable.Range(1, 6);
        var batches = items.Batch(2).ToList();
        batches.Should().HaveCount(3);
        batches[0].Should().Equal(1, 2);
        batches[1].Should().Equal(3, 4);
        batches[2].Should().Equal(5, 6);
    }

    [Fact]
    public void Batch_RemainingItems_LastBatchShouldBeSmallerThanSize()
    {
        var items = Enumerable.Range(1, 7);
        var batches = items.Batch(3).ToList();
        batches.Should().HaveCount(3);
        batches[0].Should().Equal(1, 2, 3);
        batches[1].Should().Equal(4, 5, 6);
        batches[2].Should().Equal(7);
    }

    [Fact]
    public void Batch_EmptySource_ShouldReturnNoBatches()
    {
        var batches = Enumerable.Empty<int>().Batch(10).ToList();
        batches.Should().BeEmpty();
    }

    [Fact]
    public void Batch_BatchSizeLargerThanCollection_ShouldReturnSingleBatch()
    {
        var items = new[] { 1, 2, 3 };
        var batches = items.Batch(100).ToList();
        batches.Should().HaveCount(1);
        batches[0].Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Batch_BatchSizeOne_EachItemInOwnBatch()
    {
        var items = new[] { "a", "b", "c" };
        var batches = items.Batch(1).ToList();
        batches.Should().HaveCount(3);
        batches[0].Should().Equal("a");
        batches[1].Should().Equal("b");
        batches[2].Should().Equal("c");
    }

    [Fact]
    public void Batch_TotalItemCountPreserved()
    {
        var items = Enumerable.Range(1, 100);
        var batches = items.Batch(7).ToList();
        var total = batches.Sum(b => b.Count());
        total.Should().Be(100);
    }
}
