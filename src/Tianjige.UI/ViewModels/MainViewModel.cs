using System.Windows.Input;
using Tianjige.Core.Services;
using Tianjige.Data.Memory;
using Tianjige.Data.Memory.Models;
using Tianjige.Data.Repositories;

namespace Tianjige.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly StaticRepository _repository;
    private readonly IMemoryReader _memoryReader;
    private readonly BattleDebouncer _battleDebouncer = new();

    public GeneralStarViewModel GeneralStarVm { get; }
    public TroopCompassViewModel TroopCompassVm { get; }
    public WorldSituationViewModel WorldSituationVm { get; }
    public BattleDebouncer BattleDebouncer => _battleDebouncer;

    private int _selectedTabIndex = 0;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                OnPropertyChanged(nameof(IsGeneralStarSelected));
                OnPropertyChanged(nameof(IsTroopCompassSelected));
                OnPropertyChanged(nameof(IsWorldSituationSelected));
            }
        }
    }

    public bool IsGeneralStarSelected => SelectedTabIndex == 0;
    public bool IsTroopCompassSelected => SelectedTabIndex == 1;
    public bool IsWorldSituationSelected => SelectedTabIndex == 2;

    private bool _isTopmost = true;
    public bool IsTopmost
    {
        get => _isTopmost;
        set => SetProperty(ref _isTopmost, value);
    }

    private string _seasonTitle = "建安五年 · 岁在庚辰 · 孟秋七月";
    public string SeasonTitle
    {
        get => _seasonTitle;
        set => SetProperty(ref _seasonTitle, value);
    }

    private string _battleStatusText = "大地图进军 · 帷幄筹谋";
    public string BattleStatusText
    {
        get => _battleStatusText;
        set => SetProperty(ref _battleStatusText, value);
    }

    private string _connectionStatus = "离线大典速查模式 · 气运通泰";
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }

    public ICommand SwitchTabCommand { get; }
    public ICommand ToggleTopmostCommand { get; }

    public MainViewModel(StaticRepository? repository = null, IMemoryReader? memoryReader = null)
    {
        _repository = repository ?? new StaticRepository();
        _memoryReader = memoryReader ?? new MockMemoryReader();
        GeneralStarVm = new GeneralStarViewModel(_repository);
        TroopCompassVm = new TroopCompassViewModel(_repository);
        WorldSituationVm = new WorldSituationViewModel(_memoryReader);

        WorldSituationVm.SnapshotUpdated += OnSnapshotUpdated;
        UpdateStatusFromSnapshot(WorldSituationVm.LatestSnapshot);

        SwitchTabCommand = new RelayCommand<object>(param =>
        {
            if (param is int index) SelectedTabIndex = index;
            else if (param is string s && int.TryParse(s, out var parsed)) SelectedTabIndex = parsed;
        });

        ToggleTopmostCommand = new RelayCommand(() =>
        {
            IsTopmost = !IsTopmost;
        });
    }

    private void OnSnapshotUpdated(GameLiveSnapshot? snapshot)
    {
        UpdateStatusFromSnapshot(snapshot);
    }

    public void UpdateStatusFromSnapshot(GameLiveSnapshot? snapshot)
    {
        if (snapshot != null)
        {
            SeasonTitle = AncientCalendarService.FormatYearMonth(snapshot.Year, snapshot.Month);
            _battleDebouncer.Update(snapshot.BattleState, 0, 0);
            BattleStatusText = _battleDebouncer.StatusText;
        }

        if (_memoryReader is SafeMemoryReader safeReader)
        {
            ConnectionStatus = safeReader.IsAttached
                ? "☯ 气运连通 · 战局洞若观火"
                : "○ 闭关静思 · 典籍速查模式";
        }
        else
        {
            ConnectionStatus = _memoryReader.IsAttached
                ? "☯ 气运通泰 · 演兵推演中"
                : "○ 闭关静思 · 典籍速查模式";
        }
    }
}
