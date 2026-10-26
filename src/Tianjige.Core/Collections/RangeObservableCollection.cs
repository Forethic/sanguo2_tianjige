using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Tianjige.Core.Collections;

/// <summary>
/// 支持批量更新的高性能 ObservableCollection。
/// 将频繁的逐项 Add 触发的数百次 CollectionChanged 通知缩减为单次 Reset 事件，
/// 显著减少 UI 线程在数据加载与列表过滤时的重绘与布局遍历开销。
/// </summary>
public class RangeObservableCollection<T> : ObservableCollection<T>
{
    public RangeObservableCollection() { }

    public RangeObservableCollection(IEnumerable<T> collection) : base(collection) { }

    /// <summary>
    /// 一次性清空并批量填充新数据集，仅触发单次 Reset 通知
    /// </summary>
    public void ReplaceRange(IEnumerable<T> items)
    {
        CheckReentrancy();
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// 批量追加数据集，仅触发单次 Reset 通知
    /// </summary>
    public void AddRange(IEnumerable<T> items)
    {
        CheckReentrancy();
        foreach (var item in items)
        {
            Items.Add(item);
        }
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
