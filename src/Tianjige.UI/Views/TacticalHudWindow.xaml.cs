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
    private readonly System.Windows.Threading.DispatcherTimer? _telemetryTimer;
    private readonly Tianjige.Data.Memory.IMemoryReader? _memoryReader;

    public TacticalHudWindow(Window? ownerLauncher = null, Tianjige.Data.Memory.IMemoryReader? memoryReader = null)
    {
        _ownerLauncher = ownerLauncher;
        _memoryReader = memoryReader;

        InitializeComponent();

        if (memoryReader != null)
        {
            DataContext = new MainViewModel(memoryReader: memoryReader);
        }

        if (DataContext is MainViewModel mainVm)
        {
            _telemetryTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _telemetryTimer.Tick += (s, e) =>
            {
                mainVm.WorldSituationVm.Refresh();
            };
            _telemetryTimer.Start();
        }

        Closed += (s, e) =>
        {
            _telemetryTimer?.Stop();
            _memoryReader?.Dispose();
        };
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
