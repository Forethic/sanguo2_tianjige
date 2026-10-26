using Tianjige.Core.Models;

namespace Tianjige.Core.Services;

/// <summary>
/// 武将培养潜力评价报告
/// </summary>
public sealed record GeneralPotentialInfo
{
    public required int GeneralId { get; init; }
    public required string GeneralName { get; init; }

    /// <summary>
    /// 综合培养潜力评分 (0 ~ 100)
    /// </summary>
    public required int OverallScore { get; init; }

    /// <summary>
    /// 潜力评级档位
    /// </summary>
    public required string Tier { get; init; }

    /// <summary>
    /// 印章勋章文案 (如: 【绝世·SSS】)
    /// </summary>
    public required string TierBadge { get; init; }

    /// <summary>
    /// 档位专属古风主题色 (十六进制颜色)
    /// </summary>
    public required string TierColor { get; init; }

    /// <summary>
    /// 星级展示 (如 5.0, 4.5, 4.0)
    /// </summary>
    public required double StarRating { get; init; }

    /// <summary>
    /// 技能解锁最高等级 (如 40, 38, 22)
    /// </summary>
    public required int MaxSkillLevel { get; init; }

    /// <summary>
    /// 后期大后期潜能分 (满分 35)
    /// </summary>
    public required int LateGameCapabilityScore { get; init; }

    /// <summary>
    /// 核心技能组品质分 (满分 35)
    /// </summary>
    public required int SkillKitQualityScore { get; init; }

    /// <summary>
    /// 基础属性与成长上限分 (满分 25)
    /// </summary>
    public required int AttributeGrowthScore { get; init; }

    /// <summary>
    /// 文武双修与特种兵权加分 (满分 5)
    /// </summary>
    public required int VersatilityScore { get; init; }

    /// <summary>
    /// 成长阶段评价 (如: "大后期绝顶", "后期栋梁", "中坚主力", "中期停滞", "早期绝学")
    /// </summary>
    public string GrowthStage { get; init; } = string.Empty;

    /// <summary>
    /// 成长详细解析 (如: "中期断档：技能于 Lv.28 封顶，后期无新技能")
    /// </summary>
    public string GrowthComment { get; init; } = string.Empty;

    /// <summary>
    /// 核心特色亮点标签
    /// </summary>
    public IReadOnlyList<string> HighlightBadges { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 军师参谋评语
    /// </summary>
    public string TacticalComment { get; init; } = string.Empty;
}

/// <summary>
/// 武将培养潜力评估权威计算引擎
/// 基于《三国群英传2》实战元数据（技能等级上限、伏兵/体疗/技力神技覆盖、文武双修属性）综合推演
/// </summary>
public static class GeneralPotentialService
{
    public static GeneralPotentialInfo Evaluate(GeneralStaticInfo general)
    {
        ArgumentNullException.ThrowIfNull(general);

        var skillTree = general.SkillTree ?? Array.Empty<SkillTreeNode>();

        // 1. 技能终局上限分 (0 ~ 35) - 依据群英2官方400武将原始数据严谨刻画（重点辨析28级中期断档）
        int maxSkillLevel = skillTree.Count > 0 ? skillTree.Max(s => (int)s.Level) : 1;
        var badges = new List<string>();

        string growthStage;
        string growthComment;
        int lateGameScore;

        if (maxSkillLevel >= 40)
        {
            lateGameScore = 32;
            growthStage = "大后期绝顶";
            growthComment = $"终极成长：技能上限高达 Lv.{maxSkillLevel}，大后期觉醒终极神技，群英巅峰。";
            badges.Add("Lv.40+绝学");
        }
        else if (maxSkillLevel >= 36)
        {
            lateGameScore = 26;
            growthStage = "后期栋梁";
            growthComment = $"持久成长：最高技能达 Lv.{maxSkillLevel}，中后期持续提升战力。";
            badges.Add("后期栋梁");
        }
        else if (maxSkillLevel >= 30)
        {
            lateGameScore = 18;
            growthStage = "中坚主力";
            growthComment = $"稳健成长：技能成长至 Lv.{maxSkillLevel}，实战主力骨干。";
        }
        else if (maxSkillLevel >= 26) // 重点涵盖 28 级停滞的 26+ 位武将
        {
            lateGameScore = 9;
            growthStage = "中期停滞";
            growthComment = $"中期断档：技能于 Lv.{maxSkillLevel} 封顶（28级前后即无新技能），后期培养潜力极低。";
            badges.Add("Lv.28停滞");
        }
        else
        {
            lateGameScore = 3;
            growthStage = "早期绝学";
            growthComment = $"早期封顶：最高技能仅 Lv.{maxSkillLevel}，20~25级后完全停滞，缺乏长期培养价值。";
            badges.Add("早期封顶");
        }

        int totalSkillsBonus = Math.Min(3, skillTree.Count / 3);
        lateGameScore = Math.Min(35, lateGameScore + totalSkillsBonus);

        // 2. 核心技能组品质分 (0 ~ 35)
        int skillKitScore = 0;

        bool hasAmbushMajor = skillTree.Any(s => s.SkillName.Contains("伏兵班阵") || s.SkillName.Contains("后伏班阵") || s.SkillName.Contains("伏兵连阵") || s.SkillName.Contains("后伏连阵"));
        bool hasAmbushMinor = skillTree.Any(s => s.SkillName.Contains("伏兵") || s.SkillName.Contains("后伏"));
        if (hasAmbushMajor)
        {
            skillKitScore += 14;
            badges.Add("精锐伏兵");
        }
        else if (hasAmbushMinor)
        {
            skillKitScore += 10;
            badges.Add("伏兵奇袭");
        }

        bool hasHpRegenMajor = skillTree.Any(s => s.SkillName.Contains("生疗术"));
        bool hasHpRegenMid = skillTree.Any(s => s.SkillName.Contains("体疗术"));
        bool hasHpRegenMinor = skillTree.Any(s => s.SkillName.Contains("命疗术"));
        if (hasHpRegenMajor)
        {
            skillKitScore += 12;
            badges.Add("生疗不灭");
        }
        else if (hasHpRegenMid)
        {
            skillKitScore += 9;
            badges.Add("体疗自愈");
        }
        else if (hasHpRegenMinor)
        {
            skillKitScore += 6;
            badges.Add("命疗续航");
        }

        bool hasMpRegen = skillTree.Any(s => s.SkillName.Contains("恢复技力"));
        bool hasMpPreserve = skillTree.Any(s => s.SkillName.Contains("保留气力"));
        bool hasFastCharge = skillTree.Any(s => s.SkillName.Contains("快速集气"));
        if (hasMpRegen)
        {
            skillKitScore += 10;
            badges.Add("技力源泉");
        }
        else if (hasMpPreserve || hasFastCharge)
        {
            skillKitScore += 6;
            badges.Add("法力聚气");
        }

        bool hasGodTierAttack = skillTree.Any(s => s.SkillName.Contains("鬼哭神号") || s.SkillName.Contains("天地无用"));
        bool hasTopTierAttack = skillTree.Any(s => s.SkillName.Contains("狂雷天牢") || s.SkillName.Contains("神鬼乱舞") || s.SkillName.Contains("烈火旋灯"));
        bool hasAoeWipe = skillTree.Any(s => s.SkillName.Contains("神火怒张") || s.SkillName.Contains("炎龙杀阵") || s.SkillName.Contains("分身斩") || s.SkillName.Contains("剑轮舞"));

        if (hasGodTierAttack)
        {
            skillKitScore += 12;
            badges.Add("灭世神技");
        }
        else if (hasTopTierAttack)
        {
            skillKitScore += 9;
            badges.Add("单挑强控");
        }
        else if (hasAoeWipe)
        {
            skillKitScore += 6;
            badges.Add("横扫千军");
        }

        bool hasCicada = skillTree.Any(s => s.SkillName.Contains("金蝉脱壳"));
        bool hasDiscord = skillTree.Any(s => s.SkillName.Contains("离间计"));
        bool hasSealMagic = skillTree.Any(s => s.SkillName.Contains("封武将技"));
        if (hasCicada)
        {
            skillKitScore += 6;
            badges.Add("绝命脱壳");
        }
        if (hasDiscord)
        {
            skillKitScore += 5;
            badges.Add("离间策反");
        }
        if (hasSealMagic)
        {
            skillKitScore += 6;
            badges.Add("封印奇谋");
        }

        skillKitScore = Math.Min(35, skillKitScore);

        // 3. 基础属性分 (0 ~ 25)
        // 兼容文武双全与文武专精（诸葛亮智力100或吕布武力100均为主宰级）
        int forceScore = general.BaseForce switch
        {
            >= 95 => 13,
            >= 88 => 11,
            >= 80 => 9,
            >= 70 => 6,
            >= 55 => 3,
            _ => 1
        };

        int intScore = general.BaseIntellect switch
        {
            >= 95 => 12,
            >= 88 => 10,
            >= 80 => 8,
            >= 70 => 6,
            >= 55 => 3,
            _ => 1
        };

        // 专精加成：智力>=95的顶级军师或武力>=95的顶级猛将，主属性翻倍核算
        int dominantScore = Math.Max(forceScore, intScore) * 2;
        int combinedScore = forceScore + intScore;
        int attributeScore = Math.Min(25, Math.Max(dominantScore, combinedScore));

        // 4. 文武双修与兵种特权分 (0 ~ 5)
        int versatilityScore = 0;
        if ((general.BaseForce >= 80 && general.BaseIntellect >= 70) ||
            (general.BaseForce >= 70 && general.BaseIntellect >= 80))
        {
            versatilityScore += 3;
            badges.Add("文武双全");
        }
        else if (general.BaseForce >= 90)
        {
            versatilityScore += 2;
            badges.Add("万人之敌");
        }
        else if (general.BaseIntellect >= 90)
        {
            versatilityScore += 2;
            badges.Add("算无遗策");
        }

        if (general.UnlockTroop.Contains("铁骑") || general.UnlockTroop.Contains("藤甲") || general.InitialTroop.Contains("铁骑"))
        {
            versatilityScore += 2;
            badges.Add("精锐骑盾");
        }

        versatilityScore = Math.Min(5, versatilityScore);

        // 总分与档位判定
        int overallScore = Math.Clamp(lateGameScore + skillKitScore + attributeScore + versatilityScore, 15, 100);

        string tier;
        string badge;
        string color;
        double star;
        string comment;

        if (overallScore >= 88)
        {
            tier = "SSS";
            badge = "【绝世·SSS】";
            color = "#FFD700"; // 金黄
            star = 5.0;
            comment = $"终极武将！技能等级上限高达Lv.{maxSkillLevel}，神技配置登峰造极，具备以一敌百之霸绝实力，定国安邦之柱石。";
        }
        else if (overallScore >= 76)
        {
            tier = "SS";
            badge = "【核心·SS】";
            color = "#FF7A30"; // 橙赤
            star = 4.5;
            comment = $"军团中坚砥柱！技能成长至Lv.{maxSkillLevel}，拥有强力控场或清兵杀着，无论征战攻城皆为不二首选。";
        }
        else if (overallScore >= 64)
        {
            tier = "S";
            badge = "【中坚·S】";
            color = "#3DA5D9"; // 靛蓝
            star = 4.0;
            comment = $"良才善战！技能兼具实用特色，成长至Lv.{maxSkillLevel}，可独当一面或任精锐副将协同突击。";
        }
        else if (overallScore >= 50)
        {
            tier = "A";
            badge = "【平庸·A】";
            color = "#8D99AE"; // 灰褐
            star = 3.0;
            comment = $"前期过渡将领。技能通常在Lv.{maxSkillLevel}前后断档，后期缺乏关键破局绝技，宜于守备城池或转为后勤运转。";
        }
        else
        {
            tier = "B";
            badge = "【驽钝·B】";
            color = "#6C757D"; // 墨灰
            star = 2.0;
            comment = $"潜质较低。技能上限仅Lv.{maxSkillLevel}，属性平平且无关键战略神技，仅供初期填补编制或巡城运粮。";
        }

        return new GeneralPotentialInfo
        {
            GeneralId = general.Id,
            GeneralName = general.Name,
            OverallScore = overallScore,
            Tier = tier,
            TierBadge = badge,
            TierColor = color,
            StarRating = star,
            MaxSkillLevel = maxSkillLevel,
            LateGameCapabilityScore = lateGameScore,
            SkillKitQualityScore = skillKitScore,
            AttributeGrowthScore = attributeScore,
            VersatilityScore = versatilityScore,
            GrowthStage = growthStage,
            GrowthComment = growthComment,
            HighlightBadges = badges.Take(4).ToList(),
            TacticalComment = comment
        };
    }
}
