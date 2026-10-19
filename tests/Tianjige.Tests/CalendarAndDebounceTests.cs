using FluentAssertions;
using Tianjige.Core.Services;
using Xunit;

namespace Tianjige.Tests;

public class CalendarAndDebounceTests
{
    [Theory]
    [InlineData(184, 1, "中平元年 · 岁在甲子 · 孟春正月")]
    [InlineData(190, 1, "初平元年 · 岁在庚午 · 孟春正月")]
    [InlineData(196, 5, "建安元年 · 岁在丙子 · 仲夏五月")]
    [InlineData(200, 7, "建安五年 · 岁在庚辰 · 孟秋七月")]
    [InlineData(208, 11, "建安十三年 · 岁在戊子 · 仲冬十一月")]
    [InlineData(220, 1, "建安二十五年 · 岁在庚子 · 孟春正月")]
    [InlineData(221, 4, "黄初二年 · 岁在辛丑 · 孟夏四月")]
    [InlineData(226, 12, "黄初七年 · 岁在丙午 · 季冬腊月")]
    [InlineData(227, 2, "太和元年 · 岁在丁未 · 仲春二月")]
    [InlineData(234, 8, "太和八年 · 岁在甲寅 · 仲秋八月")]
    public void AncientCalendar_ShouldFormatAccurately(int year, int month, string expected)
    {
        var result = AncientCalendarService.FormatYearMonth(year, month);
        result.Should().Be(expected);
    }

    [Fact]
    public void BattleDebouncer_WhenSlotMachineSpinning_SuppressesFlicker()
    {
        var debouncer = new BattleDebouncer();

        // 初始状态
        debouncer.IsLockedIn.Should().BeFalse();
        debouncer.StatusText.Should().Be("全军整备 · 静待烽烟");

        // 1. 进入内政状态 (BattleState = 1)
        debouncer.Update(battleState: 1, leftGeneralId: 0, rightGeneralId: 0);
        debouncer.IsLockedIn.Should().BeFalse();
        debouncer.StatusText.Should().Contain("治国理政");

        // 2. 战前老虎机阶段 (BattleState = 2): 极速快速跳变头像
        debouncer.Update(battleState: 2, leftGeneralId: 1, rightGeneralId: 10);
        debouncer.IsLockedIn.Should().BeFalse();
        debouncer.StatusText.Should().Contain("阵前定将");

        debouncer.Update(battleState: 2, leftGeneralId: 1, rightGeneralId: 15);
        debouncer.IsLockedIn.Should().BeFalse();
        debouncer.StatusText.Should().Contain("阵前定将");

        // 3. 正式确认对战 (BattleState = 3)
        debouncer.Update(battleState: 3, leftGeneralId: 1, rightGeneralId: 15);
        debouncer.IsLockedIn.Should().BeTrue();
        debouncer.StatusText.Should().Contain("两军交锋");
        debouncer.LockedLeftGeneralId.Should().Be(1);
        debouncer.LockedRightGeneralId.Should().Be(15);

        // 4. 战毕回归大地图 (BattleState = 0)
        debouncer.Update(battleState: 0, leftGeneralId: 0, rightGeneralId: 0);
        debouncer.IsLockedIn.Should().BeFalse();
        debouncer.LockedLeftGeneralId.Should().Be(0);
        debouncer.LockedRightGeneralId.Should().Be(0);
        debouncer.StatusText.Should().Contain("大地图进军");
    }
}
