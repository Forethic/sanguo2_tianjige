using System.Windows;
using System.Windows.Input;
using Tianjige.Core.Models;
using Tianjige.UI.ViewModels;

namespace Tianjige.UI.Views;

/// <summary>
/// 运行于游戏侧边纯净黑区的战局参谋 HUD 窗体
/// </summary>
public partial class TacticalHudWindow : Window
{
    private readonly Window? _ownerLauncher;

    public TacticalHudWindow(Window? ownerLauncher = null)
    {
        _ownerLauncher = ownerLauncher;
        InitializeComponent();
    }

    /// <summary>
    /// 精确对齐到目标屏幕右侧净黑边无遮挡区域
    /// </summary>
    public void ApplyGeometry(DisplayGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        Left = geometry.HudLeft;
        Top = geometry.HudTop;
        Width = geometry.RecommendedHudWidth;
        Height = geometry.HudHeight;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void BackToLauncher_Click(object sender, RoutedEventArgs e)
    {
        if (_ownerLauncher != null)
        {
            _ownerLauncher.Show();
            _ownerLauncher.WindowState = WindowState.Normal;
            _ownerLauncher.Activate();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape && DataContext is MainViewModel vm)
        {
            if (vm.GeneralStarVm.IsReverseLookupOpen)
            {
                vm.GeneralStarVm.CloseReverseLookupCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
