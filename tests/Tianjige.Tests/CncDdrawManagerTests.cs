using FluentAssertions;
using Tianjige.Core.Models;
using Tianjige.Core.Services;
using Xunit;

namespace Tianjige.Tests;

public class CncDdrawManagerTests
{
    private readonly CncDdrawManager _manager = new();

    [Fact]
    public void EmbeddedAssets_ShouldBeValidAndAvailable()
    {
        using var dllStream = _manager.GetDdrawDllStream();
        dllStream.Should().NotBeNull();
        dllStream!.Length.Should().BeGreaterThan(400_000); // 约 411KB

        var template = _manager.GetTemplateIni();
        template.Should().NotBeNullOrWhiteSpace();
        template.Should().Contain("[ddraw]");
    }

    [Fact]
    public void GenerateIniContent_ShouldContainCalculatedGeometryValues()
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
        var iniContent = _manager.GenerateIniContent(geometry);

        iniContent.Should().Contain("width=1440");
        iniContent.Should().Contain("height=1080");
        iniContent.Should().Contain("posX=0");
        iniContent.Should().Contain("posY=0");
        iniContent.Should().Contain("windowed=true");
        iniContent.Should().Contain("border=false");
        iniContent.Should().Contain("savesettings=0");
        iniContent.Should().Contain("center_window=0");
    }

    [Fact]
    public void DeployToDirectory_ShouldWriteDllAndIni()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "TianjigeTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);

            var monitor = new MonitorInfo
            {
                DeviceName = "\\\\.\\DISPLAY1",
                DisplayTitle = "屏幕 1",
                Index = 1,
                IsPrimary = true,
                Left = 0,
                Top = 0,
                Width = 1920,
                Height = 1080
            };
            var geometry = DisplayGeometryCalculator.Calculate(monitor);

            _manager.DeployToDirectory(tempDir, geometry);

            var dllPath = Path.Combine(tempDir, "ddraw.dll");
            var iniPath = Path.Combine(tempDir, "ddraw.ini");

            File.Exists(dllPath).Should().BeTrue();
            File.Exists(iniPath).Should().BeTrue();
            new FileInfo(dllPath).Length.Should().BeGreaterThan(400_000);
            File.ReadAllText(iniPath).Should().Contain("width=1440");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
