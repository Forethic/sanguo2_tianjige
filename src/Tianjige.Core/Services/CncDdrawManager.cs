using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Tianjige.Core.Models;

namespace Tianjige.Core.Services;

/// <summary>
/// cnc-ddraw 组件管理与 ini 动态配置生成引擎
/// </summary>
public class CncDdrawManager
{
    private const string DdrawDllResource = "Tianjige.Core.Assets.CncDdraw.ddraw.dll";
    private const string DdrawIniResource = "Tianjige.Core.Assets.CncDdraw.cnc-ddraw.ini.template";

    public Stream? GetDdrawDllStream()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream(DdrawDllResource);
    }

    public string GetTemplateIni()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(DdrawIniResource)
            ?? throw new InvalidOperationException($"找不到内置 cnc-ddraw ini 模板: {DdrawIniResource}");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// 依据推导的几何数据生成天机阁权威 ddraw.ini 配置文本
    /// </summary>
    public string GenerateIniContent(DisplayGeometry geometry, string renderer = "auto", bool devmode = true, bool border = true)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        var template = GetTemplateIni();

        var overrides = new Dictionary<string, string>
        {
            ["width"] = geometry.GameWidth.ToString(),
            ["height"] = geometry.GameHeight.ToString(),
            ["posX"] = geometry.GameLeft.ToString(),
            ["posY"] = geometry.GameTop.ToString(),
            ["windowed"] = "true",
            ["border"] = border ? "true" : "false",
            ["resizable"] = "true",
            ["fullscreen"] = "false",
            ["maintas"] = "false",
            ["savesettings"] = "0",
            ["center_window"] = geometry.IsGameCentered ? "2" : "0",
            ["renderer"] = renderer,
            ["devmode"] = devmode ? "true" : "false",
            ["nonexclusive"] = "true",
            ["vsync"] = "false",
            ["maxfps"] = "60",
            ["adjmouse"] = "false", // 严禁软件插值灵敏度计算，交由系统硬件光标原生 1:1 驱动，彻底根绝粘连拖拽感
            ["noactivateapp"] = "true",
            ["toggle_borderless"] = "false"
        };

        var lines = template.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var handledKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = new StringBuilder();
        bool inDdrawSection = false;
        bool addedMissingOverrides = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                var sectionName = trimmed[1..^1].Trim();
                if (inDdrawSection && !addedMissingOverrides)
                {
                    // 即将离开 [ddraw] 节区，补齐尚未覆盖的核心参数
                    foreach (var (key, value) in overrides)
                    {
                        if (!handledKeys.Contains(key))
                        {
                            result.AppendLine($"{key}={value}");
                            handledKeys.Add(key);
                        }
                    }
                    addedMissingOverrides = true;
                }
                inDdrawSection = string.Equals(sectionName, "ddraw", StringComparison.OrdinalIgnoreCase);
                result.AppendLine(line);
                continue;
            }

            if (inDdrawSection)
            {
                if (trimmed.StartsWith(";") || trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed))
                {
                    result.AppendLine(line);
                    continue;
                }

                int eqIndex = line.IndexOf('=');
                if (eqIndex > 0)
                {
                    var key = line[..eqIndex].Trim();
                    if (overrides.TryGetValue(key, out var overrideValue))
                    {
                        result.AppendLine($"{key}={overrideValue}");
                        handledKeys.Add(key);
                        continue;
                    }
                }
            }

            result.AppendLine(line);
        }

        // 若文件末尾尚未补齐核心参数（例如没有其他节区）
        if (!addedMissingOverrides)
        {
            foreach (var (key, value) in overrides)
            {
                if (!handledKeys.Contains(key))
                {
                    result.AppendLine($"{key}={value}");
                    handledKeys.Add(key);
                }
            }
        }

        // 显式追加/覆写游戏专有节区 [Sango2] 与 [sango2]
        // cnc-ddraw 会优先读取匹配执行程序名称的节区，确保无论何种启动路径均绝对执行天机阁权威配置
        var gameSections = new[] { "Sango2", "sango2" };
        foreach (var section in gameSections)
        {
            result.AppendLine();
            result.AppendLine($"[{section}]");
            foreach (var (key, value) in overrides)
            {
                result.AppendLine($"{key}={value}");
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// 全量权威覆写部署 ddraw.dll 与天机阁 ddraw.ini 到游戏所在目录
    /// 天机阁作为主控启程配置台，每次启动均全量更新环境，确保 100% 无错位、零输入延迟与无缝并排体验
    /// </summary>
    public void DeployToDirectory(string targetDirectory, DisplayGeometry geometry, string renderer = "auto", bool devmode = true, bool border = true)
    {
        ArgumentNullException.ThrowIfNull(targetDirectory);
        ArgumentNullException.ThrowIfNull(geometry);

        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        var dllPath = Path.Combine(targetDirectory, "ddraw.dll");
        try
        {
            using var dllStream = GetDdrawDllStream()
                ?? throw new InvalidOperationException("无法获取嵌入式 ddraw.dll 二进制资源");
            using var fileStream = new FileStream(dllPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            dllStream.CopyTo(fileStream);
        }
        catch (IOException)
        {
            // 若被正在运行的游戏锁定，则容错跳过 dll 覆写，继续部署 ini
        }

        // 全量覆盖写入天机阁权威配置
        var iniContent = GenerateIniContent(geometry, renderer, devmode, border);
        var iniPath = Path.Combine(targetDirectory, "ddraw.ini");
        File.WriteAllText(iniPath, iniContent, Encoding.UTF8);
    }
}
