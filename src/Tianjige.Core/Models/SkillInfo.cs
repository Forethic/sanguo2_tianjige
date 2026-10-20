namespace Tianjige.Core.Models;

/// <summary>
/// 技能典籍信息
/// </summary>
public sealed record SkillInfo
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required SkillCategory Category { get; init; } // 1: 武将技, 2: 军师技
    public required byte MpCost { get; init; }
    public required string Description { get; init; }
    public string Tag { get; init; } = string.Empty; // 如: 清兵/单挑/控制/增益
    public string DamageDetail { get; init; } = string.Empty; // 伤害与效果数值细则
}
