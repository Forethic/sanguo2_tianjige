using Tianjige.Data.Memory.Models;

namespace Tianjige.Data.Memory;

/// <summary>
/// 游戏只读内存扫描契约接口
/// </summary>
public interface IMemoryReader : IDisposable
{
    bool IsAttached { get; }
    int CurrentScenarioId { get; set; }
    bool TryAttach(int processId);
    bool TryGetSnapshot(out GameLiveSnapshot snapshot);
    void Detach();
}
