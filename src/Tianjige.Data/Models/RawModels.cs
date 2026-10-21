using System.Text.Json.Serialization;

namespace Tianjige.Data.Models;

/// <summary>
/// data/Generals.json 原始武将档案 DTO
/// </summary>
public sealed class RawGeneralDto
{
    public int Sequence { get; set; }
    public int Face { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte Str { get; set; }
    public byte Int { get; set; }
    public byte BaseHp { get; set; }
    public byte BaseMp { get; set; }
    public int City { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Weapon { get; set; }
    public int Horse { get; set; }
    public int Thing { get; set; }
    public byte Loyal { get; set; }

    public List<RawSkillTreeItemDto> GeneralSkillTree { get; set; } = new();
    public List<RawSkillTreeItemDto> AdvisorSkillTree { get; set; } = new();
}

/// <summary>
/// 武将双技能树节点 DTO
/// </summary>
public sealed class RawSkillTreeItemDto
{
    public byte Level { get; set; }
    public int SkillId { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// data/GeneralSkills.json 原始武将技 DTO
/// </summary>
public sealed class RawGeneralSkillDto
{
    public int Sequence { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public byte Mp { get; set; }
    public int Power { get; set; }
    public int Attack { get; set; }
    public string Note { get; set; } = string.Empty;
}

/// <summary>
/// data/AdvisorSkills.json 原始军师技 DTO
/// </summary>
public sealed class RawAdvisorSkillDto
{
    public int Sequence { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Execute { get; set; }
    public int WiseLevel { get; set; }
    public int Level { get; set; }
}

/// <summary>
/// 装备与珍宝 DTO (Weapons.json, Horses.json, Books.json, Jewelry.json 通用)
/// </summary>
public sealed class RawItemDto
{
    public int Sequence { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public byte Level { get; set; }
    public sbyte Str { get; set; }
    public sbyte Int { get; set; }
    public sbyte Speed { get; set; }
    public sbyte MaxHp { get; set; }
    public sbyte MaxMp { get; set; }
    public sbyte Loyal { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// data/Monarchs.json 原始势力君主 DTO
/// </summary>
public sealed class RawMonarchDto
{
    public string Section { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int FlagNum { get; set; }
    public int Type { get; set; }
    public int Select { get; set; }
}

/// <summary>
/// data/Cities.json 原始城池字典 DTO
/// </summary>
public sealed class RawCityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// data/Cities_Scenario1-5.json 原始剧本城池初始状态 DTO
/// </summary>
public sealed class RawScenarioCityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Lord { get; set; }
    public int Chief { get; set; }
    public int Adviser { get; set; }
    public int Money { get; set; }
    public int People { get; set; }
    public int Defend { get; set; }
    public int Economics { get; set; }
    public int ReserveSoldier { get; set; }
}
