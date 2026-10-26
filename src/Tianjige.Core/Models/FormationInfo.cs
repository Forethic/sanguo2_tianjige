namespace Tianjige.Core.Models;

/// <summary>
/// 兵种类别（三国群英传2三大兵系与特殊兵系）
/// </summary>
public enum TroopCategory
{
    Long = 1,    // 长兵器 (长枪/大刀/链锤/铁锤)
    Short = 2,   // 短兵器 (朴刀/武斗/蛮族/藤甲/女兵)
    Ranged = 3,  // 远程兵 (弓箭/飞刀/弩兵)
    Special = 4, // 特殊兵 (黄巾兵)
    Cavalry = 5  // 骑乘形态 (兼容历史扩展)
}

/// <summary>
/// 五行属性（保留兼容）
/// </summary>
public enum FiveElement
{
    Metal = 1,
    Wood = 2,
    Water = 3,
    Fire = 4,
    Earth = 5
}

/// <summary>
/// 兵种对阵胜率评分项
/// </summary>
public sealed record TroopAdvantageScore
{
    public required string Opponent { get; init; }
    public required int AdvantageScore { get; init; } // 对抗胜负指数（如 +66%, -42%）
    public string CategoryName { get; init; } = "";
    public bool IsAdvantage => AdvantageScore > 0;
    public string RatingText => AdvantageScore switch
    {
        >= 50 => "绝对完克",
        >= 25 => "显著大优",
        > 0 => "微弱小优",
        0 => "势均力敌",
        > -25 => "微弱小劣",
        > -50 => "显著劣势",
        _ => "惨遭碾压"
    };
    public string DisplayScore => AdvantageScore > 0 ? $"+{AdvantageScore}%" : $"{AdvantageScore}%";
}

/// <summary>
/// 兵种相克客观法则（三国群英传2 PAK 官方 13 大正规兵种）
/// </summary>
public sealed record TroopCounterInfo
{
    public required string TroopName { get; init; }
    public required TroopCategory Category { get; init; }

    public string CategoryName => Category switch
    {
        TroopCategory.Long => "长兵器",
        TroopCategory.Short => "短兵器",
        TroopCategory.Ranged => "远程兵",
        _ => "特殊兵"
    };

    public FiveElement Element { get; init; } = FiveElement.Metal;

    public string ElementName => Element switch
    {
        FiveElement.Metal => "金",
        FiveElement.Wood => "木",
        FiveElement.Water => "水",
        FiveElement.Fire => "火",
        _ => "土"
    };

    public string ElementBadge => $"【{ElementName}】";

    /// <summary>
    /// 优势克制兵种清单
    /// </summary>
    public IReadOnlyList<string> StrongAgainst { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 劣势被克兵种清单
    /// </summary>
    public IReadOnlyList<string> WeakAgainst { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 13x13 完整对抗绝对数值字典 (键: 敌方兵种名, 值: 我方对敌胜负差分 -68 ~ +68)
    /// </summary>
    public Dictionary<string, int> AdvantageMatrix { get; init; } = new();

    /// <summary>
    /// 兵种客观特性评价
    /// </summary>
    public string Characteristics { get; init; } = string.Empty;

    /// <summary>
    /// 兵家相克口诀秘籍
    /// </summary>
    public string ChantQuote { get; init; } = string.Empty;
}

/// <summary>
/// 八门阵型特性
/// </summary>
public sealed record FormationInfo
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string CenterGravity { get; init; } // 站位重心
    public required string Advantages { get; init; }    // 对抗优势
    public required string Disadvantages { get; init; } // 对抗劣势
    public required string Description { get; init; }   // 客观战术效用
}
