namespace Tianjige.Core.Models;

/// <summary>
/// 宝物种类
/// </summary>
public enum ItemCategory
{
    Weapon = 1, // 神兵利刃
    Book = 2,   // 奇书兵法
    Horse = 3,  // 绝尘名驹
    Special = 4 // 灵丹异宝
}

/// <summary>
/// 宝物图鉴实体
/// </summary>
public sealed record ItemInfo
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required ItemCategory Category { get; init; }
    public sbyte ForceBonus { get; init; }
    public sbyte IntellectBonus { get; init; }
    public sbyte HpBonus { get; init; }
    public sbyte SpeedBonus { get; init; }
    public required string Description { get; init; }
    public string DefaultOwner { get; init; } = string.Empty; // 初始拥有者/出处
}
