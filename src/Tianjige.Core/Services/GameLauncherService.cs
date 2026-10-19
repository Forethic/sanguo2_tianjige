using System.Diagnostics;
using Tianjige.Core.Models;

namespace Tianjige.Core.Services;

/// <summary>
/// 游戏拉起与环境注入核心服务
/// </summary>
public class GameLauncherService
{
    private readonly CncDdrawManager _cncDdrawManager;

    public GameLauncherService(CncDdrawManager? cncDdrawManager = null)
    {
        _cncDdrawManager = cncDdrawManager ?? new CncDdrawManager();
    }

    /// <summary>
    /// 校验游戏目录合法性（判断目录内是否存在 Sango2.exe）
    /// </summary>
    public bool ValidateGameDirectory(string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath)) return false;
        if (!Directory.Exists(directoryPath)) return false;

        var exeNames = new[] { "Sango2.exe", "sango2.exe", "SANGO2.EXE", "Sango 2.exe" };
        return exeNames.Any(name => File.Exists(Path.Combine(directoryPath, name)));
    }

    /// <summary>
    /// 找到游戏主执行程序路径
    /// </summary>
    public string? FindExecutable(string directoryPath)
    {
        if (!Directory.Exists(directoryPath)) return null;

        var exeNames = new[] { "Sango2.exe", "sango2.exe", "SANGO2.EXE", "Sango 2.exe" };
        foreach (var name in exeNames)
        {
            var fullPath = Path.Combine(directoryPath, name);
            if (File.Exists(fullPath)) return fullPath;
        }

        return null;
    }

    /// <summary>
    /// 自动对齐 cnc-ddraw 环境并拉起原版游戏进程
    /// </summary>
    public Process LaunchGame(string gameDirectory, DisplayGeometry geometry, string renderer = "auto", bool devmode = false)
    {
        ArgumentNullException.ThrowIfNull(gameDirectory);
        ArgumentNullException.ThrowIfNull(geometry);

        var exePath = FindExecutable(gameDirectory)
            ?? throw new FileNotFoundException($"在选定目录中找不到《三国群英传2》执行程序: {gameDirectory}");

        // 自动注入/更新 ddraw.dll 与精准 ddraw.ini
        _cncDdrawManager.DeployToDirectory(gameDirectory, geometry, renderer, devmode);

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = gameDirectory,
            UseShellExecute = false
        };

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.Start();
        return process;
    }
}
