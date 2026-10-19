using System.Runtime.InteropServices;
using FluentAssertions;
using Tianjige.Core.Models;
using Tianjige.Data.Memory;
using Tianjige.Data.Memory.Models;
using Xunit;

namespace Tianjige.Tests;

public class MemoryStructTests
{
    [Fact]
    public void LiveGeneralMemory_ShouldHaveConsistentLayout()
    {
        // 验证武将内存结构体大小与字段偏移安全
        int size = Marshal.SizeOf<LiveGeneralMemoryRaw>();
        size.Should().BeGreaterThan(0);
        size.Should().BeLessThanOrEqualTo(64);
    }

    [Fact]
    public void LiveCityMemory_ShouldHaveConsistentLayout()
    {
        // 验证城池内存结构体大小
        int size = Marshal.SizeOf<LiveCityMemoryRaw>();
        size.Should().BeGreaterThan(0);
        size.Should().BeLessThanOrEqualTo(32);
    }

    [Fact]
    public void AobPattern_ShouldMatchValidByteSequence()
    {
        byte[] buffer = new byte[] { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x10, 0x8B, 0x45, 0x08 };
        string pattern = "83 EC ?? 8B";

        int offset = AobPatternMatcher.FindPattern(buffer, pattern);
        offset.Should().Be(3);
    }

    [Fact]
    public void MockMemoryReader_ProvidesConsistentFullGameSnapshot()
    {
        var reader = new MockMemoryReader();
        bool success = reader.TryGetSnapshot(out var snapshot);

        success.Should().BeTrue();
        snapshot.Should().NotBeNull();
        snapshot.Generals.Should().HaveCount(400);
        snapshot.Cities.Should().HaveCount(60);

        // 验证关羽实时状态
        var guanyu = snapshot.Generals.FirstOrDefault(g => g.GeneralId == 1 || g.Name == "关羽");
        guanyu.Should().NotBeNull();
        guanyu!.Force.Should().Be(98);
        guanyu.Loyalty.Should().BeGreaterThan(90);

        // 验证城池预备兵
        var xuchang = snapshot.Cities.FirstOrDefault(c => c.CityId == 1 || c.Name == "许昌");
        xuchang.Should().NotBeNull();
        xuchang!.ReserveTroops.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Snapshot_DetectsUndergroundItemsAccurately()
    {
        var reader = new MockMemoryReader();
        reader.TryGetSnapshot(out var snapshot);

        // 找出所有未被搜出的埋藏宝物
        var hiddenItems = snapshot.GetUndergroundItems();
        hiddenItems.Should().NotBeEmpty();
        hiddenItems.Should().Contain(i => i.CityName != "");
    }

    [Fact]
    public void Snapshot_DetectsEscapedAndWanderingGenerals()
    {
        var reader = new MockMemoryReader();
        reader.TryGetSnapshot(out var snapshot);

        // 获取在野与流浪将领
        var wandering = snapshot.GetWanderingGenerals();
        wandering.Should().NotBeNull();
        wandering.Should().Contain(g => g.Status == GeneralStatus.Wandering || g.Status == GeneralStatus.Wild);
    }
}
