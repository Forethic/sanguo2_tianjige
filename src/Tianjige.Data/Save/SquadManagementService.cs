using System.IO;
using System.Text.Json;
using Tianjige.Core.Models;
using Tianjige.Data.Memory.Models;
using Tianjige.Data.Repositories;

namespace Tianjige.Data.Save;

/// <summary>
/// 战将编组单体数据模型（每队上限 5 名主将/副将）
/// </summary>
public sealed class SquadModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "新编战队";
    public string LeaderName { get; set; } = string.Empty;
    public List<string> MemberNames { get; set; } = new();
    public string Notes { get; set; } = string.Empty;

    // 衍生分析指标
    public int TotalForce { get; set; }
    public double AverageForce { get; set; }
    public int TotalIntellect { get; set; }
    public double AverageIntellect { get; set; }
    public int TotalTroops { get; set; }
    public bool HasAmbush { get; set; }
    public bool HasHpRegen { get; set; }
    public bool HasMpRegen { get; set; }
    public bool HasDuelControl { get; set; }
    public string SynergyGrade { get; set; } = "A";
    public string SynergyComment { get; set; } = string.Empty;
}

/// <summary>
/// 全局理想编组与本存档实装编组运筹管理引擎
/// </summary>
public class SquadManagementService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _storageFolder;
    private readonly StaticRepository _repository;

    public SquadManagementService(StaticRepository? repository = null, string? storageFolder = null)
    {
        _repository = repository ?? new StaticRepository();

        if (string.IsNullOrWhiteSpace(storageFolder))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _storageFolder = Path.Combine(appData, "Tianjige", "Squads");
        }
        else
        {
            _storageFolder = storageFolder;
        }

        if (!Directory.Exists(_storageFolder))
        {
            try { Directory.CreateDirectory(_storageFolder); } catch { }
        }
    }

    /// <summary>
    /// 读取全局预设编队库（若不存在则自动生成经典五虎、五子、东吴都督等经典阵型模板）
    /// </summary>
    public List<SquadModel> LoadGlobalSquads()
    {
        var filePath = Path.Combine(_storageFolder, "global_squads.json");
        if (File.Exists(filePath))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<SquadModel>>(json, JsonOptions);
                if (list != null && list.Count > 0)
                {
                    foreach (var sq in list) RecalculateSquadMetrics(sq);
                    return list;
                }
            }
            catch { }
        }

        // 初始化默认经典全局编组大典
        var defaults = CreateDefaultGlobalSquads();
        foreach (var sq in defaults) RecalculateSquadMetrics(sq);
        SaveGlobalSquads(defaults);
        return defaults;
    }

    public void SaveGlobalSquads(List<SquadModel> squads)
    {
        var filePath = Path.Combine(_storageFolder, "global_squads.json");
        try
        {
            var json = JsonSerializer.Serialize(squads, JsonOptions);
            File.WriteAllText(filePath, json);
        }
        catch { }
    }

    /// <summary>
    /// 读取特定存档的实装编组配置
    /// </summary>
    public List<SquadModel> LoadSaveSquads(int slotNumber)
    {
        var filePath = Path.Combine(_storageFolder, $"save_squads_slot_{slotNumber:D2}.json");
        if (File.Exists(filePath))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var list = JsonSerializer.Deserialize<List<SquadModel>>(json, JsonOptions);
                if (list != null)
                {
                    foreach (var sq in list) RecalculateSquadMetrics(sq);
                    return list;
                }
            }
            catch { }
        }

        // 若存档编组尚无记录，克隆一份全局理想模版作为初始底稿
        var templates = LoadGlobalSquads();
        var clones = templates.Select(t => new SquadModel
        {
            Name = t.Name,
            LeaderName = t.LeaderName,
            MemberNames = new List<string>(t.MemberNames),
            Notes = $"[存档{slotNumber:D2}] 实装配置"
        }).ToList();

        foreach (var c in clones) RecalculateSquadMetrics(c);
        SaveSaveSquads(slotNumber, clones);
        return clones;
    }

    public void SaveSaveSquads(int slotNumber, List<SquadModel> squads)
    {
        var filePath = Path.Combine(_storageFolder, $"save_squads_slot_{slotNumber:D2}.json");
        try
        {
            var json = JsonSerializer.Serialize(squads, JsonOptions);
            File.WriteAllText(filePath, json);
        }
        catch { }
    }

    /// <summary>
    /// 依据群英传2名将元数据重新推演战队的均势战力、伏兵/恢复覆盖率与协同度
    /// </summary>
    public void RecalculateSquadMetrics(SquadModel squad, Dictionary<string, GeneralStaticInfo>? lookup = null)
    {
        if (squad == null) return;

        lookup ??= _repository.GetGenerals().ToDictionary(g => g.Name);

        var members = squad.MemberNames
            .Select(name => lookup.GetValueOrDefault(name))
            .Where(g => g != null)
            .Cast<GeneralStaticInfo>()
            .ToList();

        if (members.Count == 0)
        {
            squad.TotalForce = 0;
            squad.AverageForce = 0;
            squad.TotalIntellect = 0;
            squad.AverageIntellect = 0;
            squad.SynergyGrade = "B";
            squad.SynergyComment = "空编队，请吸纳名将入伍";
            return;
        }

        squad.TotalForce = members.Sum(m => (int)m.BaseForce);
        squad.AverageForce = Math.Round((double)squad.TotalForce / members.Count, 1);
        squad.TotalIntellect = members.Sum(m => (int)m.BaseIntellect);
        squad.AverageIntellect = Math.Round((double)squad.TotalIntellect / members.Count, 1);

        // 技能覆盖率
        squad.HasAmbush = members.Any(m => m.SkillTree.Any(s => s.SkillName.Contains("伏兵")));
        squad.HasHpRegen = members.Any(m => m.SkillTree.Any(s => s.SkillName.Contains("体疗") || s.SkillName.Contains("生疗") || s.SkillName.Contains("命疗") || s.SkillName.Contains("增加体力")));
        squad.HasMpRegen = members.Any(m => m.SkillTree.Any(s => s.SkillName.Contains("恢复技力")));
        squad.HasDuelControl = members.Any(m => m.SkillTree.Any(s => s.SkillName.Contains("落日弓") || s.SkillName.Contains("落月弓") || s.SkillName.Contains("御飞刀") || s.SkillName.Contains("鬼哭神号") || s.SkillName.Contains("神鬼乱舞")));
        var hasGodAoe = members.Any(m => m.SkillTree.Any(s => s.SkillName.Contains("天地狂雷") || s.SkillName.Contains("狂雷天牢") || s.SkillName.Contains("神剑闪华") || s.SkillName.Contains("黄龙天翔") || s.SkillName.Contains("炎龙怒涛") || s.SkillName.Contains("烈火旋灯")));
        var hasTactics = members.Any(m => m.SkillTree.Any(s => s.SkillName.Contains("金蝉脱壳") || s.SkillName.Contains("强化武将") || s.SkillName.Contains("封武将技") || s.SkillName.Contains("十面埋伏")));

        // 协同评级
        int score = 0;
        if (squad.AverageForce >= 90) score += 30;
        else if (squad.AverageForce >= 80) score += 20;
        else if (squad.AverageForce >= 70) score += 10;

        if (squad.AverageIntellect >= 90) score += 30;
        else if (squad.AverageIntellect >= 80) score += 25;
        else if (squad.AverageIntellect >= 70) score += 15;
        else if (squad.AverageIntellect >= 60) score += 10;

        // 奖励特化或文武兼备阵容（极致武力突击队 或 奇谋绝顶军师队）
        if (Math.Max(squad.AverageForce, squad.AverageIntellect) >= 85) score += 10;

        if (squad.HasAmbush) score += 20; // 伏兵系是群英2最强战术
        if (squad.HasHpRegen) score += 15;
        if (squad.HasMpRegen) score += 15; // 恢复技力极度强力
        if (hasGodAoe) score += 15;
        if (squad.HasDuelControl) score += 10;
        if (hasTactics) score += 10;

        // 武将资质均值加成
        var avgPotential = members.Average(m => m.Potential?.OverallScore ?? 50);
        if (avgPotential >= 85) score += 15;
        else if (avgPotential >= 75) score += 10;

        if (score >= 85)
        {
            squad.SynergyGrade = "无双·SSS";
            squad.SynergyComment = "绝顶神兵梯队：兼具灭军神技、持续续航与巅峰攻防，横扫千军";
        }
        else if (score >= 70)
        {
            squad.SynergyGrade = "精锐·SS";
            squad.SynergyComment = "铁血主力阵容：战术攻防均衡，实战破城斩将极具杀伤力";
        }
        else if (score >= 50)
        {
            squad.SynergyGrade = "中坚·S";
            squad.SynergyComment = "扎实中坚梯队：适合驻防要冲关隘或策应正面战场";
        }
        else
        {
            squad.SynergyGrade = "辅翼·A";
            squad.SynergyComment = "常规守备连队：缺乏关键神技支撑，宜作辅助轮换";
        }
    }

    /// <summary>
    /// 依据本档实装武将状态校验阵容齐整度
    /// </summary>
    public (int PresentCount, List<string> MissingGenerals) EvaluateSaveSquadReadiness(SquadModel squad, IEnumerable<LiveGeneralRecord> availableGenerals)
    {
        var activeNames = new HashSet<string>(availableGenerals.Select(g => g.Name));
        var missing = new List<string>();
        int present = 0;

        foreach (var member in squad.MemberNames)
        {
            if (activeNames.Contains(member)) present++;
            else missing.Add(member);
        }

        return (present, missing);
    }

    /// <summary>
    /// 自动将模板编队转化为当前存档所拥有的实装阵容
    /// </summary>
    public SquadModel FormSquadFromTemplate(SquadModel template, IEnumerable<LiveGeneralRecord> availableGenerals)
    {
        var activeNames = new HashSet<string>(availableGenerals.Select(g => g.Name));
        var saveSquad = new SquadModel
        {
            Name = template.Name,
            LeaderName = template.LeaderName,
            Notes = template.Notes
        };

        foreach (var name in template.MemberNames)
        {
            if (activeNames.Contains(name))
            {
                saveSquad.MemberNames.Add(name);
            }
        }

        RecalculateSquadMetrics(saveSquad);
        return saveSquad;
    }

    private static List<SquadModel> CreateDefaultGlobalSquads()
    {
        return new List<SquadModel>
        {
            new()
            {
                Name = "蜀汉·五虎上将",
                LeaderName = "关羽",
                MemberNames = new() { "关羽", "张飞", "赵云", "马超", "黄忠" },
                Notes = "天下至强武力突击集团，关张赵神鬼破阵"
            },
            new()
            {
                Name = "卧龙凤雏·奇门神机",
                LeaderName = "诸葛亮",
                MemberNames = new() { "诸葛亮", "庞统", "姜维", "魏延", "关平" },
                Notes = "天地狂雷、伏兵班阵、增员生疗无懈可击"
            },
            new()
            {
                Name = "魏武·五子良将",
                LeaderName = "张辽",
                MemberNames = new() { "张辽", "乐进", "于禁", "张郃", "徐晃" },
                Notes = "稳健行军，破阵斩将，攻守兼备之魏武前锋"
            },
            new()
            {
                Name = "江东·周郎火神",
                LeaderName = "周瑜",
                MemberNames = new() { "周瑜", "陆逊", "吕蒙", "太史慈", "甘宁" },
                Notes = "烈火旋灯、赤焰火海、绝命弓矢，火攻神域"
            },
            new()
            {
                Name = "天下至强·飞将鬼神",
                LeaderName = "吕布",
                MemberNames = new() { "吕布", "张辽", "高顺", "陈宫", "貂蝉" },
                Notes = "神鬼乱舞极限输出，单骑斩将无出其右"
            }
        };
    }
}
