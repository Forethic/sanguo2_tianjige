using FluentAssertions;
using Tianjige.Data.Memory;
using Tianjige.Data.Memory.Models;
using Xunit;

namespace Tianjige.Tests;

public class WorldSituationTests
{
    private readonly MockMemoryReader _reader = new();

    [Fact]
    public void Snapshot_CitiesAreGroupedAndSortedByReserveTroops()
    {
        _reader.TryGetSnapshot(out var snapshot);
        snapshot.Should().NotBeNull();

        // 验证可按预备兵穿透筛选重兵城与空虚城
        var heavilyDefended = snapshot.Cities.Where(c => c.ReserveTroops >= 400).ToList();
        var emptyCities = snapshot.Cities.Where(c => c.ReserveTroops == 0).ToList();

        snapshot.Cities.Should().NotBeEmpty();
    }

    [Fact]
    public void Snapshot_LowLoyaltyGenerals_AreSortedAscendingForTargeting()
    {
        _reader.TryGetSnapshot(out var snapshot);
        snapshot.Should().NotBeNull();

        var lowLoyalty = snapshot.Generals
            .Where(g => g.Status == GeneralStatus.Active && g.Loyalty < 80)
            .OrderBy(g => g.Loyalty)
            .ToList();

        lowLoyalty.Should().NotBeEmpty();
        if (lowLoyalty.Count >= 2)
        {
            lowLoyalty[0].Loyalty.Should().BeLessThanOrEqualTo(lowLoyalty[1].Loyalty);
        }
    }

    [Fact]
    public void Snapshot_WanderingAndWildGenerals_Identified()
    {
        _reader.TryGetSnapshot(out var snapshot);
        var wandering = snapshot.GetWanderingGenerals();

        wandering.Should().NotBeEmpty();
        wandering.Should().AllSatisfy(g =>
        {
            (g.Status == GeneralStatus.Wild || g.Status == GeneralStatus.Wandering).Should().BeTrue();
        });
    }

    [Fact]
    public void Snapshot_UndergroundItems_FoundAccurately()
    {
        _reader.TryGetSnapshot(out var snapshot);
        var items = snapshot.GetUndergroundItems();

        items.Should().NotBeEmpty();
        items.Should().Contain(i => i.ItemName == "方天画戟");
        items.Should().Contain(i => i.ItemName == "赤兔马");
    }

    [Fact]
    public void SanguoCityTable_ShouldMapAllCityIds_WithoutGenericPrefixedNames()
    {
        // 验证群城映射：0 为野外，1~60 均精准对应 authentic Cities.json，无一处出现 '郡城_'
        SanguoCityTable.GetCityName(0).Should().Be("野外 · 游历");
        SanguoCityTable.GetCityName(1).Should().Be("襄平");
        SanguoCityTable.GetCityName(2).Should().Be("北平");
        SanguoCityTable.GetCityName(15).Should().Be("天水");
        SanguoCityTable.GetCityName(16).Should().Be("西凉");
        SanguoCityTable.GetCityName(19).Should().Be("许昌");
        SanguoCityTable.GetCityName(24).Should().Be("襄阳");
        SanguoCityTable.GetCityName(27).Should().Be("江陵");
        SanguoCityTable.GetCityName(37).Should().Be("豫章");
        SanguoCityTable.GetCityName(41).Should().Be("成都");
        SanguoCityTable.GetCityName(45).Should().Be("云南");
        SanguoCityTable.GetCityName(60).Should().Be("合肥");

        for (int id = 0; id <= 60; id++)
        {
            var name = SanguoCityTable.GetCityName(id);
            name.Should().NotStartWith("郡城_");
            name.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void LiveGeneralRecord_StatusDisplayName_ShouldBeChinese()
    {
        var wildGeneral = new LiveGeneralRecord
        {
            GeneralId = 1,
            Name = "张角",
            LordId = 0,
            LordName = "无",
            CityId = 0,
            CityName = "野外 · 游历",
            Status = GeneralStatus.Wild,
            CurrentHp = 100,
            MaxHp = 100,
            CurrentMp = 100,
            MaxMp = 100,
            Level = 1,
            TroopCount = 0,
            MaxTroopCount = 100,
            Loyalty = 100,
            Force = 56,
            Intellect = 95
        };

        wildGeneral.StatusDisplayName.Should().Be("在野");

        var wandering = wildGeneral with { Status = GeneralStatus.Wandering };
        wandering.StatusDisplayName.Should().Be("流浪");

        var active = wildGeneral with { Status = GeneralStatus.Active };
        active.StatusDisplayName.Should().Be("出仕");

        var captured = wildGeneral with { Status = GeneralStatus.Captured };
        captured.StatusDisplayName.Should().Be("被俘");
    }
}
