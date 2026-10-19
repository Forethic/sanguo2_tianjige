using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Tianjige.Core.Models;
using Tianjige.Core.Native;
using Tianjige.Core.Services;

namespace Tianjige.UI.ViewModels;

public class LauncherViewModel : ViewModelBase
{
    private readonly UserSettingsService _settingsService;
    private readonly GameLauncherService _launcherService;
    private readonly UserSettings _settings;

    public ObservableCollection<MonitorInfo> Monitors { get; } = new();

    private MonitorInfo? _selectedMonitor;
    public MonitorInfo? SelectedMonitor
    {
        get => _selectedMonitor;
        set
        {
            if (SetProperty(ref _selectedMonitor, value))
            {
                RecalculateGeometry();
            }
        }
    }

    private string _gameDirectory = string.Empty;
    public string GameDirectory
    {
        get => _gameDirectory;
        set
        {
            if (SetProperty(ref _gameDirectory, value))
            {
                ValidateDirectory();
            }
        }
    }

    private bool _isGameDirectoryValid;
    public bool IsGameDirectoryValid
    {
        get => _isGameDirectoryValid;
        private set => SetProperty(ref _isGameDirectoryValid, value);
    }

    private string _directoryStatusText = "请选定《三国群英传2》游戏目录";
    public string DirectoryStatusText
    {
        get => _directoryStatusText;
        private set => SetProperty(ref _directoryStatusText, value);
    }

    private int _customHudWidth = 400;
    public int CustomHudWidth
    {
        get => _customHudWidth;
        set
        {
            if (SetProperty(ref _customHudWidth, value))
            {
                RecalculateGeometry();
            }
        }
    }

    private DisplayGeometry? _currentGeometry;
    public DisplayGeometry? CurrentGeometry
    {
        get => _currentGeometry;
        private set
        {
            if (SetProperty(ref _currentGeometry, value))
            {
                OnPropertyChanged(nameof(GeometrySummaryText));
                OnPropertyChanged(nameof(MaxHudWidth));
            }
        }
    }

    public int MaxHudWidth => CurrentGeometry?.BlackBarWidth > 0 ? CurrentGeometry.BlackBarWidth : 600;

    public string GeometrySummaryText => CurrentGeometry != null
        ? $"4:3 游戏区: {CurrentGeometry.GameWidth}×{CurrentGeometry.GameHeight}  |  右侧净黑边: {CurrentGeometry.BlackBarWidth}px  |  HUD 实际宽: {CurrentGeometry.RecommendedHudWidth}px"
        : "正在检测屏幕规格...";

    public ICommand SelectMonitorCommand { get; }
    public ICommand BrowseGameDirectoryCommand { get; }
    public ICommand LaunchGameCommand { get; }
    public ICommand OpenOfflineLoreCommand { get; }

    public event Action<DisplayGeometry>? OnLaunchGameRequested;
    public event Action? OnOpenOfflineLoreRequested;

    public LauncherViewModel() : this(null, null)
    {
    }

    public LauncherViewModel(UserSettingsService? settingsService = null, GameLauncherService? launcherService = null)
    {
        _settingsService = settingsService ?? new UserSettingsService();
        _launcherService = launcherService ?? new GameLauncherService();
        _settings = _settingsService.Load();

        _gameDirectory = _settings.GameDirectory;
        _customHudWidth = _settings.CustomHudWidth;

        SelectMonitorCommand = new RelayCommand<MonitorInfo>(m =>
        {
            if (m != null) SelectedMonitor = m;
        });

        BrowseGameDirectoryCommand = new RelayCommand(BrowseGameDirectory);

        LaunchGameCommand = new RelayCommand(() =>
        {
            if (!IsGameDirectoryValid || CurrentGeometry == null) return;

            // 保存用户当前选配
            _settings.GameDirectory = GameDirectory;
            _settings.CustomHudWidth = CustomHudWidth;
            if (SelectedMonitor != null)
            {
                _settings.SelectedMonitorDeviceName = SelectedMonitor.DeviceName;
            }
            _settingsService.Save(_settings);

            OnLaunchGameRequested?.Invoke(CurrentGeometry);
        }, () => IsGameDirectoryValid && CurrentGeometry != null);

        OpenOfflineLoreCommand = new RelayCommand(() =>
        {
            OnOpenOfflineLoreRequested?.Invoke();
        });

        LoadMonitors();
        ValidateDirectory();
    }

    private void LoadMonitors()
    {
        Monitors.Clear();
        var list = DisplayMonitorService.GetMonitors();
        foreach (var m in list) Monitors.Add(m);

        // 尝试恢复之前保存的屏幕，未匹配则选主屏
        SelectedMonitor = Monitors.FirstOrDefault(m => m.DeviceName == _settings.SelectedMonitorDeviceName)
            ?? Monitors.FirstOrDefault(m => m.IsPrimary)
            ?? Monitors.FirstOrDefault();
    }

    private void RecalculateGeometry()
    {
        if (SelectedMonitor != null)
        {
            CurrentGeometry = DisplayGeometryCalculator.Calculate(SelectedMonitor, CustomHudWidth);
        }
    }

    private void ValidateDirectory()
    {
        IsGameDirectoryValid = _launcherService.ValidateGameDirectory(GameDirectory);
        if (IsGameDirectoryValid)
        {
            DirectoryStatusText = "已就绪 · 检测到 Sango2.exe 主程序";
        }
        else
        {
            DirectoryStatusText = Directory.Exists(GameDirectory)
                ? "目录下未找到 Sango2.exe，请确认选择群英传2根目录"
                : "请选定《三国群英传2》游戏目录";
        }
    }

    private void BrowseGameDirectory()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择《三国群英传2》主程序 (Sango2.exe)",
            Filter = "群英传2 主程序 (Sango2.exe)|*sango2*.exe;Sango2.exe|所有可执行文件 (*.exe)|*.exe",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            var dir = Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(dir))
            {
                GameDirectory = dir;
            }
        }
    }
}
