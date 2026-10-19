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
    public void GetRelation_ChangqiangCountersTieqi_AndCounteredByGongjian()
    {
        _service.GetRelation("长枪兵", "铁骑兵").Should().Be(TroopRelation.Strong);
        _service.GetRelation("长枪兵", "弓箭兵").Should().Be(TroopRelation.Weak);
    }

    [Fact]
    public void GetRelation_PudaoCountersGongjian_AndCounteredByTieqi()
    {
        _service.GetRelation("朴刀兵", "弓箭兵").Should().Be(TroopRelation.Strong);
        _service.GetRelation("朴刀兵", "铁骑兵").Should().Be(TroopRelation.Weak);
    }

    [Fact]
    public void GetRelation_SymmetricConsistency()
    {
        // 铁骑兵对长枪兵应为 Weak
        _service.GetRelation("铁骑兵", "长枪兵").Should().Be(TroopRelation.Weak);
        // 弓箭兵对朴刀兵应为 Weak
        _service.GetRelation("弓箭兵", "朴刀兵").Should().Be(TroopRelation.Weak);
    }

    [Fact]
    public void GetTroopAnalysis_ReturnsAccurateCounters()
    {
        var analysis = _service.AnalyzeTroop("长枪兵");
        analysis.Should().NotBeNull();
        analysis.Troop.TroopName.Should().Be("长枪兵");
        analysis.StrongAgainst.Should().Contain("铁骑兵");
        analysis.WeakAgainst.Should().Contain("弓箭兵");
    }

    [Fact]
    public void Formations_ShouldContainAll8ClassicFormations()
    {
        var formations = _repository.GetFormations();
        formations.Should().HaveCount(8);

        var fang = formations.FirstOrDefault(f => f.Name == "方形阵");
        fang.Should().NotBeNull();
        fang!.CenterGravity.Should().NotBeEmpty();

        var chong = formations.FirstOrDefault(f => f.Name == "冲阵");
        chong.Should().NotBeNull();
        chong!.Advantages.Should().Contain("一字阵");
    }
}
