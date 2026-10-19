using FluentAssertions;
using Tianjige.Core.Models;
using Tianjige.Data.Repositories;
using Xunit;

namespace Tianjige.Tests;

public class StaticDataTests
{
    private readonly StaticRepository _repository = new();

    [Fact]
    public void Generals_ShouldBeLoaded_WithValidAttributes()
    {
        var generals = _repository.GetAllGenerals();
        generals.Should().NotBeEmpty();

        // 验证关羽
        var guanyu = _repository.GetGeneralByName("关羽");
        guanyu.Should().NotBeNull();
        guanyu!.BaseForce.Should().Be(98);
        guanyu.SealTier.Should().Be("无双");
        guanyu.SkillTree.Should().NotBeEmpty();
        guanyu.SkillTree.Should().Contain(s => s.SkillName == "鬼哭神号");

        // 验证诸葛亮
        var zhuge = _repository.GetGeneralByName("诸葛亮");
        zhuge.Should().NotBeNull();
        zhuge!.BaseIntellect.Should().Be(100);
        zhuge.SealTier.Should().Be("谋主");
        zhuge.SkillTree.Should().Contain(s => s.SkillName == "八门金锁");
    }

    [Fact]
    public void Skills_ShouldBeLoaded_WithValidCategoriesAndCosts()
    {
        var skills = _repository.GetAllSkills();
        skills.Should().NotBeEmpty();

        var guikush = _repository.GetSkillByName("鬼哭神号");
        guikush.Should().NotBeNull();
        guikush!.Category.Should().Be(SkillCategory.General);
        guikush.MpCost.Should().Be(50);

        var bamen = _repository.GetSkillByName("八门金锁");
        bamen.Should().NotBeNull();
        bamen!.Category.Should().Be(SkillCategory.Military);
        bamen.MpCost.Should().Be(30);
    }

    [Fact]
    public void Items_ShouldBeLoaded_WithCorrectBonuses()
    {
        var items = _repository.GetAllItems();
        items.Should().NotBeEmpty();

        var chitu = items.FirstOrDefault(i => i.Name == "赤兔马");
        chitu.Should().NotBeNull();
        chitu!.Category.Should().Be(ItemCategory.Horse);
        chitu.SpeedBonus.Should().Be(12);

        var fangtian = items.FirstOrDefault(i => i.Name == "方天画戟");
        fangtian.Should().NotBeNull();
        fangtian!.Category.Should().Be(ItemCategory.Weapon);
        fangtian.ForceBonus.Should().Be(10);
    }

    [Fact]
    public void FormationsAndCounters_ShouldMatchGameRules()
    {
        var formations = _repository.GetFormations();
        formations.Should().HaveCount(8);

        var troopCounters = _repository.GetTroopCounters();
        troopCounters.Should().NotBeEmpty();

        var pudao = troopCounters.FirstOrDefault(t => t.TroopName == "朴刀兵");
        pudao.Should().NotBeNull();
        pudao!.StrongAgainst.Should().Contain("弓箭兵");
        pudao.WeakAgainst.Should().Contain("铁骑兵");

        var changqiang = troopCounters.FirstOrDefault(t => t.TroopName == "长枪兵");
        changqiang.Should().NotBeNull();
        changqiang!.StrongAgainst.Should().Contain("铁骑兵");
        changqiang.WeakAgainst.Should().Contain("弓箭兵");
    }
}
