using System.IO;
using System.Text;
using System.Text.Json;
using Tianjige.Data.Models;
using Tianjige.Data.Repositories;

namespace Tianjige.Data.Memory;

/// <summary>
/// 三国群英传2 全图 60 郡城关隘标准名称表（100% 对应游戏内存与 data/Cities.json）
/// </summary>
public static class SanguoCityTable
{
    private static readonly string[] DefaultCityNames = new[]
    {
        "野外 · 游历", // 0: 无所属城池，流浪在野中
        "襄平", "北平", "代县", "晋阳", "南皮", "平原", "邺城", "北海", "濮阳", "陈留",       // 1 - 10
        "洛阳", "弘农", "长安", "安定", "天水", "西凉", "下邳", "徐州", "许昌", "谯县",       // 11 - 20
        "汝南", "宛城", "新野", "襄阳", "上庸", "江夏", "江陵", "武陵", "长沙", "桂阳",       // 21 - 30
        "零陵", "寿春", "建业", "吴城", "会稽", "庐江", "豫章", "汉中", "下弁", "梓潼",       // 31 - 40
        "成都", "永安", "江州", "建宁", "云南", "庐陵", "鄱阳", "夷州", "易京", "乐陵",       // 41 - 50
        "界桥", "壶关", "定陶", "都阳", "官渡", "筑阳", "长坂", "建平", "赤壁", "合肥"        // 51 - 60
    };

    private static readonly string[] RuntimeCityNames;

    static SanguoCityTable()
    {
        RuntimeCityNames = (string[])DefaultCityNames.Clone();
        try
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "data", "Cities.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "data", "Cities.json"),
                Path.Combine(AppContext.BaseDirectory, "Cities.json")
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path, Encoding.UTF8);
                    var cities = JsonSerializer.Deserialize<List<RawCityDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (cities != null)
                    {
                        foreach (var c in cities)
                        {
                            if (c.Id >= 1 && c.Id < RuntimeCityNames.Length)
                            {
                                var simp = AvatarResolver.ToSimplified(c.Name);
                                if (simp == "予章") simp = "豫章";
                                if (simp == "鄴") simp = "邺城";
                                if (simp == "長板") simp = "长坂";
                                RuntimeCityNames[c.Id] = simp;
                            }
                        }
                    }
                    break;
                }
            }
        }
        catch { }
    }

    public static string GetCityName(int cityId)
    {
        if (cityId >= 0 && cityId < RuntimeCityNames.Length)
        {
            return RuntimeCityNames[cityId];
        }
        return cityId <= 0 ? "野外 · 游历" : $"郡城_{cityId}";
    }
}
