using System.Runtime.InteropServices;

namespace Tianjige.Data.Memory.Models;

/// <summary>
/// 武将当前出仕状态
/// </summary>
public enum GeneralStatus : byte
{
    Active = 0,     // 正常出仕
    Captured = 1,   // 遭俘虏
    Wild = 2,       // 在野名士
    Dead = 3,       // 阵亡英雄冢
    Wandering = 4   // 残血逃亡/败退流浪中
}

/// <summary>
/// x86 武将原始紧凑内存布局 (400 武将数组单体)
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LiveGeneralMemoryRaw
{
    public ushort GeneralId;
    public byte Force;
    public byte Intellect;
    public ushort CurrentHp;
    public ushort MaxHp;
    public byte CurrentMp;
    public byte MaxMp;
    public ushort TroopCount;
    public ushort MaxTroopCount;
    public byte Loyalty;
    public byte Status;
    public byte LordId;
    public byte CityId;
    public byte TroopType;
    public byte Weapon;
    public byte Horse;
    public byte Book;
    public byte Level;
    public uint Exp;
}

/// <summary>
/// x86 城池原始紧凑内存布局 (60 城池数组单体)
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LiveCityMemoryRaw
{
    public byte CityId;
    public byte LordId;
    public ushort Defense;
    public ushort ReserveTroops;
    public byte GeneralCount;
    public byte UndergroundItemId;
    public uint Money;
}

/// <summary>
/// 武将实时高层表现实体
/// </summary>
public sealed record LiveGeneralRecord
{
    public required int GeneralId { get; init; }
    public required string Name { get; init; }
    public required byte Force { get; init; }
    public required byte Intellect { get; init; }
    public required int CurrentHp { get; init; }
    public required int MaxHp { get; init; }
    public required int CurrentMp { get; init; }
    public required int MaxMp { get; init; }
    public required int TroopCount { get; init; }
    public required int MaxTroopCount { get; init; }
    public required byte Loyalty { get; init; }
    public required GeneralStatus Status { get; init; }
    public string StatusDisplayName => Status switch
    {
        GeneralStatus.Wild => "在野",
        GeneralStatus.Wandering => "流浪",
        GeneralStatus.Captured => "被俘",
        GeneralStatus.Dead => "阵亡",
        _ => "出仕"
    };
    public required int LordId { get; init; }
    public required string LordName { get; init; }
    public required int CityId { get; init; }
    public required string CityName { get; init; }
    public required byte Level { get; init; }
    public string WeaponName { get; init; } = string.Empty;
    public string HorseName { get; init; } = string.Empty;
    public string BookName { get; init; } = string.Empty;
}

/// <summary>
/// 城池实时高层表现实体
/// </summary>
public sealed record LiveCityRecord
{
    public required int CityId { get; init; }
    public required string Name { get; init; }
    public required int LordId { get; init; }
    public required string LordName { get; init; }
    public required int Defense { get; init; }
    public required int ReserveTroops { get; init; }
    public required int GeneralCount { get; init; }
    public required int UndergroundItemId { get; init; }
    public string UndergroundItemName { get; init; } = string.Empty;
}

/// <summary>
/// 地脉埋藏神兵宝物点位
/// </summary>
public sealed record UndergroundItemSpot
{
    public required int ItemId { get; init; }
    public required string ItemName { get; init; }
    public required int CityId { get; init; }
    public required string CityName { get; init; }
    public required string Description { get; init; }
}

/// <summary>
/// 全图实时战局全量只读快照
/// </summary>
public sealed record GameLiveSnapshot
{
    public IReadOnlyList<LiveGeneralRecord> Generals { get; init; } = Array.Empty<LiveGeneralRecord>();
    public IReadOnlyList<LiveCityRecord> Cities { get; init; } = Array.Empty<LiveCityRecord>();
    public ushort Year { get; init; } = 200;
    public byte Month { get; init; } = 7;
    public byte Day { get; init; } = 1;
    public byte ScenarioId { get; init; } = 3;
    public byte BattleState { get; init; } = 0; // 0=大地图, 1=内政, 2=战前选将老虎机, 3=战斗

    /// <summary>
    /// 获取全图所有尚未被搜出的深埋地脉宝物
    /// </summary>
    public IReadOnlyList<UndergroundItemSpot> GetUndergroundItems()
    {
        var list = new List<UndergroundItemSpot>();
        foreach (var c in Cities)
        {
            if (c.UndergroundItemId > 0 && !string.IsNullOrEmpty(c.UndergroundItemName))
            {
                list.Add(new UndergroundItemSpot
                {
                    ItemId = c.UndergroundItemId,
                    ItemName = c.UndergroundItemName,
                    CityId = c.CityId,
                    CityName = c.Name,
                    Description = $"深埋于【{c.Name}】地脉，尚未被诸侯开发搜出"
                });
            }
        }
        return list;
    }

    /// <summary>
    /// 获取全图所有在野、下野与流浪败逃中的将领
    /// </summary>
    public IReadOnlyList<LiveGeneralRecord> GetWanderingGenerals()
    {
        return Generals
            .Where(g => g.Status == GeneralStatus.Wild || g.Status == GeneralStatus.Wandering)
            .OrderBy(g => g.Status)
            .ThenByDescending(g => g.Force + g.Intellect)
            .ToList();
    }
}
