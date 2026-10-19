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
    /// 依据推导的几何数据生成精准的 ddraw.ini 配置文本
    /// </summary>
    public string GenerateIniContent(DisplayGeometry geometry, string renderer = "auto", bool devmode = false)
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
            ["border"] = "false",
            ["fullscreen"] = "false",
            ["maintas"] = "false",
            ["savesettings"] = "0",
            ["center_window"] = "0",
            ["renderer"] = renderer,
            ["devmode"] = devmode ? "true" : "false"
        };

        var lines = template.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var handledKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = new StringBuilder();
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
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

            result.AppendLine(line);
        }

        // 确保所有必要参数均已写入 [ddraw] 节区
        foreach (var (key, value) in overrides)
        {
            if (!handledKeys.Contains(key))
            {
                result.AppendLine($"{key}={value}");
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// 部署 ddraw.dll 与生成的 ddraw.ini 到游戏所在目录
    /// </summary>
    public void DeployToDirectory(string targetDirectory, DisplayGeometry geometry, string renderer = "auto", bool devmode = false)
    {
        ArgumentNullException.ThrowIfNull(targetDirectory);
        ArgumentNullException.ThrowIfNull(geometry);

        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        var dllPath = Path.Combine(targetDirectory, "ddraw.dll");
        if (!File.Exists(dllPath))
        {
            using var dllStream = GetDdrawDllStream()
                ?? throw new InvalidOperationException("无法获取嵌入式 ddraw.dll 二进制资源");
            using var fileStream = File.Create(dllPath);
            dllStream.CopyTo(fileStream);
        }

        var iniContent = GenerateIniContent(geometry, renderer, devmode);
        var iniPath = Path.Combine(targetDirectory, "ddraw.ini");
        File.WriteAllText(iniPath, iniContent, Encoding.UTF8);
    }
}
