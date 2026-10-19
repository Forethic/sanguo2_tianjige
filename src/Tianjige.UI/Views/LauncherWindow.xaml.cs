using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Tianjige.Core.Models;
using Tianjige.Core.Services;
using Tianjige.UI.ViewModels;

namespace Tianjige.UI.Views;

/// <summary>
/// 聚贤启程台（主启动与可视化配置窗体）
/// </summary>
public partial class LauncherWindow : Window
{
    private readonly GameLauncherService _launcherService = new();
    private TacticalHudWindow? _currentHudWindow;

    public LauncherWindow()
    {
        InitializeComponent();

        if (DataContext is LauncherViewModel vm)
        {
            vm.OnLaunchGameRequested += OnLaunchGame;
            vm.OnOpenOfflineLoreRequested += OnOpenOfflineLore;
        }
    }

    private void OnLaunchGame(DisplayGeometry geometry)
    {
        if (DataContext is not LauncherViewModel vm) return;

        try
        {
            // 拉起游戏进程并注入 cnc-ddraw
            var process = _launcherService.LaunchGame(
                vm.GameDirectory,
                geometry);

            var memoryReader = new Tianjige.Data.Memory.SafeMemoryReader();
            memoryReader.TryAttach(process.Id);

            // 升起战局参谋 HUD 并吸附至侧边净黑区
            _currentHudWindow?.Close();
            _currentHudWindow = new TacticalHudWindow(this, memoryReader);
            _currentHudWindow.ApplyGeometry(geometry);
            _currentHudWindow.Show();

            // 隐藏启动配置台
            Hide();

            // 监听游戏退出事件，平滑解绑并复原启动台
            process.Exited += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    _currentHudWindow?.Close();
                    _currentHudWindow = null;
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                });
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"拉起战局遇到阻碍：\n{ex.Message}",
                "天机阁 · 启程提示",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void OnOpenOfflineLore()
    {
        // 打开单机离线典籍速查模式
        var offlineWindow = new MainWindow();
        offlineWindow.Show();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _currentHudWindow?.Close();
        Application.Current.Shutdown();
    }
}
