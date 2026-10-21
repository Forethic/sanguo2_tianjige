using System.Text;
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

        // 1. 无边框沉浸模式 (1440x1080)
        var geometryBorderless = DisplayGeometryCalculator.Calculate(monitor, isBordered: false);
        var iniBorderless = _manager.GenerateIniContent(geometryBorderless, border: false, devmode: false);

        iniBorderless.Should().Contain("width=1440");
        iniBorderless.Should().Contain("height=1080");
        iniBorderless.Should().Contain("posX=0");
        iniBorderless.Should().Contain("posY=0");
        iniBorderless.Should().Contain("windowed=true");
        iniBorderless.Should().Contain("border=false");
        iniBorderless.Should().Contain("devmode=false");
        iniBorderless.Should().Contain("savesettings=0");
        iniBorderless.Should().Contain("center_window=0");

        // 2. 有边框窗口模式 (1280x960, 2x 缩放)
        var geometryBordered = DisplayGeometryCalculator.Calculate(monitor, isBordered: true);
        var iniBordered = _manager.GenerateIniContent(geometryBordered, border: true, devmode: true);

        iniBordered.Should().Contain("width=1280");
        iniBordered.Should().Contain("height=960");
        iniBordered.Should().Contain("border=true");
        iniBordered.Should().Contain("devmode=true");
        iniBordered.Should().Contain("vsync=false");
        iniBordered.Should().Contain("maxfps=60");
        iniBordered.Should().Contain("adjmouse=false");
        iniBordered.Should().Contain("resizable=true");
        iniBordered.Should().Contain("[Sango2]");
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

    [Fact]
    public void DeployToDirectory_ShouldAuthoritativelyOverwriteExistingIniWithCleanTianjigeConfig()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "TianjigeTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);
            var iniPath = Path.Combine(tempDir, "ddraw.ini");
            // 模拟之前残留的冲突配置（如错误的旧分辨率、开启了垂直同步产生延迟等）
            var staleDirtyContent = @"[ddraw]
width=800
height=600
vsync=true
maxfps=-1
devmode=false
border=false
";
            File.WriteAllText(iniPath, staleDirtyContent, Encoding.UTF8);

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
            var geometry = DisplayGeometryCalculator.Calculate(monitor, isBordered: true);

            // 天机阁作为主控配置台，全量覆写
            _manager.DeployToDirectory(tempDir, geometry, border: true, devmode: true);

            var updatedIni = File.ReadAllText(iniPath, Encoding.UTF8);
            // 验证已彻底清除旧冲突，并由天机阁权威配置覆盖
            updatedIni.Should().Contain("width=1280");
            updatedIni.Should().Contain("height=960");
            updatedIni.Should().Contain("border=true");
            updatedIni.Should().Contain("devmode=true");
            var lines = updatedIni.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            lines.Should().Contain("vsync=false");
            lines.Should().NotContain("vsync=true");
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
