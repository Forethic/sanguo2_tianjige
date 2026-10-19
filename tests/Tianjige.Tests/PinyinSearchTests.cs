using FluentAssertions;
using Tianjige.Core.Services;
using Tianjige.Data.Repositories;
using Xunit;

namespace Tianjige.Tests;

public class PinyinSearchTests
{
    private readonly StaticRepository _repository = new();

    [Theory]
    [InlineData("gy", "关羽")]
    [InlineData("tsc", "太史慈")]
    [InlineData("zgl", "诸葛亮")]
    [InlineData("lb", "吕布")]
    [InlineData("xhd", "夏侯惇")]
    [InlineData("zyu", "周瑜")]
    [InlineData("xdr", "邢道荣")]
    public void PinyinSearch_ShouldMatchExactGeneral(string pinyin, string expectedName)
    {
        var generals = _repository.GetAllGenerals();
        var results = PinyinSearchEngine.Search(generals, pinyin);

        results.Should().NotBeEmpty();
        results.First().Name.Should().Be(expectedName);
    }

    [Fact]
    public void ChineseNameSearch_ShouldMatchDirectly()
    {
        var generals = _repository.GetAllGenerals();
        var results = PinyinSearchEngine.Search(generals, "诸葛");

        results.Should().NotBeEmpty();
        results.First().Name.Should().Be("诸葛亮");
    }

    [Fact]
    public void EmptyQuery_ShouldReturnAllGenerals()
    {
        var generals = _repository.GetAllGenerals();
        var results = PinyinSearchEngine.Search(generals, "   ");

        results.Should().HaveCount(generals.Count);
    }

    [Fact]
    public void SkillInvertedIndex_ShouldReverseLookupCorrectly()
    {
        var generals = _repository.GetAllGenerals();
        var index = new SkillInvertedIndex(generals);

        // 验证谁掌握【鬼哭神号】
        var guikuMasters = index.GetMastersOfSkill("鬼哭神号");
        guikuMasters.Should().HaveCountGreaterThanOrEqualTo(3);
        guikuMasters.Select(m => m.GeneralName).Should().Contain(new[] { "关羽", "太史慈", "吕布" });
        guikuMasters.All(m => m.UnlockLevel == 40).Should().BeTrue();

        // 验证谁掌握【八门金锁】
        var bamenMasters = index.GetMastersOfSkill("八门金锁");
        bamenMasters.Should().NotBeEmpty();
        bamenMasters.First().GeneralName.Should().Be("诸葛亮");
        bamenMasters.First().UnlockLevel.Should().Be(6);

        // 验证谁掌握【烈火旋灯】
        var liehuoMasters = index.GetMastersOfSkill("烈火旋灯");
        liehuoMasters.Should().HaveCountGreaterThanOrEqualTo(4);
        // 周瑜在 Lv.8 解锁，应排在前面
        liehuoMasters.First().GeneralName.Should().Be("周瑜");
        liehuoMasters.First().UnlockLevel.Should().Be(8);
    }
}
