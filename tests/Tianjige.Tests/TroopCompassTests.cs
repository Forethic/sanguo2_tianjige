using FluentAssertions;
using Tianjige.Core.Models;
using Tianjige.Core.Services;
using Tianjige.Data.Repositories;
using Xunit;

namespace Tianjige.Tests;

public class TroopCompassTests
{
    private readonly StaticRepository _repository = new();
    private readonly TroopRelationService _service;

    public TroopCompassTests()
    {
        _service = new TroopRelationService(_repository.GetTroopCounters());
    }

    [Fact]
    public void GetRelation_WithSameTroop_ReturnsSelf()
    {
        var relation = _service.GetRelation("长枪兵", "长枪兵");
        relation.Should().Be(TroopRelation.Self);
    }

    [Fact]
    public void GetRelation_ChangqiangCountersPudao_AndCounteredByGongjian()
    {
        // 长枪(长)克朴刀(短)，但畏惧弓箭(远)
        _service.GetRelation("长枪兵", "朴刀兵").Should().Be(TroopRelation.Strong);
        _service.GetRelation("长枪兵", "弓箭兵").Should().Be(TroopRelation.Weak);
    }

    [Fact]
    public void GetRelation_PudaoCountersGongjian_AndCounteredByChangqiang()
    {
        // 朴刀(短)克弓箭(远)，但畏惧长枪(长)
        _service.GetRelation("朴刀兵", "弓箭兵").Should().Be(TroopRelation.Strong);
        _service.GetRelation("朴刀兵", "长枪兵").Should().Be(TroopRelation.Weak);
    }

    [Fact]
    public void GetRelation_SymmetricConsistency()
    {
        // 弓箭对朴刀为 Weak
        _service.GetRelation("弓箭兵", "朴刀兵").Should().Be(TroopRelation.Weak);
        // 朴刀对长枪为 Weak
        _service.GetRelation("朴刀兵", "长枪兵").Should().Be(TroopRelation.Weak);
    }

    [Fact]
    public void GetTroopAnalysis_ReturnsAccurateCounters()
    {
        var analysis = _service.AnalyzeTroop("长枪兵");
        analysis.Should().NotBeNull();
        analysis.Troop.TroopName.Should().Be("长枪兵");
        analysis.StrongAgainst.Should().Contain("朴刀兵");
        analysis.WeakAgainst.Should().Contain("弓箭兵");
    }

    [Fact]
    public void All13Troops_ShouldBeLoadedFromOfficialPak_WithoutCavalry()
    {
        var troops = _repository.GetTroopCounters();
        // 官方 PAK 原版 13 大正规兵种，绝无“铁骑兵”
        troops.Should().HaveCount(13);

        var names = troops.Select(t => t.TroopName).ToList();
        names.Should().NotContain("铁骑兵");
        names.Should().Contain(new[]
        {
            "长枪兵", "朴刀兵", "大刀兵", "弓箭兵", "链锤兵",
            "铁锤兵", "飞刀兵", "武斗兵", "蛮族兵", "藤甲兵",
            "女兵", "弩兵", "黄巾兵"
        });

        // 验证兵种系别分类
        troops.First(t => t.TroopName == "长枪兵").Category.Should().Be(TroopCategory.Long);
        troops.First(t => t.TroopName == "朴刀兵").Category.Should().Be(TroopCategory.Short);
        troops.First(t => t.TroopName == "弓箭兵").Category.Should().Be(TroopCategory.Ranged);
        troops.First(t => t.TroopName == "黄巾兵").Category.Should().Be(TroopCategory.Special);
        troops.First(t => t.TroopName == "女兵").Category.Should().Be(TroopCategory.Short);
        troops.First(t => t.TroopName == "弩兵").Category.Should().Be(TroopCategory.Ranged);
        troops.First(t => t.TroopName == "铁锤兵").Category.Should().Be(TroopCategory.Long);
    }

    [Fact]
    public void Sango2_ClassicRhymeCounters_MatchGameRules()
    {
        // 1. “大刀对女是短板”
        _service.GetRelation("女兵", "大刀兵").Should().Be(TroopRelation.Strong);
        _service.GetRelation("大刀兵", "女兵").Should().Be(TroopRelation.Weak);

        // 2. “飞刀爆的链锤惨”
        _service.GetRelation("飞刀兵", "链锤兵").Should().Be(TroopRelation.Strong);
        _service.GetRelation("链锤兵", "飞刀兵").Should().Be(TroopRelation.Weak);

        // 3. “弩兵全能很能干，碰到飞弓就完蛋”
        _service.GetRelation("弓箭兵", "弩兵").Should().Be(TroopRelation.Strong);
        _service.GetRelation("飞刀兵", "弩兵").Should().Be(TroopRelation.Strong);
        _service.GetRelation("弩兵", "弓箭兵").Should().Be(TroopRelation.Weak);

        // 4. “武斗不惧锤和缒”
        _service.GetRelation("武斗兵", "铁锤兵").Should().Be(TroopRelation.Strong);
        _service.GetRelation("武斗兵", "链锤兵").Should().Be(TroopRelation.Strong);

        // 5. “黄巾只克女飞蛮”
        _service.GetRelation("黄巾兵", "女兵").Should().Be(TroopRelation.Strong);
        _service.GetRelation("黄巾兵", "飞刀兵").Should().Be(TroopRelation.Strong);
        _service.GetRelation("黄巾兵", "蛮族兵").Should().Be(TroopRelation.Strong);
    }

    [Fact]
    public void Formations_ShouldContainAll9OfficialFormations()
    {
        var formations = _repository.GetFormations();
        formations.Should().HaveCount(9);

        var names = formations.Select(f => f.Name).ToList();
        names.Should().Contain(new[]
        {
            "方形阵", "圆形阵", "锥形阵", "雁形阵", "玄襄阵",
            "鱼丽阵", "钩形阵", "冲锋阵", "箭矢阵"
        });

        var fang = formations.FirstOrDefault(f => f.Name == "方形阵");
        fang.Should().NotBeNull();
        fang!.Advantages.Should().Contain("钩形");

        var yuan = formations.FirstOrDefault(f => f.Name == "圆形阵");
        yuan.Should().NotBeNull();
        yuan!.Advantages.Should().Contain("雁形");
    }
}
