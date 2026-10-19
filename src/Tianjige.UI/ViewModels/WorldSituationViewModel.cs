using System.Collections.ObjectModel;
using System.Windows.Input;
using Tianjige.Data.Memory;
using Tianjige.Data.Memory.Models;

namespace Tianjige.UI.ViewModels;

public class WorldSituationViewModel : ViewModelBase
{
    private readonly IMemoryReader _memoryReader;

    public ObservableCollection<LiveCityRecord> FilteredCities { get; } = new();
    public ObservableCollection<LiveGeneralRecord> LowLoyaltyGenerals { get; } = new();
    public ObservableCollection<LiveGeneralRecord> WanderingGenerals { get; } = new();
    public ObservableCollection<UndergroundItemSpot> UndergroundItems { get; } = new();

    public event Action<GameLiveSnapshot?>? SnapshotUpdated;

    private GameLiveSnapshot? _latestSnapshot;
    public GameLiveSnapshot? LatestSnapshot
    {
        get => _latestSnapshot;
        private set => SetProperty(ref _latestSnapshot, value);
    }

    private int _selectedSubSectionIndex = 0;
    public int SelectedSubSectionIndex
    {
        get => _selectedSubSectionIndex;
        set
        {
            if (SetProperty(ref _selectedSubSectionIndex, value))
            {
                OnPropertyChanged(nameof(IsCitiesSectionSelected));
                OnPropertyChanged(nameof(IsLoyaltySectionSelected));
                OnPropertyChanged(nameof(IsWanderingSectionSelected));
                OnPropertyChanged(nameof(IsArtifactSectionSelected));
            }
        }
    }

    public bool IsCitiesSectionSelected => SelectedSubSectionIndex == 0;
    public bool IsLoyaltySectionSelected => SelectedSubSectionIndex == 1;
    public bool IsWanderingSectionSelected => SelectedSubSectionIndex == 2;
    public bool IsArtifactSectionSelected => SelectedSubSectionIndex == 3;

    private string _selectedLordFilter = "全部";
    public string SelectedLordFilter
    {
        get => _selectedLordFilter;
        set
        {
            if (SetProperty(ref _selectedLordFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public IReadOnlyList<string> LordFilters { get; } = new[]
    {
        "全部", "曹操", "刘备", "孙权", "袁绍", "其他"
    };

    public ICommand SwitchSubSectionCommand { get; }
    public ICommand SelectLordFilterCommand { get; }
    public ICommand RefreshCommand { get; }

    public WorldSituationViewModel(IMemoryReader? memoryReader = null)
    {
        _memoryReader = memoryReader ?? new MockMemoryReader();

        SwitchSubSectionCommand = new RelayCommand<object>(param =>
        {
            if (param is int idx) SelectedSubSectionIndex = idx;
            else if (param is string s && int.TryParse(s, out var parsed)) SelectedSubSectionIndex = parsed;
        });

        SelectLordFilterCommand = new RelayCommand<string>(lord =>
        {
            if (!string.IsNullOrEmpty(lord)) SelectedLordFilter = lord;
        });

        RefreshCommand = new RelayCommand(Refresh);

        Refresh();
    }

    public void Refresh()
    {
        if (_memoryReader.TryGetSnapshot(out var snapshot))
        {
            LatestSnapshot = snapshot;
            ApplyFilters();
            SnapshotUpdated?.Invoke(LatestSnapshot);
        }
    }

    private void ApplyFilters()
    {
        if (LatestSnapshot == null) return;

        // 1. 城池过滤与排序 (按预备兵降序)
        FilteredCities.Clear();
        var citiesQuery = LatestSnapshot.Cities.AsEnumerable();
        if (SelectedLordFilter != "全部")
        {
            if (SelectedLordFilter == "其他")
            {
                citiesQuery = citiesQuery.Where(c => c.LordName != "曹操" && c.LordName != "刘备" && c.LordName != "孙权" && c.LordName != "袁绍");
            }
            else
            {
                citiesQuery = citiesQuery.Where(c => c.LordName == SelectedLordFilter);
            }
        }

        foreach (var c in citiesQuery.OrderByDescending(c => c.ReserveTroops))
        {
            FilteredCities.Add(c);
        }

        // 2. 忠诚度雷达过滤 (忠诚度升序排序，优先显示动摇将领)
        LowLoyaltyGenerals.Clear();
        var generalsQuery = LatestSnapshot.Generals
            .Where(g => g.Status == GeneralStatus.Active);

        if (SelectedLordFilter != "全部")
        {
            if (SelectedLordFilter == "其他")
            {
                generalsQuery = generalsQuery.Where(g => g.LordName != "曹操" && g.LordName != "刘备" && g.LordName != "孙权" && g.LordName != "袁绍");
            }
            else
            {
                generalsQuery = generalsQuery.Where(g => g.LordName == SelectedLordFilter);
            }
        }

        foreach (var g in generalsQuery.OrderBy(g => g.Loyalty).Take(30))
        {
            LowLoyaltyGenerals.Add(g);
        }

        // 3. 在野与逃将流动
        WanderingGenerals.Clear();
        foreach (var g in LatestSnapshot.GetWanderingGenerals())
        {
            WanderingGenerals.Add(g);
        }

        // 4. 地脉宝物
        UndergroundItems.Clear();
        foreach (var item in LatestSnapshot.GetUndergroundItems())
        {
            UndergroundItems.Add(item);
        }
    }
}
