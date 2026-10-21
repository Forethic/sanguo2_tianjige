namespace Tianjige.Core.Models;

/// <summary>
/// 用户本地个性化配置
/// </summary>
public class UserSettings
{
    /// <summary>
    /// 《三国群英传2》安装目录
    /// </summary>
    public string GameDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 选定的目标显示器设备名称 (如 \\.\DISPLAY1)
    /// </summary>
    public string SelectedMonitorDeviceName { get; set; } = string.Empty;

    /// <summary>
    /// HUD 自定义宽度 (默认 400px，可微调)
    /// </summary>
    public int CustomHudWidth { get; set; } = 400;

    /// <summary>
    /// HUD 自定义水平偏移微调量 (像素)
    /// </summary>
    public int CustomHudOffset { get; set; } = 0;

    /// <summary>
    /// cnc-ddraw 图形渲染后端 (auto / direct3d9 / opengl / gdi)
    /// </summary>
    public string Renderer { get; set; } = "auto";

    /// <summary>
    /// 是否在启程时自动拉起游戏进程
    /// </summary>
    public bool AutoLaunchGame { get; set; } = true;

    /// <summary>
    /// 是否使用标准窗口边框 (有边框模式，便于拖动与最小化)
    /// </summary>
    public bool IsWindowBordered { get; set; } = true;

    /// <summary>
    /// 是否开启免锁定光标模式 (devmode=true，鼠标可在游戏与HUD间自由穿梭)
    /// </summary>
    public bool UnlockCursor { get; set; } = true;
}
