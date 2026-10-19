using FluentAssertions;
using Tianjige.Core.Models;
using Tianjige.Core.Services;
using Xunit;

namespace Tianjige.Tests;

public class DisplayGeometryTests
{
    [Fact]
    public void CalculateGeometry_For1080p_ProducesCorrectBlackBarAndCoordinates()
    {
        var monitor = new MonitorInfo
        {
            DeviceName = "\\\\.\\DISPLAY1",
            DisplayTitle = "屏幕 1 (主屏, 1920x1080)",
            Index = 1,
            IsPrimary = true,
            Left = 0,
            Top = 0,
            Width = 1920,
            Height = 1080
        };

        var geometry = DisplayGeometryCalculator.Calculate(monitor);

        geometry.ScreenWidth.Should().Be(1920);
        geometry.ScreenHeight.Should().Be(1080);
        geometry.GameHeight.Should().Be(1080);
        geometry.GameWidth.Should().Be(1440); // 1080 * 4 / 3
        geometry.BlackBarWidth.Should().Be(480); // 1920 - 1440
        geometry.GameLeft.Should().Be(0);
        geometry.GameTop.Should().Be(0);
        geometry.HudLeft.Should().Be(1440);
        geometry.HudTop.Should().Be(0);
        geometry.RecommendedHudWidth.Should().Be(400);
    }

    [Fact]
    public void CalculateGeometry_For2KSecondaryMonitor_OffsetsCoordinatesCorrectly()
    {
        // 假设主屏是 1080p (0,0, 1920, 1080)，副屏是 2K (1920, 0, 2560, 1440)
        var monitor = new MonitorInfo
        {
            DeviceName = "\\\\.\\DISPLAY2",
            DisplayTitle = "屏幕 2 (副屏, 2560x1440)",
            Index = 2,
            IsPrimary = false,
            Left = 1920,
            Top = 0,
            Width = 2560,
            Height = 1440
        };

        var geometry = DisplayGeometryCalculator.Calculate(monitor);

        geometry.GameHeight.Should().Be(1440);
        geometry.GameWidth.Should().Be(1920); // 1440 * 4 / 3
        geometry.BlackBarWidth.Should().Be(640); // 2560 - 1920
        geometry.GameLeft.Should().Be(1920);
        geometry.HudLeft.Should().Be(1920 + 1920); // 3840
    }

    [Fact]
    public void CalculateGeometry_For4KMonitor_Calculates960BlackBar()
    {
        var monitor = new MonitorInfo
        {
            DeviceName = "\\\\.\\DISPLAY1",
            DisplayTitle = "屏幕 1 (4K, 3840x2160)",
            Index = 1,
            IsPrimary = true,
            Left = 0,
            Top = 0,
            Width = 3840,
            Height = 2160
        };

        var geometry = DisplayGeometryCalculator.Calculate(monitor);

        geometry.GameHeight.Should().Be(2160);
        geometry.GameWidth.Should().Be(2880); // 2160 * 4 / 3
        geometry.BlackBarWidth.Should().Be(960); // 3840 - 2880
        geometry.HudLeft.Should().Be(2880);
    }

    [Fact]
    public void CalculateGeometry_For16To10_Produces320BlackBar()
    {
        // 1920x1200
        var monitor = new MonitorInfo
        {
            DeviceName = "\\\\.\\DISPLAY1",
            DisplayTitle = "屏幕 1 (16:10, 1920x1200)",
            Index = 1,
            IsPrimary = true,
            Left = 0,
            Top = 0,
            Width = 1920,
            Height = 1200
        };

        var geometry = DisplayGeometryCalculator.Calculate(monitor);

        geometry.GameHeight.Should().Be(1200);
        geometry.GameWidth.Should().Be(1600); // 1200 * 4 / 3
        geometry.BlackBarWidth.Should().Be(320); // 1920 - 1600
        geometry.RecommendedHudWidth.Should().Be(320);
        geometry.HudLeft.Should().Be(1600);
    }

    [Fact]
    public void DisplayMonitorService_ShouldReturnAtLeastOneValidMonitor()
    {
        var monitors = Tianjige.Core.Native.DisplayMonitorService.GetMonitors();
        monitors.Should().NotBeEmpty();

        var primary = monitors.FirstOrDefault(m => m.IsPrimary);
        primary.Should().NotBeNull();
        primary!.Width.Should().BeGreaterThan(0);
        primary.Height.Should().BeGreaterThan(0);
    }
}
