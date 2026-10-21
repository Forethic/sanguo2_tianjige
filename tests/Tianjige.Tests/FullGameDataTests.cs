using System.IO;
using FluentAssertions;
using Tianjige.Core.Models;
using Tianjige.Data.Repositories;
using Xunit;

namespace Tianjige.Tests;

public class FullGameDataTests
{
    private static string FindDataDir()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var dataDir = Path.Combine(current.FullName, "data");
            if (Directory.Exists(dataDir) && File.Exists(Path.Combine(dataDir, "Generals.json")))
            {
                return dataDir;
            }
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not find data directory");
    }

    [Fact]
    public void FullDataRepository_ShouldLoad_400Generals_177Skills_77Items()
    {
        var dataDir = FindDataDir();
        var repo = new StaticRepository(dataDir);

        var generals = repo.GetAllGenerals();
        generals.Should().HaveCount(400);

        var skills = repo.GetAllSkills();
        skills.Should().HaveCount(177); // 110 武将技 + 67 军师技

        var items = repo.GetAllItems();
        items.Should().HaveCount(77); // 34 武器 + 8 马匹 + 8 兵书 + 27 兵符珍宝
    }

    [Fact]
    public void FullDataRepository_KeyGenerals_ShouldHaveCorrectAttributesAndAvatar()
    {
        var dataDir = FindDataDir();
        var repo = new StaticRepository(dataDir);

        // 关羽
        var guanyu = repo.GetGeneralByName("关羽");
        guanyu.Should().NotBeNull();
        guanyu!.BaseForce.Should().Be(99);
        guanyu.SealTier.Should().Be("无双");
        guanyu.SkillTree.Should().Contain(s => s.SkillName == "鬼哭神号");

        var guanyuAvatar = repo.ResolveAvatarByName(guanyu.Name);
        guanyuAvatar.Should().NotBeNull();
        File.Exists(guanyuAvatar!).Should().BeTrue();

        // 诸葛亮
        var zhuge = repo.GetGeneralByName("诸葛亮");
        zhuge.Should().NotBeNull();
        zhuge!.BaseIntellect.Should().Be(100);
        zhuge.SealTier.Should().Be("谋主");

        var zhugeAvatar = repo.ResolveAvatarByName(zhuge.Name);
        zhugeAvatar.Should().NotBeNull();
        File.Exists(zhugeAvatar!).Should().BeTrue();
    }

    [Fact]
    public void FullDataRepository_All400Generals_ShouldHaveMatchingAvatar()
    {
        var dataDir = FindDataDir();
        var repo = new StaticRepository(dataDir);
        var generals = repo.GetAllGenerals();

        var missing = new List<string>();
        foreach (var general in generals)
        {
            var path = repo.ResolveAvatarByName(general.Name);
            if (path == null || !File.Exists(path))
            {
                missing.Add($"{general.Id}: {general.Name}");
            }
        }

        missing.Should().BeEmpty(because: "Every general among 400 must have an indexed avatar in data/avatars");
    }

    [Fact]
    public void FullDataRepository_TraditionalNameLookup_ShouldWork()
    {
        var dataDir = FindDataDir();
        var repo = new StaticRepository(dataDir);

        var guanyuTrad = repo.GetGeneralByName("關羽");
        var guanyuSimp = repo.GetGeneralByName("关羽");

        guanyuTrad.Should().NotBeNull();
        guanyuSimp.Should().NotBeNull();
        guanyuTrad!.Id.Should().Be(guanyuSimp!.Id);
    }
}
