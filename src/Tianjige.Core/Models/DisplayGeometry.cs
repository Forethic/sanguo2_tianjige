namespace Tianjige.Core.Models;

/// <summary>
/// 显示器信息描述
/// </summary>
public sealed record MonitorInfo : System.ComponentModel.INotifyPropertyChanged
{
    public required string DeviceName { get; init; }
    public required string DisplayTitle { get; init; }
    public required int Index { get; init; }
    public required bool IsPrimary { get; init; }
    public required int Left { get; init; }
    public required int Top { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public double DpiScale { get; init; } = 1.0;

    public string ResolutionText => $"{Width} × {Height}";

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// 4:3 游戏视口与右侧 HUD 净黑边几何推导结果
/// </summary>
public sealed record DisplayGeometry
{
    public required MonitorInfo Monitor { get; init; }

    /// <summary>
    /// 目标屏幕宽度 (W_screen)
    /// </summary>
    public int ScreenWidth => Monitor.Width;

    /// <summary>
    /// 目标屏幕高度 (H_screen)
    /// </summary>
    public int ScreenHeight => Monitor.Height;

    /// <summary>
    /// 4:3 游戏画布拉伸宽度: floor(H_screen * 4 / 3)
    /// </summary>
    public required int GameWidth { get; init; }

    /// <summary>
    /// 4:3 游戏画布拉伸高度: 等同于屏幕全高 H_screen
    /// </summary>
    public required int GameHeight { get; init; }

    /// <summary>
    /// 游戏窗口左侧绝对坐标 (在多屏桌面环境下的绝对坐标)
    /// </summary>
    public required int GameLeft { get; init; }

    /// <summary>
    /// 游戏窗口顶部绝对坐标
    /// </summary>
    public required int GameTop { get; init; }

    /// <summary>
    /// 留出的纯物理右侧黑边宽度 (W_black = W_screen - W_game)
    /// </summary>
    public required int BlackBarWidth { get; init; }

    /// <summary>
    /// HUD 建议宽度 (不遮挡游戏任何画面的最大安全宽度)
    /// </summary>
    public required int RecommendedHudWidth { get; init; }

    /// <summary>
    /// HUD 起始左侧绝对坐标 (X_hud = GameLeft + GameWidth)
    /// </summary>
    public required int HudLeft { get; init; }

    /// <summary>
    /// HUD 起始顶部绝对坐标
    /// </summary>
    public required int HudTop { get; init; }

    /// <summary>
    /// HUD 高度 (铺满全屏高)
    /// </summary>
    public required int HudHeight { get; init; }
}
