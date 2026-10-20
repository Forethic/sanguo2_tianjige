namespace Tianjige.Core.Models;

/// <summary>
/// 武将全周期技能树节点
/// </summary>
public sealed record SkillTreeNode
{
    public required byte Level { get; init; }
    public required string SkillName { get; init; }
    public required SkillCategory Category { get; init; }
    public required byte MpCost { get; init; }
    public required string Description { get; init; }
    public string DamageDetail { get; init; } = string.Empty; // 伤害/杀伤/效果点数 (如: "对将伤害: 12点 | 清兵: 5~8人")
}

/// <summary>
/// 技能类别
/// </summary>
public enum SkillCategory
{
    General = 1, // 武将技 (赤红)
    Military = 2 // 军师技 (幽蓝)
}

/// <summary>
/// 武将静态档案（不可变客观规律）
/// </summary>
public sealed record GeneralStaticInfo
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string PinyinIndex { get; init; }
    public required string SealTier { get; init; } // 【无双】/【良将】/【谋主】/【裨将】/【白身】
    public required byte BaseForce { get; init; }
    public required byte BaseIntellect { get; init; }
    public string Comment { get; init; } = string.Empty;
    public string InitialTroop { get; init; } = string.Empty;
    public string UnlockTroop { get; init; } = string.Empty;
    public IReadOnlyList<SkillTreeNode> SkillTree { get; init; } = Array.Empty<SkillTreeNode>();
}
