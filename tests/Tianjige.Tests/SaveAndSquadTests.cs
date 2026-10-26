using FluentAssertions;
using Tianjige.Core.Models;
using Tianjige.Core.Services;
using Tianjige.Data.Save;
using Xunit;

namespace Tianjige.Tests;

public class SaveAndSquadTests
{
    [Fact]
    public void UserSettings_LockCursor_TogglesCorrectly()
    {
        var settings = new UserSettings { LockCursor = false };
        settings.UnlockCursor.Should().BeTrue();

        settings.LockCursor = true;
        settings.UnlockCursor.Should().BeFalse();

        settings.UnlockCursor = true;
        settings.LockCursor.Should().BeFalse();
    }

    [Fact]
    public void DualScreenDeck_CalculatesCenteredGame()
    {
        var gameMon = new MonitorInfo
        {
            Index = 1,
            DisplayTitle = "显示器 1",
            DeviceName = "\\\\.\\DISPLAY1",
            Left = 0,
            Top = 0,
            Width = 1920,
            Height = 1080,
            IsPrimary = true
        };

        var deckMon = new MonitorInfo
        {
            Index = 2,
            DisplayTitle = "显示器 2",
            DeviceName = "\\\\.\\DISPLAY2",
            Left = 1920,
            Top = 0,
            Width = 1920,
            Height = 1080,
            IsPrimary = false
        };

        var geometry = DisplayGeometryCalculator.CalculateDualScreenDeck(gameMon, deckMon, isBordered: false);
        geometry.Mode.Should().Be(LauncherDisplayMode.DualScreenDeck);
        geometry.IsGameCentered.Should().BeTrue();
        geometry.GameWidth.Should().Be(1440); // 1080 * 4 / 3 = 1440
        geometry.GameHeight.Should().Be(1080);
        geometry.GameLeft.Should().Be((1920 - 1440) / 2); // 240
        geometry.DeckMonitor.Should().Be(deckMon);
    }

    [Fact]
    public void SquadManagementService_GlobalTemplates_CalculateAccurateSynergy()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "Tianjige_Test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new SquadManagementService(null, tempFolder);
            var squads = service.LoadGlobalSquads();

            squads.Should().NotBeEmpty();
            var wuhu = squads.FirstOrDefault(s => s.Name.Contains("五虎"));
            wuhu.Should().NotBeNull();
            wuhu!.MemberNames.Should().HaveCount(5);
            wuhu.AverageForce.Should().BeGreaterThan(90);
            wuhu.SynergyGrade.Should().Contain("S");

            var wolong = squads.FirstOrDefault(s => s.Name.Contains("卧龙"));
            wolong.Should().NotBeNull();
            wolong!.AverageIntellect.Should().BeGreaterThan(80);
            wolong.HasMpRegen.Should().BeTrue();
            wolong.SynergyGrade.Should().Contain("SSS");
        }
        finally
        {
            if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);
        }
    }

    [Fact]
    public void SaveGameReader_ParseSummaryString_DetectsYearAndMonarch()
    {
        // 模拟群英传2存档 0x0C 开始的 64 字节摘要字符串
        // Big5 编码：刘备 城数05 武将024 时间208年
        var summary = "刘备   城数05 武将024 时间208年";
        var monarch = SaveGameReader.ExtractMonarchFromSummary(summary);
        monarch.Should().Be("刘备");

        var year = SaveGameReader.ExtractYearFromSummary(summary);
        year.Should().Be(208);

        var city = SaveGameReader.ExtractCityCountFromSummary(summary);
        city.Should().Be(5);

        var general = SaveGameReader.ExtractGeneralCountFromSummary(summary);
        general.Should().Be(24);
    }
}
