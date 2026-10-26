namespace Tianjige.UI.ViewModels;

// 命名空间类型转发，保证 UI 与 Core 全局兼容
public class RangeObservableCollection<T> : Tianjige.Core.Collections.RangeObservableCollection<T>
{
    public RangeObservableCollection() { }
    public RangeObservableCollection(System.Collections.Generic.IEnumerable<T> collection) : base(collection) { }
}
