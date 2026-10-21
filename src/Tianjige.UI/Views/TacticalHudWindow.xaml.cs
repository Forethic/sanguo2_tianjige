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

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    public void EnsureTopmost()
    {
        try
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }
        catch { }
    }

    public TacticalHudWindow(Window? ownerLauncher = null, Tianjige.Data.Memory.IMemoryReader? memoryReader = null)
    {
        _ownerLauncher = ownerLauncher;
        _memoryReader = memoryReader;

        InitializeComponent();

        if (memoryReader != null)
        {
            DataContext = new MainViewModel(memoryReader: memoryReader);
        }

        Loaded += (s, e) => EnsureTopmost();

        if (DataContext is MainViewModel mainVm)
        {
            _telemetryTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _telemetryTimer.Tick += (s, e) =>
            {
                EnsureTopmost();
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
    /// 精确对齐到目标屏幕右侧净黑边无遮挡区域 (自适应 DPI 物理像素换算)
    /// </summary>
    public void ApplyGeometry(DisplayGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
        double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1.0;
        double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1.0;

        Left = geometry.HudLeft / scaleX;
        Top = geometry.HudTop / scaleY;
        Width = geometry.RecommendedHudWidth / scaleX;
        Height = geometry.HudHeight / scaleY;

        EnsureTopmost();
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
