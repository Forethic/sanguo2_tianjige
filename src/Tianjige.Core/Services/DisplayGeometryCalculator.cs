using Tianjige.Core.Models;

namespace Tianjige.Core.Services;

/// <summary>
/// 4:3 视口几何推导纯算法引擎
/// </summary>
public static class DisplayGeometryCalculator
{
    /// <summary>
    /// 依据目标显示器物理规格推导 4:3 游戏画布与侧边 HUD 几何数据
    /// </summary>
    public static DisplayGeometry Calculate(MonitorInfo monitor, int? customHudWidth = null, bool isBordered = false)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        int hScreen = monitor.Height;
        int wScreen = monitor.Width;

        int hGame;
        int wGame;

        if (isBordered)
        {
            // 有边框标准窗口模式：预留 Windows 任务栏 (~48px) 与 窗口标题栏+上下边框 (~40px)
            // 采用舒适的 4:3 规格（如 1080p 屏幕下适配 960px 净高，为 640x480 的完整 2x 缩放）
            int maxAvailableH = Math.Max(480, hScreen - 90);
            hGame = (maxAvailableH >= 960 && hScreen <= 1200) ? 960 : (maxAvailableH / 4) * 4;
            wGame = (int)Math.Floor(hGame * 4.0 / 3.0);
            if (wGame > wScreen - 250)
            {
                wGame = Math.Max(320, (wScreen - 250) / 4 * 4);
                hGame = (int)Math.Floor(wGame * 3.0 / 4.0);
            }
        }
        else
        {
            // 无边框沉浸模式：占满全高
            hGame = hScreen;
            wGame = (int)Math.Floor(hScreen * 4.0 / 3.0);
            if (wGame > wScreen)
            {
                wGame = wScreen;
            }
        }

        int blackBarWidth = Math.Max(0, wScreen - wGame);

        // 建议 HUD 宽度：若黑边 >= 400px 则默认 400px，否则采用黑边全宽
        int defaultHudWidth = blackBarWidth >= 400 ? 400 : Math.Max(200, blackBarWidth);
        int finalHudWidth = customHudWidth.HasValue
            ? Math.Clamp(customHudWidth.Value, 200, Math.Max(200, blackBarWidth))
            : defaultHudWidth;

        int gameTop = isBordered ? monitor.Top + Math.Max(20, (hScreen - 48 - hGame) / 2) : monitor.Top;
        int gameLeft = isBordered ? monitor.Left + 20 : monitor.Left;
        int hudLeft = isBordered ? gameLeft + wGame + 12 : monitor.Left + wGame;

        return new DisplayGeometry
        {
            Monitor = monitor,
            GameWidth = wGame,
            GameHeight = hGame,
            GameLeft = gameLeft,
            GameTop = gameTop,
            BlackBarWidth = blackBarWidth,
            RecommendedHudWidth = finalHudWidth,
            HudLeft = hudLeft,
            HudTop = monitor.Top,
            HudHeight = hScreen
        };
    }
}
