using System.Collections.ObjectModel;
using System.Windows.Input;
using Tianjige.Core.Models;
using Tianjige.Data.Memory;
using Tianjige.Data.Memory.Models;
using Tianjige.Data.Repositories;

namespace Tianjige.UI.ViewModels;

public sealed class ScenarioItemViewModel : ViewModelBase
{
    public ScenarioMetaInfo Meta { get; }
    public int ScenarioId => Meta.ScenarioId;
    public string Title => Meta.Title;
    public string Description => Meta.Description;
    public int StartYear => Meta.StartYear;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ScenarioItemViewModel(ScenarioMetaInfo meta, bool isSelected = false)
    {
        Meta = meta;
        _isSelected = isSelected;
    }
}

public sealed class LordFilterItemViewModel : ViewModelBase
{
    public string Name { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public LordFilterItemViewModel(string name, bool isSelected = false)
    {
        Name = name;
        _isSelected = isSelected;
    }
}

public class WorldSituationViewModel : ViewModelBase
{
    private readonly IMemoryReader _memoryReader;
    private readonly StaticRepository _repository;

    public ObservableCollection<LiveCityRecord> FilteredCities { get; } = new();
    public ObservableCollection<LiveGeneralRecord> LowLoyaltyGenerals { get; } = new();
    public ObservableCollection<LiveGeneralRecord> WanderingGenerals { get; } = new();
    public ObservableCollection<UndergroundItemSpot> UndergroundItems { get; } = new();
    public ObservableCollection<string> LordFilters { get; } = new();
    public ObservableCollection<LordFilterItemViewModel> LordFilterItems { get; } = new();
    public ObservableCollection<ScenarioItemViewModel> ScenarioItems { get; } = new();

    public IReadOnlyList<ScenarioMetaInfo> AvailableScenarios => StaticRepository.Scenarios;

    private ScenarioMetaInfo _selectedScenario;
    public ScenarioMetaInfo SelectedScenario
    {
        get => _selectedScenario;
        set
        {
            if (SetProperty(ref _selectedScenario, value))
            {
                _memoryReader.CurrentScenarioId = value.ScenarioId;
                foreach (var s in ScenarioItems)
                {
                    s.IsSelected = (s.ScenarioId == value.ScenarioId);
                }
                UpdateLordFilters();
                Refresh();
            }
        }
    }

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
                foreach (var lf in LordFilterItems)
                {
                    lf.IsSelected = (lf.Name == value);
                }
                ApplyFilters();
            }
        }
    }

    public bool HasNoCities => FilteredCities.Count == 0;
    public bool HasNoLowLoyaltyGenerals => LowLoyaltyGenerals.Count == 0;
    public bool HasNoWanderingGenerals => WanderingGenerals.Count == 0;
    public bool HasNoUndergroundItems => UndergroundItems.Count == 0;

    public ICommand SwitchSubSectionCommand { get; }
    public ICommand SelectLordFilterCommand { get; }
    public ICommand SelectScenarioCommand { get; }
    public ICommand RefreshCommand { get; }

    public WorldSituationViewModel() : this(null, null)
    {
    }

    public WorldSituationViewModel(IMemoryReader? memoryReader = null, StaticRepository? repository = null)
    {
        _repository = repository ?? new StaticRepository();
        _memoryReader = memoryReader ?? new MockMemoryReader(_repository);

        _selectedScenario = StaticRepository.Scenarios.FirstOrDefault() 
            ?? new ScenarioMetaInfo { ScenarioId = 1, Title = "黄巾之乱", StartYear = 184, Description = "" };
        _memoryReader.CurrentScenarioId = _selectedScenario.ScenarioId;

        SwitchSubSectionCommand = new RelayCommand<object>(param =>
        {
            if (param is int idx) SelectedSubSectionIndex = idx;
            else if (param is string s && int.TryParse(s, out var parsed)) SelectedSubSectionIndex = parsed;
        });

        SelectLordFilterCommand = new RelayCommand<string>(lord =>
        {
            if (!string.IsNullOrEmpty(lord)) SelectedLordFilter = lord;
        });

        SelectScenarioCommand = new RelayCommand<object>(param =>
        {
            int scenarioId = 1;
            if (param is int id) scenarioId = id;
            else if (param is string s && int.TryParse(s, out var parsed)) scenarioId = parsed;
            else if (param is ScenarioMetaInfo meta) scenarioId = meta.ScenarioId;

            var match = AvailableScenarios.FirstOrDefault(sc => sc.ScenarioId == scenarioId);
            if (match != null)
            {
                SelectedScenario = match;
            }
        });

        RefreshCommand = new RelayCommand(Refresh);

        // 初始化历史剧本选项
        ScenarioItems.Clear();
        foreach (var sc in AvailableScenarios)
        {
            ScenarioItems.Add(new ScenarioItemViewModel(sc, sc.ScenarioId == _selectedScenario.ScenarioId));
        }

        UpdateLordFilters();
        Refresh();
    }

    public void UpdateLordFilters()
    {
        LordFilters.Clear();
        LordFilterItems.Clear();

        LordFilters.Add("全部");
        LordFilterItems.Add(new LordFilterItemViewModel("全部", true));

        var scenarioMonarchs = _repository.GetScenarioMonarchs(SelectedScenario.ScenarioId);
        foreach (var m in scenarioMonarchs)
        {
            var simpName = AvatarResolver.ToSimplified(m.Name);
            if (!string.IsNullOrWhiteSpace(simpName) && simpName != "盗贼" && !LordFilters.Contains(simpName))
            {
                LordFilters.Add(simpName);
                LordFilterItems.Add(new LordFilterItemViewModel(simpName, false));
            }
        }

        LordFilters.Add("其他");
        LordFilterItems.Add(new LordFilterItemViewModel("其他", false));

        _selectedLordFilter = "全部";
        OnPropertyChanged(nameof(SelectedLordFilter));
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

        var mainLords = LordFilters.Where(x => x != "全部" && x != "其他").ToHashSet();

        // 1. 城池过滤与排序 (按预备兵降序)
        FilteredCities.Clear();
        var citiesQuery = LatestSnapshot.Cities.AsEnumerable();
        if (SelectedLordFilter != "全部")
        {
            if (SelectedLordFilter == "其他")
            {
                citiesQuery = citiesQuery.Where(c => !mainLords.Contains(c.LordName));
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
                generalsQuery = generalsQuery.Where(g => !mainLords.Contains(g.LordName));
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

        OnPropertyChanged(nameof(HasNoCities));
        OnPropertyChanged(nameof(HasNoLowLoyaltyGenerals));
        OnPropertyChanged(nameof(HasNoWanderingGenerals));
        OnPropertyChanged(nameof(HasNoUndergroundItems));
    }
}
