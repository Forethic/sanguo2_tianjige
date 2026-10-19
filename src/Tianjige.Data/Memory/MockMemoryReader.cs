using Tianjige.Data.Memory.Models;
using Tianjige.Data.Repositories;

namespace Tianjige.Data.Memory;

/// <summary>
/// 用于离线推演、单元测试与演示的仿真内存采集器
/// </summary>
public class MockMemoryReader : IMemoryReader
{
    private readonly StaticRepository _repository;
    private bool _isAttached = true;

    public bool IsAttached => _isAttached;

    public MockMemoryReader(StaticRepository? repository = null)
    {
        _repository = repository ?? new StaticRepository();
    }

    public bool TryAttach(int processId)
    {
        _isAttached = true;
        return true;
    }

    public bool TryGetSnapshot(out GameLiveSnapshot snapshot)
    {
        var staticGenerals = _repository.GetAllGenerals();
        var generals = new List<LiveGeneralRecord>(400);

        for (int i = 0; i < 400; i++)
        {
            var s = i < staticGenerals.Count ? staticGenerals[i] : null;
            string name = s?.Name ?? $"群英_{i + 1}";
            byte force = s?.BaseForce ?? (byte)(60 + (i % 35));
            byte intellect = s?.BaseIntellect ?? (byte)(50 + (i % 45));

            int lordId;
            string lordName;
            GeneralStatus status = GeneralStatus.Active;
            byte loyalty = 90;
            int cityId = (i % 60) + 1;

            if (name == "关羽" || name == "张飞" || name == "诸葛亮")
            {
                lordId = 2; lordName = "刘备"; loyalty = 100; cityId = 4; // 襄阳
            }
            else if (name == "夏侯惇" || name == "张辽" || name == "曹操")
            {
                lordId = 1; lordName = "曹操"; loyalty = 96; cityId = 1; // 许昌
            }
            else if (name == "太史慈" || name == "周瑜")
            {
                lordId = 3; lordName = "孙权"; loyalty = 94; cityId = 5; // 建业
            }
            else if (i % 15 == 0)
            {
                // 流浪败逃武将
                status = GeneralStatus.Wandering;
                lordId = 0; lordName = "在野/流浪"; loyalty = 45; cityId = 0;
            }
            else if (i % 8 == 0)
            {
                // 在野名士
                status = GeneralStatus.Wild;
                lordId = 0; lordName = "在野"; loyalty = 60;
            }
            else
            {
                lordId = (i % 6) + 1;
                lordName = GetLordName(lordId);
                loyalty = (byte)(65 + (i * 7 % 35));
            }

            generals.Add(new LiveGeneralRecord
            {
                GeneralId = i + 1,
                Name = name,
                Force = force,
                Intellect = intellect,
                CurrentHp = 80 + (force % 30),
                MaxHp = 100,
                CurrentMp = 40 + (intellect % 40),
                MaxMp = 60,
                TroopCount = 80 + (i % 100),
                MaxTroopCount = 200,
                Loyalty = loyalty,
                Status = status,
                LordId = lordId,
                LordName = lordName,
                CityId = cityId,
                CityName = GetCityName(cityId),
                Level = (byte)(1 + (i % 25))
            });
        }

        var cities = new List<LiveCityRecord>(60);
        var undergroundMapping = new Dictionary<int, (int ItemId, string ItemName)>
        {
            [1] = (1, "方天画戟"),
            [2] = (2, "青龙偃月刀"),
            [3] = (3, "赤兔马"),
            [4] = (4, "兵法二十四篇"),
            [5] = (5, "倚天剑"),
            [6] = (6, "青囊书"),
            [7] = (7, "爪黄飞电"),
            [8] = (8, "的卢")
        };

        for (int i = 1; i <= 60; i++)
        {
            int lordId = (i % 7);
            undergroundMapping.TryGetValue(i, out var underground);

            cities.Add(new LiveCityRecord
            {
                CityId = i,
                Name = GetCityName(i),
                LordId = lordId,
                LordName = GetLordName(lordId),
                Defense = 300 + (i * 13 % 600),
                ReserveTroops = lordId > 0 ? (i * 37 % 650) : 0,
                GeneralCount = generals.Count(g => g.CityId == i && g.Status == GeneralStatus.Active),
                UndergroundItemId = underground.ItemId,
                UndergroundItemName = underground.ItemName ?? string.Empty
            });
        }

        snapshot = new GameLiveSnapshot
        {
            Generals = generals,
            Cities = cities,
            Year = 200,
            Month = 7,
            Day = 15,
            ScenarioId = 3,
            BattleState = 0
        };

        return true;
    }

    public void Detach()
    {
        _isAttached = false;
    }

    public void Dispose()
    {
        Detach();
    }

    private static string GetLordName(int lordId) => lordId switch
    {
        1 => "曹操",
        2 => "刘备",
        3 => "孙权",
        4 => "袁绍",
        5 => "刘表",
        6 => "马腾",
        _ => "无主/在野"
    };

    private static string GetCityName(int cityId) => cityId switch
    {
        1 => "许昌",
        2 => "洛阳",
        3 => "长安",
        4 => "襄阳",
        5 => "建业",
        6 => "成都",
        7 => "邺城",
        8 => "北平",
        9 => "下邳",
        10 => "汉中",
        11 => "汉寿",
        12 => "宛城",
        13 => "江陵",
        14 => "寿春",
        15 => "柴桑",
        _ => $"郡城_{cityId}"
    };
}
