using System.Collections.Specialized;
using FluentAssertions;
using Tianjige.Core.Collections;
using Xunit;

namespace Tianjige.Tests;

public class RangeObservableCollectionTests
{
    [Fact]
    public void ReplaceRange_ShouldFireSingleResetNotification()
    {
        var collection = new RangeObservableCollection<int> { 1, 2, 3 };
        int notificationCount = 0;
        NotifyCollectionChangedAction? lastAction = null;

        collection.CollectionChanged += (sender, e) =>
        {
            notificationCount++;
            lastAction = e.Action;
        };

        var newItems = Enumerable.Range(10, 400).ToList();
        collection.ReplaceRange(newItems);

        notificationCount.Should().Be(1, "400 items replaced in batch must trigger exactly 1 CollectionChanged event");
        lastAction.Should().Be(NotifyCollectionChangedAction.Reset);
        collection.Count.Should().Be(400);
        collection[0].Should().Be(10);
        collection[399].Should().Be(409);
    }

    [Fact]
    public void AddRange_ShouldFireSingleResetNotification()
    {
        var collection = new RangeObservableCollection<string> { "关羽" };
        int notificationCount = 0;
        NotifyCollectionChangedAction? lastAction = null;

        collection.CollectionChanged += (sender, e) =>
        {
            notificationCount++;
            lastAction = e.Action;
        };

        collection.AddRange(new[] { "张飞", "赵云", "马超", "黄忠" });

        notificationCount.Should().Be(1, "AddRange should batch fire only 1 Reset event");
        lastAction.Should().Be(NotifyCollectionChangedAction.Reset);
        collection.Count.Should().Be(5);
        collection.Should().ContainInOrder("关羽", "张飞", "赵云", "马超", "黄忠");
    }

    [Fact]
    public void StandardAdd_ShouldFireAddNotification()
    {
        var collection = new RangeObservableCollection<string>();
        NotifyCollectionChangedAction? lastAction = null;

        collection.CollectionChanged += (sender, e) =>
        {
            lastAction = e.Action;
        };

        collection.Add("诸葛亮");

        lastAction.Should().Be(NotifyCollectionChangedAction.Add);
        collection.Count.Should().Be(1);
    }
}
