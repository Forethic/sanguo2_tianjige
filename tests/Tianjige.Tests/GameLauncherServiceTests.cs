using FluentAssertions;
using Tianjige.Core.Services;
using Xunit;

namespace Tianjige.Tests;

public class GameLauncherServiceTests
{
    private readonly GameLauncherService _service = new();

    [Fact]
    public void ValidateGameDirectory_EmptyOrInvalid_ReturnsFalse()
    {
        _service.ValidateGameDirectory(null).Should().BeFalse();
        _service.ValidateGameDirectory("").Should().BeFalse();
        _service.ValidateGameDirectory(@"Z:\NonExistentPath_12345").Should().BeFalse();
    }

    [Fact]
    public void ValidateGameDirectory_WhenSango2ExeExists_ReturnsTrue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "Sango2Mock_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);
            var exePath = Path.Combine(tempDir, "Sango2.exe");
            File.WriteAllText(exePath, "mock exe");

            _service.ValidateGameDirectory(tempDir).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
