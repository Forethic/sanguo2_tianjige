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
    private DeckWindow? _currentDeckWindow;
    private System.Threading.CancellationTokenSource? _gameWatcherCts;

    public LauncherWindow()
    {
        InitializeComponent();

        if (DataContext is LauncherViewModel vm)
        {
            vm.OnLaunchGameRequested += OnLaunchGame;
            vm.OnOpenOfflineLoreRequested += OnOpenOfflineLore;
            vm.OnOpenDeckRequested += OnOpenDeck;
        }
    }

    private void OnOpenDeck(string gameDirectory)
    {
        if (_currentDeckWindow != null && _currentDeckWindow.IsLoaded)
        {
            if (_currentDeckWindow.WindowState == WindowState.Minimized)
                _currentDeckWindow.WindowState = WindowState.Normal;
            _currentDeckWindow.Activate();
            _currentDeckWindow.Focus();
            return;
        }

        var deckVm = new DeckViewModel(gameDirectory);
        _currentDeckWindow = new DeckWindow(deckVm);
        _currentDeckWindow.Closed += (s, e) => _currentDeckWindow = null;
        _currentDeckWindow.Show();
    }

    private void OnLaunchGame(DisplayGeometry geometry)
    {
        if (DataContext is not LauncherViewModel vm) return;

        try
        {
            // 拉起游戏进程并注入 cnc-ddraw (devmode = !LockCursor)
            bool devmode = !vm.LockCursor;
            var process = _launcherService.LaunchGame(
                vm.GameDirectory,
                geometry,
                vm.Renderer,
                devmode: devmode,
                border: vm.IsWindowBordered);

            var memoryReader = new Tianjige.Data.Memory.SafeMemoryReader();
            memoryReader.TryAttach(process.Id);

            if (geometry.Mode == LauncherDisplayMode.DualScreenDeck)
            {
                // 双屏模式：副屏升起全景指挥台 Deck
                _currentDeckWindow?.Close();
                var deckVm = new DeckViewModel(vm.GameDirectory);
                _currentDeckWindow = new DeckWindow(deckVm);

                if (geometry.DeckMonitor != null)
                {
                    _currentDeckWindow.Left = geometry.DeckMonitor.Left + 40;
                    _currentDeckWindow.Top = geometry.DeckMonitor.Top + 40;
                    _currentDeckWindow.Width = Math.Min(1440, Math.Max(1000, geometry.DeckMonitor.Width - 80));
                    _currentDeckWindow.Height = Math.Min(900, Math.Max(650, geometry.DeckMonitor.Height - 80));
                }
                _currentDeckWindow.Show();

                _currentDeckWindow.Closed += (s, e) =>
                {
                    _gameWatcherCts?.Cancel();
                    _currentDeckWindow = null;
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                };
            }
            else
            {
                // 单屏模式：升起战局参谋 HUD 并吸附至侧边净黑区
                _currentHudWindow?.Close();
                _currentHudWindow = new TacticalHudWindow(this, memoryReader);
                _currentHudWindow.ApplyGeometry(geometry);
                _currentHudWindow.Show();
                _currentHudWindow.EnsureTopmost();

                // 监听 HUD 被用户手动关闭事件
                _currentHudWindow.Closed += (s, e) =>
                {
                    _gameWatcherCts?.Cancel();
                    _currentHudWindow = null;
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                };

                // 延迟保鲜置顶：在游戏创建窗体取得焦点后，自动再次浮出到侧边，绝不被遮挡
                Task.Delay(800).ContinueWith(_ => Dispatcher.Invoke(() => _currentHudWindow?.EnsureTopmost()));
                Task.Delay(2000).ContinueWith(_ => Dispatcher.Invoke(() => _currentHudWindow?.EnsureTopmost()));
                Task.Delay(4000).ContinueWith(_ => Dispatcher.Invoke(() => _currentHudWindow?.EnsureTopmost()));
            }

            // 隐藏启动配置台
            Hide();

            // 启动游戏会话守护监听器，平滑应对 Steam / Odin 引导加载器多进程交接，防止 HUD 提前误关
            _gameWatcherCts?.Cancel();
            _gameWatcherCts = new System.Threading.CancellationTokenSource();
            StartGameSessionWatcher(process, memoryReader, _gameWatcherCts.Token);
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

    private void StartGameSessionWatcher(Process initialProcess, Tianjige.Data.Memory.SafeMemoryReader memoryReader, System.Threading.CancellationToken token)
    {
        Task.Run(async () =>
        {
            var startTime = DateTime.UtcNow;
            var startupGracePeriod = TimeSpan.FromSeconds(12); // 12秒平滑启动交接保护窗口

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(1200, token).ConfigureAwait(false);

                bool isInitialAlive = false;
                try
                {
                    isInitialAlive = !initialProcess.HasExited;
                }
                catch { }

                // 检查系统中是否存在任何 Sango2 相关游戏主进程
                var sangoProcesses = Process.GetProcesses()
                    .Where(p =>
                    {
                        try
                        {
                            var name = p.ProcessName;
                            return name.Equals("Sango2", StringComparison.OrdinalIgnoreCase) ||
                                   name.Equals("LaunchSango", StringComparison.OrdinalIgnoreCase);
                        }
                        catch { return false; }
                    })
                    .ToList();

                bool isAnyGameAlive = isInitialAlive || sangoProcesses.Count > 0;

                // 若初次拉起进程已转交或退出，但真实 Sango2 游戏子进程已就绪：动态重绑内存安全探针
                if (!memoryReader.IsAttached && sangoProcesses.Count > 0)
                {
                    var target = sangoProcesses.FirstOrDefault(p => !p.HasExited);
                    if (target != null)
                    {
                        memoryReader.TryAttach(target.Id);
                    }
                }

                // 在前 12 秒过渡保护期内，绝对不主动关闭 HUD，给 Steam/Odin 等引导加载器充分的子进程交接时间
                if (DateTime.UtcNow - startTime < startupGracePeriod)
                {
                    continue;
                }

                // 超过过渡保护期后，若初始进程和所有 Sango2 进程均已退出，则确认游戏真正关闭
                if (!isAnyGameAlive)
                {
                    break;
                }
            }

            if (!token.IsCancellationRequested)
            {
                Dispatcher.Invoke(() =>
                {
                    if (_currentHudWindow != null)
                    {
                        _currentHudWindow.Close();
                        _currentHudWindow = null;
                    }
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                });
            }
        }, token);
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
        _gameWatcherCts?.Cancel();
        _currentHudWindow?.Close();
        Application.Current.Shutdown();
    }
}
