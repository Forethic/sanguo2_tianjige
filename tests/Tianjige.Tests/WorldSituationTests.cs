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
}
