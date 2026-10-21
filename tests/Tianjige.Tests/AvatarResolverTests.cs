using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Tianjige.Data.Models;
using Tianjige.Data.Repositories;
using Xunit;

namespace Tianjige.Tests;

public class AvatarResolverTests
{
    private static string FindAvatarsDir()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var avatarsDir = Path.Combine(current.FullName, "data", "avatars");
            if (Directory.Exists(avatarsDir))
            {
                return avatarsDir;
            }
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not find data/avatars directory");
    }

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

    [Theory]
    [InlineData("关羽")]
    [InlineData("關羽")]
    [InlineData("诸葛亮")]
    [InlineData("諸葛亮")]
    [InlineData("曹操")]
    [InlineData("刘备")]
    [InlineData("劉備")]
    [InlineData("赵云")]
    [InlineData("趙雲")]
    public void ResolveByName_ShouldResolve_FamousGenerals_BothSimplifiedAndTraditional(string generalName)
    {
        var avatarsDir = FindAvatarsDir();
        var resolver = new AvatarResolver(avatarsDir);

        var path = resolver.ResolveByName(generalName);

        path.Should().NotBeNull();
        File.Exists(path!).Should().BeTrue();
    }

    [Fact]
    public void ResolveByName_ShouldMatch_All400GeneralsInGeneralsJson_100Percent()
    {
        var dataDir = FindDataDir();
        var avatarsDir = FindAvatarsDir();
        var resolver = new AvatarResolver(avatarsDir);

        var json = File.ReadAllText(Path.Combine(dataDir, "Generals.json"), Encoding.UTF8);
        var generals = JsonSerializer.Deserialize<List<RawGeneralDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        generals.Should().NotBeNull();
        generals!.Should().HaveCount(400);

        var missing = new List<string>();
        foreach (var general in generals)
        {
            var path = resolver.ResolveByName(general.Name);
            if (path == null || !File.Exists(path))
            {
                missing.Add($"{general.Sequence}: {general.Name}");
            }
        }

        missing.Should().BeEmpty(because: "All 400 generals in Generals.json should have a matching avatar in data/avatars");
    }

    [Fact]
    public void ResolveByName_UnknownGeneral_ShouldReturnNull()
    {
        var avatarsDir = FindAvatarsDir();
        var resolver = new AvatarResolver(avatarsDir);

        var path = resolver.ResolveByName("不存在的无名氏");

        path.Should().BeNull();
    }
}
