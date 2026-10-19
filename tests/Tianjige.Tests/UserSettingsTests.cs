using FluentAssertions;
using Tianjige.Core.Models;
using Tianjige.Core.Services;
using Xunit;

namespace Tianjige.Tests;

public class UserSettingsTests
{
    [Fact]
    public void DefaultSettings_ShouldHaveReasonableDefaults()
    {
        var settings = new UserSettings();
        settings.CustomHudWidth.Should().Be(400);
        settings.Renderer.Should().Be("auto");
        settings.AutoLaunchGame.Should().BeTrue();
    }

    [Fact]
    public void UserSettingsService_SaveAndLoad_ShouldPersistValues()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "TianjigeSettings_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var service = new UserSettingsService(tempFile);
            var initial = service.Load();
            initial.CustomHudWidth = 420;
            initial.GameDirectory = @"D:\Games\Sango2";
            initial.SelectedMonitorDeviceName = @"\\.\DISPLAY2";

            service.Save(initial);

            File.Exists(tempFile).Should().BeTrue();

            var reloaded = service.Load();
            reloaded.CustomHudWidth.Should().Be(420);
            reloaded.GameDirectory.Should().Be(@"D:\Games\Sango2");
            reloaded.SelectedMonitorDeviceName.Should().Be(@"\\.\DISPLAY2");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
