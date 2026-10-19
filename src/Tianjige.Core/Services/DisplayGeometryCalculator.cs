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
    public static DisplayGeometry Calculate(MonitorInfo monitor, int? customHudWidth = null)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        int hScreen = monitor.Height;
        int wScreen = monitor.Width;

        int hGame = hScreen;
        int wGame = (int)Math.Floor(hScreen * 4.0 / 3.0);

        if (wGame > wScreen)
        {
            wGame = wScreen;
        }

        int blackBarWidth = Math.Max(0, wScreen - wGame);

        // 建议 HUD 宽度：若黑边 >= 400px 则默认 400px，否则采用黑边全宽
        int defaultHudWidth = blackBarWidth >= 400 ? 400 : blackBarWidth;
        int finalHudWidth = customHudWidth.HasValue
            ? Math.Clamp(customHudWidth.Value, 200, Math.Max(200, blackBarWidth))
            : defaultHudWidth;

        return new DisplayGeometry
        {
            Monitor = monitor,
            GameWidth = wGame,
            GameHeight = hGame,
            GameLeft = monitor.Left,
            GameTop = monitor.Top,
            BlackBarWidth = blackBarWidth,
            RecommendedHudWidth = finalHudWidth,
            HudLeft = monitor.Left + wGame,
            HudTop = monitor.Top,
            HudHeight = hGame
        };
    }
}
