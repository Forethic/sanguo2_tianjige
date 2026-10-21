using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Tianjige.Data.Models;
using Tianjige.Data.Repositories;
using Xunit;

namespace Tianjige.Tests;

public class RawDataTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
    public void GeneralsJson_ShouldDeserialize_400Items()
    {
        var dir = FindDataDir();
        var json = File.ReadAllText(Path.Combine(dir, "Generals.json"), Encoding.UTF8);
        var list = JsonSerializer.Deserialize<List<RawGeneralDto>>(json, JsonOptions);

        list.Should().NotBeNull();
        list!.Should().HaveCount(400);

        var first = list[0];
        first.Sequence.Should().Be(1);
        first.Name.Should().Be("丁奉");
        first.Str.Should().Be(80);
        first.GeneralSkillTree.Should().NotBeEmpty();
        first.AdvisorSkillTree.Should().NotBeEmpty();
    }

    [Fact]
    public void GeneralSkillsJson_ShouldDeserialize_110Items()
    {
        var dir = FindDataDir();
        var json = File.ReadAllText(Path.Combine(dir, "GeneralSkills.json"), Encoding.UTF8);
        var list = JsonSerializer.Deserialize<List<RawGeneralSkillDto>>(json, JsonOptions);

        list.Should().NotBeNull();
        list!.Should().HaveCount(110);
        list[0].Code.Should().Be("R001");
        list[0].Name.Should().Be("命療術");
    }

    [Fact]
    public void AdvisorSkillsJson_ShouldDeserialize_67Items()
    {
        var dir = FindDataDir();
        var json = File.ReadAllText(Path.Combine(dir, "AdvisorSkills.json"), Encoding.UTF8);
        var list = JsonSerializer.Deserialize<List<RawAdvisorSkillDto>>(json, JsonOptions);

        list.Should().NotBeNull();
        list!.Should().HaveCount(67);
        list[0].Name.Should().Be("鼓舞士氣(初)");
    }

    [Fact]
    public void ItemsJson_AllFourFiles_ShouldDeserialize()
    {
        var dir = FindDataDir();
        var weapons = JsonSerializer.Deserialize<List<RawItemDto>>(File.ReadAllText(Path.Combine(dir, "Weapons.json"), Encoding.UTF8), JsonOptions);
        var horses = JsonSerializer.Deserialize<List<RawItemDto>>(File.ReadAllText(Path.Combine(dir, "Horses.json"), Encoding.UTF8), JsonOptions);
        var books = JsonSerializer.Deserialize<List<RawItemDto>>(File.ReadAllText(Path.Combine(dir, "Books.json"), Encoding.UTF8), JsonOptions);
        var jewelry = JsonSerializer.Deserialize<List<RawItemDto>>(File.ReadAllText(Path.Combine(dir, "Jewelry.json"), Encoding.UTF8), JsonOptions);

        weapons.Should().NotBeNull();
        weapons!.Should().HaveCount(34);
        horses.Should().NotBeNull();
        horses!.Should().HaveCount(8);
        books.Should().NotBeNull();
        books!.Should().HaveCount(8);
        jewelry.Should().NotBeNull();
        jewelry!.Should().HaveCount(27);
    }

    [Fact]
    public void MonarchsJson_ShouldDeserialize_19Items()
    {
        var dir = FindDataDir();
        var monarchs = JsonSerializer.Deserialize<List<RawMonarchDto>>(File.ReadAllText(Path.Combine(dir, "Monarchs.json"), Encoding.UTF8), JsonOptions);
        monarchs.Should().NotBeNull();
        monarchs!.Should().HaveCount(19);
        monarchs[0].Section.Should().Be("KING01");
        monarchs[0].Name.Should().Be("何進");
    }

    [Fact]
    public void CitiesJson_ShouldDeserialize_60Items()
    {
        var dir = FindDataDir();
        var cities = JsonSerializer.Deserialize<List<RawCityDto>>(File.ReadAllText(Path.Combine(dir, "Cities.json"), Encoding.UTF8), JsonOptions);
        cities.Should().NotBeNull();
        cities!.Should().HaveCount(60);
        cities[0].Id.Should().Be(1);
        cities[0].Name.Should().Be("襄平");
        cities[59].Id.Should().Be(60);
        cities[59].Name.Should().Be("合肥");
    }

    [Fact]
    public void CitiesScenario_All5Files_ShouldDeserialize_60ItemsEach()
    {
        var dir = FindDataDir();
        for (int i = 1; i <= 5; i++)
        {
            var path = Path.Combine(dir, $"Cities_Scenario{i}.json");
            File.Exists(path).Should().BeTrue();
            var cities = JsonSerializer.Deserialize<List<RawScenarioCityDto>>(File.ReadAllText(path, Encoding.UTF8), JsonOptions);
            cities.Should().NotBeNull();
            cities!.Should().HaveCount(60);
            cities[0].Id.Should().Be(1);
            cities[0].Name.Should().Be("襄平");
        }
    }

    [Fact]
    public void GeneralsScenario_All5Files_ShouldDeserialize_400ItemsEach()
    {
        var dir = FindDataDir();
        for (int i = 1; i <= 5; i++)
        {
            var path = Path.Combine(dir, $"Generals_Scenario{i}.json");
            File.Exists(path).Should().BeTrue();
            var generals = JsonSerializer.Deserialize<List<RawGeneralDto>>(File.ReadAllText(path, Encoding.UTF8), JsonOptions);
            generals.Should().NotBeNull();
            generals!.Should().HaveCount(400);
            generals[0].Sequence.Should().Be(1);
            generals[0].Name.Should().Be("丁奉");
        }
    }

    [Fact]
    public void MonarchsScenario_All5Files_ShouldDeserialize()
    {
        var dir = FindDataDir();
        for (int i = 1; i <= 5; i++)
        {
            var path = Path.Combine(dir, $"Monarchs_Scenario{i}.json");
            File.Exists(path).Should().BeTrue();
            var monarchs = JsonSerializer.Deserialize<List<RawMonarchDto>>(File.ReadAllText(path, Encoding.UTF8), JsonOptions);
            monarchs.Should().NotBeNull();
            monarchs!.Should().NotBeEmpty();
        }
    }

    [Fact]
    public void StaticRepository_CitiesAndScenarios_ShouldLoadAccurately()
    {
        var dir = FindDataDir();
        var repo = new StaticRepository(dir);
        var cities = repo.GetAllCities();
        cities.Should().HaveCount(60);

        var xiangping = repo.GetCityById(1);
        xiangping.Should().NotBeNull();
        xiangping!.Name.Should().Be("襄平");

        var xuchang = repo.GetCityByName("许昌");
        xuchang.Should().NotBeNull();
        xuchang!.Id.Should().Be(19);

        // 验证 5 大剧本接口
        StaticRepository.Scenarios.Should().HaveCount(5);
        for (int i = 1; i <= 5; i++)
        {
            var sCities = repo.GetScenarioCities(i);
            sCities.Should().HaveCount(60);

            var sGenerals = repo.GetScenarioGenerals(i);
            sGenerals.Should().HaveCount(400);

            var sMonarchs = repo.GetScenarioMonarchs(i);
            sMonarchs.Should().NotBeEmpty();
        }

        // 验证各剧本特色君主
        var s1Monarchs = repo.GetScenarioMonarchs(1);
        s1Monarchs.Select(m => m.Name).Should().Contain(new[] { "何进", "张角", "丁原" });

        var s2Monarchs = repo.GetScenarioMonarchs(2);
        s2Monarchs.Select(m => m.Name).Should().Contain(new[] { "董卓", "曹操", "袁绍" });

        var s5Monarchs = repo.GetScenarioMonarchs(5);
        s5Monarchs.Select(m => m.Name).Should().Contain(new[] { "曹操", "刘备", "孙权", "孟获" });
    }
}
