using FluentAssertions;
using Tianjige.Core.Models;
using Tianjige.Core.Services;
using Tianjige.Data.Repositories;
using Xunit;

namespace Tianjige.Tests;

public class GeneralPotentialServiceTests
{
    private readonly StaticRepository _repository = new();

    [Fact]
    public void Evaluate_ZhugeLiang_ShouldBeSSSGrade()
    {
        var zhuge = _repository.GetGeneralByName("诸葛亮");
        zhuge.Should().NotBeNull();

        var potential = GeneralPotentialService.Evaluate(zhuge!);
        potential.OverallScore.Should().BeGreaterThanOrEqualTo(88);
        potential.Tier.Should().Be("SSS");
        potential.TierBadge.Should().Contain("绝世·SSS");
        potential.MaxSkillLevel.Should().Be(40);
        potential.HighlightBadges.Should().Contain(b => b.Contains("狂雷") || b.Contains("技力") || b.Contains("神技") || b.Contains("遗策"));
    }

    [Fact]
    public void Evaluate_GuanYu_ShouldBeSSSGrade()
    {
        var guanyu = _repository.GetGeneralByName("关羽");
        guanyu.Should().NotBeNull();

        var potential = GeneralPotentialService.Evaluate(guanyu!);
        potential.OverallScore.Should().BeGreaterThanOrEqualTo(88);
        potential.Tier.Should().Be("SSS");
        potential.MaxSkillLevel.Should().Be(40);
        potential.HighlightBadges.Should().Contain(b => b.Contains("灭世") || b.Contains("神技") || b.Contains("文武"));
    }

    [Fact]
    public void Evaluate_LowPotentialGeneral_ScoresSignificantlyLower()
    {
        // 构造一个20级后就不学技能、无伏兵无恢复的低潜力武将
        var lowGeneral = new GeneralStaticInfo
        {
            Id = 999,
            Name = "平庸裨将",
            Avatar = "avatars/001.bmp",
            PinyinIndex = "pybj",
            SealTier = "裨将",
            BaseForce = 62,
            BaseIntellect = 48,
            SkillTree = new[]
            {
                new SkillTreeNode { Level = 1, SkillName = "落石", Category = SkillCategory.General, MpCost = 15, Description = "基础滚石" },
                new SkillTreeNode { Level = 8, SkillName = "飞矢", Category = SkillCategory.General, MpCost = 18, Description = "箭雨" },
                new SkillTreeNode { Level = 15, SkillName = "火雷", Category = SkillCategory.General, MpCost = 22, Description = "地雷" },
                new SkillTreeNode { Level = 20, SkillName = "突石", Category = SkillCategory.General, MpCost = 25, Description = "突石" }
            }
        };

        var potential = GeneralPotentialService.Evaluate(lowGeneral);
        potential.MaxSkillLevel.Should().Be(20);
        potential.LateGameCapabilityScore.Should().BeLessThanOrEqualTo(10);
        potential.OverallScore.Should().BeLessThan(60);
        potential.Tier.Should().BeOneOf("B", "A");
    }

    [Fact]
    public void SkillDetailDictionary_ProvidesRichDamageNumbers()
    {
        var fubin = SkillDetailDictionary.Enrich("伏兵班阵", "召唤士兵", "");
        fubin.DamageDetail.Should().Contain("50");

        var kuanglei = SkillDetailDictionary.Enrich("天地狂雷", "狂雷轰顶", "");
        kuanglei.DamageDetail.Should().Contain("全屏");

        var shengliao = SkillDetailDictionary.Enrich("生疗术", "恢复生命", "");
        shengliao.DamageDetail.Should().Contain("体力");
    }

    [Fact]
    public void Evaluate_Level28StagnationGeneral_ShouldBeMarkedAsMidGameStagnation()
    {
        // 模拟群英传2中28级封顶的武将（如原始数据中26位28级停滞武将）
        var midGeneral = new GeneralStaticInfo
        {
            Id = 888,
            Name = "及格偏将",
            Avatar = "avatars/002.bmp",
            PinyinIndex = "jgpj",
            SealTier = "良将",
            BaseForce = 72,
            BaseIntellect = 60,
            SkillTree = new[]
            {
                new SkillTreeNode { Level = 1, SkillName = "旋灯火", Category = SkillCategory.General, MpCost = 15, Description = "初级旋灯" },
                new SkillTreeNode { Level = 9, SkillName = "突石剑", Category = SkillCategory.General, MpCost = 18, Description = "突石" },
                new SkillTreeNode { Level = 18, SkillName = "火牛阵", Category = SkillCategory.General, MpCost = 22, Description = "火牛" },
                new SkillTreeNode { Level = 28, SkillName = "八面火", Category = SkillCategory.General, MpCost = 28, Description = "八面火" }
            }
        };

        var potential = GeneralPotentialService.Evaluate(midGeneral);
        potential.MaxSkillLevel.Should().Be(28);
        potential.GrowthStage.Should().Be("中期停滞");
        potential.GrowthComment.Should().Contain("28级前后即无新技能");
        potential.HighlightBadges.Should().Contain("Lv.28停滞");
        potential.LateGameCapabilityScore.Should().BeLessThanOrEqualTo(12);
    }
}
