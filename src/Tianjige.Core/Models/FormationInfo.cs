namespace Tianjige.Core.Models;

/// <summary>
/// 兵种类别
/// </summary>
public enum TroopCategory
{
    Short = 1,   // 短兵系 (朴刀/藤甲)
    Long = 2,    // 长兵系 (长枪/大刀)
    Ranged = 3,  // 远兵系 (弓箭/飞刀/弩兵)
    Cavalry = 4, // 骑兵系 (铁骑/重骑)
    Special = 5  // 特殊系 (武斗/蛮族)
}

/// <summary>
/// 兵种相克客观法则
/// </summary>
public sealed record TroopCounterInfo
{
    public required string TroopName { get; init; }
    public required TroopCategory Category { get; init; }
    public IReadOnlyList<string> StrongAgainst { get; init; } = Array.Empty<string>(); // 极大克制
    public IReadOnlyList<string> WeakAgainst { get; init; } = Array.Empty<string>();   // 被其克制
    public string Characteristics { get; init; } = string.Empty;                       // 兵种客观特性
}

/// <summary>
/// 八门阵型特性
/// </summary>
public sealed record FormationInfo
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string CenterGravity { get; init; } // 站位重心 (如: 前列密集、两翼包夹、中央突破)
    public required string Advantages { get; init; }    // 对抗优势
    public required string Disadvantages { get; init; } // 对抗劣势
    public required string Description { get; init; }   // 客观战术效用
}
