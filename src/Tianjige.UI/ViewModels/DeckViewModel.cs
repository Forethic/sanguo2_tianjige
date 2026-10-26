using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Tianjige.Core.Models;
using Tianjige.Core.Services;
using Tianjige.Data.Memory.Models;
using Tianjige.Data.Repositories;
using Tianjige.Data.Save;

namespace Tianjige.UI.ViewModels;

public class GeneralRowViewModel : ViewModelBase
{
    private readonly GeneralPotentialInfo _potential;

    public GeneralStaticInfo StaticInfo { get; }
    public LiveGeneralRecord? LiveRecord { get; }

    public int Id => StaticInfo.Id;
    public string Name => StaticInfo.Name;
    public string PinyinIndex => StaticInfo.PinyinIndex;
    public string AvatarPath => StaticInfo.Avatar;
    public int Force => LiveRecord != null ? LiveRecord.Force : StaticInfo.BaseForce;
    public int Intellect => LiveRecord != null ? LiveRecord.Intellect : StaticInfo.BaseIntellect;
    public int Level => LiveRecord != null ? LiveRecord.Level : 1;
    public int CurrentHp => LiveRecord != null ? LiveRecord.CurrentHp : 100;
    public int MaxHp => LiveRecord != null ? LiveRecord.MaxHp : 100;
    public int CurrentMp => LiveRecord != null ? LiveRecord.CurrentMp : 50;
    public int MaxMp => LiveRecord != null ? LiveRecord.MaxMp : 50;
    public int TroopCount => LiveRecord != null ? LiveRecord.TroopCount : 0;
    public int MaxTroopCount => LiveRecord != null ? LiveRecord.MaxTroopCount : 50;
    public int Loyalty => LiveRecord != null ? LiveRecord.Loyalty : 75;
    public string StatusText => LiveRecord != null ? LiveRecord.StatusDisplayName : "在野/未登场";
    public string LocationName => LiveRecord != null ? LiveRecord.CityName : "游历天下";
    public string MonarchName => LiveRecord != null ? LiveRecord.LordName : "在野";
    public string FactionColor => LiveRecord != null ? SaveGameReader.GetMonarchColor(LiveRecord.LordName) : "#736052";
    
    public GeneralPotentialInfo Potential => _potential;
    public int PotentialScore => _potential.OverallScore;
    public string PotentialTier => _potential.TierBadge;
    public string PotentialTierColor => _potential.TierColor;
    public string PotentialComment => _potential.TacticalComment;
    public int MaxSkillLevel => _potential.MaxSkillLevel;
    public string GrowthStage => _potential.GrowthStage;
    public string GrowthComment => _potential.GrowthComment;
    public string HighlightBadgesText { get; }

    public string FirstChar { get; }
    public bool HasAvatar { get; }
    public bool HasNoAvatar { get; }

    public bool HasAmbush { get; }
    public bool HasHpRegen { get; }
    public bool HasMpRegen { get; }
    public bool HasUltimate { get; }
    public bool HasEscape { get; }
    public bool HasReinforce { get; }

    public GeneralRowViewModel(GeneralStaticInfo staticInfo, LiveGeneralRecord? liveRecord = null)
    {
        StaticInfo = staticInfo;
        LiveRecord = liveRecord;
        _potential = staticInfo.Potential ?? GeneralPotentialService.Evaluate(staticInfo);

        FirstChar = !string.IsNullOrEmpty(staticInfo.Name) ? staticInfo.Name.Substring(0, 1) : "将";
        HasAvatar = !string.IsNullOrEmpty(staticInfo.Avatar) && System.IO.File.Exists(staticInfo.Avatar);
        HasNoAvatar = !HasAvatar;

        HighlightBadgesText = _potential.HighlightBadges.Count > 0 ? string.Join(" · ", _potential.HighlightBadges) : "常规格局";
        HasAmbush = _potential.HighlightBadges.Any(b => b.Contains("伏兵"));
        HasHpRegen = _potential.HighlightBadges.Any(b => b.Contains("体疗") || b.Contains("生疗") || b.Contains("体力"));
        HasMpRegen = _potential.HighlightBadges.Any(b => b.Contains("技力"));
        HasUltimate = _potential.MaxSkillLevel >= 38;
        HasEscape = _potential.HighlightBadges.Any(b => b.Contains("金蝉脱壳")) || staticInfo.SkillTree.Any(s => s.SkillName.Contains("金蝉脱壳"));
        HasReinforce = _potential.HighlightBadges.Any(b => b.Contains("增员") || b.Contains("强化"));
    }
}

/// <summary>
/// 势力全景分析模型 (Requirement 5)
/// </summary>
public sealed class FactionSummaryViewModel : ViewModelBase
{
    public required string MonarchName { get; init; }
    public required string FactionColor { get; init; }
    public required int CityCount { get; init; }
    public required int GeneralCount { get; init; }
    public required int TotalTroops { get; init; }
    public required int TotalReserveTroops { get; init; }
    public List<string> CityNames { get; init; } = new();
    public string CityNamesDisplay => CityNames.Count > 0 ? string.Join(" · ", CityNames) : "流亡/未据城";
}

/// <summary>
/// 城池全景分析模型 (Requirement 5)
/// </summary>
public sealed class CitySummaryViewModel : ViewModelBase
{
    public required int CityId { get; init; }
    public required string CityName { get; init; }
    public required string LordName { get; init; }
    public required string FactionColor { get; init; }
    public required int Defense { get; init; }
    public required int ReserveTroops { get; init; }
    public required int GeneralCount { get; init; }
    public required int TotalTroops { get; init; }
    public List<GeneralRowViewModel> Generals { get; init; } = new();
}

public class DeckViewModel : ViewModelBase
{
    private readonly StaticRepository _repository;
    private readonly SaveGameService _saveService;
    private readonly SquadManagementService _squadService;

    // 存档状态
    private bool _hasSelectedSave;
    public bool HasSelectedSave
    {
        get => _hasSelectedSave;
        set
        {
            if (SetProperty(ref _hasSelectedSave, value))
            {
                OnPropertyChanged(nameof(HasNoSelectedSave));
            }
        }
    }

    public bool HasNoSelectedSave => !HasSelectedSave;

    private SaveSlotSummary? _selectedSlotSummary;
    public SaveSlotSummary? SelectedSlotSummary
    {
        get => _selectedSlotSummary;
        set => SetProperty(ref _selectedSlotSummary, value);
    }

    private SaveGameSnapshot? _currentSnapshot;
    public SaveGameSnapshot? CurrentSnapshot
    {
        get => _currentSnapshot;
        set => SetProperty(ref _currentSnapshot, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _statusMessage = "请选择要透析的官方游戏存档";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private int _selectedTabIndex = 0;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    public ObservableCollection<SaveSlotSummary> SaveSlots { get; } = new();

    // 势力与城池版图数据 (Requirement 5)
    public ObservableCollection<FactionSummaryViewModel> FactionsSummary { get; } = new();
    public ObservableCollection<CitySummaryViewModel> CitiesSummary { get; } = new();

    private FactionSummaryViewModel? _selectedFactionSummary;
    public FactionSummaryViewModel? SelectedFactionSummary
    {
        get => _selectedFactionSummary;
        set => SetProperty(ref _selectedFactionSummary, value);
    }

    private CitySummaryViewModel? _selectedCitySummary;
    public CitySummaryViewModel? SelectedCitySummary
    {
        get => _selectedCitySummary;
        set => SetProperty(ref _selectedCitySummary, value);
    }

    public bool HasCities => CitiesSummary.Count > 0;

    // 武将筛选与排序
    private System.Threading.CancellationTokenSource? _searchCts;
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _searchCts?.Cancel();
                    ApplyFilter();
                }
                else
                {
                    _searchCts?.Cancel();
                    _searchCts = new System.Threading.CancellationTokenSource();
                    var token = _searchCts.Token;
                    System.Threading.Tasks.Task.Delay(100, token).ContinueWith(t =>
                    {
                        if (!t.IsCanceled)
                        {
                            System.Windows.Application.Current?.Dispatcher.InvokeAsync(ApplyFilter);
                        }
                    }, System.Threading.Tasks.TaskScheduler.Default);
                }
            }
        }
    }

    private string _selectedStatus = "全部";
    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
                ApplyFilter();
        }
    }

    private string _selectedFaction = "全部";
    public string SelectedFaction
    {
        get => _selectedFaction;
        set
        {
            if (SetProperty(ref _selectedFaction, value))
                ApplyFilter();
        }
    }

    private string _selectedPotentialTier = "全部";
    public string SelectedPotentialTier
    {
        get => _selectedPotentialTier;
        set
        {
            if (SetProperty(ref _selectedPotentialTier, value))
                ApplyFilter();
        }
    }

    private string _selectedSkillFilter = "全部";
    public string SelectedSkillFilter
    {
        get => _selectedSkillFilter;
        set
        {
            if (SetProperty(ref _selectedSkillFilter, value))
                ApplyFilter();
        }
    }

    private int _minForce = 0;
    public int MinForce
    {
        get => _minForce;
        set
        {
            if (SetProperty(ref _minForce, value))
                ApplyFilter();
        }
    }

    private int _maxForce = 100;
    public int MaxForce
    {
        get => _maxForce;
        set
        {
            if (SetProperty(ref _maxForce, value))
                ApplyFilter();
        }
    }

    private int _minIntellect = 0;
    public int MinIntellect
    {
        get => _minIntellect;
        set
        {
            if (SetProperty(ref _minIntellect, value))
                ApplyFilter();
        }
    }

    private int _maxIntellect = 100;
    public int MaxIntellect
    {
        get => _maxIntellect;
        set
        {
            if (SetProperty(ref _maxIntellect, value))
                ApplyFilter();
        }
    }

    private string _currentSortColumn = "PotentialScore";
    public string CurrentSortColumn
    {
        get => _currentSortColumn;
        set => SetProperty(ref _currentSortColumn, value);
    }

    private bool _isSortAscending = false;
    public bool IsSortAscending
    {
        get => _isSortAscending;
        set => SetProperty(ref _isSortAscending, value);
    }

    public ObservableCollection<string> AvailableFactions { get; } = new() { "全部" };
    public ObservableCollection<string> StatusOptions { get; } = new() { "全部", "出仕", "在野", "君主", "俘虏", "死亡" };
    public ObservableCollection<string> PotentialTierOptions { get; } = new() { "全部", "绝世·SSS", "核心·SS", "中坚·S", "平庸·A", "驽钝·B" };
    public ObservableCollection<string> SkillFilterOptions { get; } = new() { "全部", "伏兵系", "恢复体力", "恢复技力", "增员/强化", "终极必杀", "金蝉脱壳" };

    private readonly List<GeneralRowViewModel> _allGenerals = new();
    public RangeObservableCollection<GeneralRowViewModel> FilteredGenerals { get; } = new();

    private int _filteredCount;
    public int FilteredCount
    {
        get => _filteredCount;
        set => SetProperty(ref _filteredCount, value);
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    // 编组功能
    private bool _isGlobalSquadMode = true;
    public bool IsGlobalSquadMode
    {
        get => _isGlobalSquadMode;
        set
        {
            if (SetProperty(ref _isGlobalSquadMode, value))
            {
                OnPropertyChanged(nameof(IsSaveSquadMode));
                LoadSquads();
            }
        }
    }

    public bool IsSaveSquadMode
    {
        get => !_isGlobalSquadMode;
        set => IsGlobalSquadMode = !value;
    }

    public ObservableCollection<SquadModel> Squads { get; } = new();
    public ObservableCollection<GeneralRowViewModel> SelectedSquadMembers { get; } = new();

    private SquadModel? _selectedSquad;
    public SquadModel? SelectedSquad
    {
        get => _selectedSquad;
        set
        {
            if (SetProperty(ref _selectedSquad, value))
            {
                RefreshSelectedSquadMembers();
            }
        }
    }

    public void RefreshSelectedSquadMembers()
    {
        SelectedSquadMembers.Clear();
        if (SelectedSquad == null) return;

        foreach (var memberName in SelectedSquad.MemberNames)
        {
            var match = _allGenerals.FirstOrDefault(g => g.Name == memberName);
            if (match != null)
            {
                SelectedSquadMembers.Add(match);
            }
            else
            {
                var staticInfo = _repository.GetGeneralByName(memberName) ?? new GeneralStaticInfo
                {
                    Id = 9999,
                    Name = memberName,
                    PinyinIndex = "",
                    SealTier = "白身",
                    BaseForce = 70,
                    BaseIntellect = 70
                };
                SelectedSquadMembers.Add(new GeneralRowViewModel(staticInfo));
            }
        }
    }

    // 事件与命令委托
    public Action<GeneralStaticInfo, LiveGeneralRecord?>? OpenGeneralCardRequested { get; set; }
    public Action? OpenTroopCompassRequested { get; set; }
    public Action? OpenTroopCounterRequested { get; set; }
    public Action? OpenFormationCounterRequested { get; set; }

    public ICommand SelectSlotCommand { get; }
    public ICommand SwitchSlotCommand { get; }
    public ICommand RefreshSlotCommand { get; }
    public ICommand SortCommand { get; }
    public ICommand QuickSortCommand { get; }
    public ICommand SwitchToTabCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand OpenGeneralCardCommand { get; }
    public ICommand OpenTroopCompassCommand { get; }
    public ICommand OpenTroopCounterCommand { get; }
    public ICommand OpenFormationCounterCommand { get; }
    public ICommand EnterFreeModeCommand { get; }
    public ICommand AddSquadCommand { get; }
    public ICommand DeleteSquadCommand { get; }
    public ICommand SelectSquadCommand { get; }
    public ICommand AddMemberToSquadCommand { get; }
    public ICommand RemoveMemberFromSquadCommand { get; }
    public ICommand SaveSquadsCommand { get; }

    public DeckViewModel(string gameDirectory, StaticRepository? repository = null, SaveGameService? saveService = null, SquadManagementService? squadService = null)
    {
        _repository = repository ?? new StaticRepository();
        _saveService = saveService ?? new SaveGameService(_repository);
        _squadService = squadService ?? new SquadManagementService(_repository);

        _saveService.Initialize(gameDirectory, 1);
        _saveService.OnSaveUpdated += OnSaveFileUpdated;
        _saveService.OnSlotsRefreshed += OnSlotsRefreshed;

        SwitchToTabCommand = new RelayCommand<object>(param =>
        {
            if (param is int i) SelectedTabIndex = i;
            else if (param is string s && int.TryParse(s, out var v)) SelectedTabIndex = v;
        });

        QuickSortCommand = new RelayCommand<string>(col =>
        {
            if (string.IsNullOrEmpty(col)) return;
            if (CurrentSortColumn == col)
            {
                IsSortAscending = !IsSortAscending;
            }
            else
            {
                CurrentSortColumn = col;
                IsSortAscending = (col == "Name");
            }
            ApplyFilter();
        });

        SelectSlotCommand = new RelayCommand<object>(async param =>
        {
            int slot = 1;
            if (param is SaveSlotSummary sum) slot = sum.SlotNumber;
            else if (param is int n) slot = n;
            else if (param is string s && int.TryParse(s, out var p)) slot = p;

            await LoadSlotAsync(slot);
        });

        SwitchSlotCommand = new RelayCommand(() =>
        {
            SelectedTabIndex = 3; // 直接切入第4标签：存档透析
            StatusMessage = "请在下方槽位中选取游戏存档进行实时推演";
            RefreshSlotsList();
        });

        RefreshSlotCommand = new RelayCommand(async () =>
        {
            if (SelectedSlotSummary != null)
            {
                await LoadSlotAsync(SelectedSlotSummary.SlotNumber);
            }
        });

        SortCommand = new RelayCommand<string>(column =>
        {
            if (string.IsNullOrEmpty(column)) return;
            if (CurrentSortColumn == column)
            {
                IsSortAscending = !IsSortAscending;
            }
            else
            {
                CurrentSortColumn = column;
                IsSortAscending = false; // 默认大数优先
            }
            ApplyFilter();
        });

        ResetFiltersCommand = new RelayCommand(() =>
        {
            SearchText = string.Empty;
            SelectedStatus = "全部";
            SelectedFaction = "全部";
            SelectedPotentialTier = "全部";
            SelectedSkillFilter = "全部";
            MinForce = 0;
            MaxForce = 100;
            MinIntellect = 0;
            MaxIntellect = 100;
        });

        OpenGeneralCardCommand = new RelayCommand<object>(param =>
        {
            if (param is GeneralRowViewModel row)
            {
                OpenGeneralCardRequested?.Invoke(row.StaticInfo, row.LiveRecord);
            }
            else if (param is GeneralStaticInfo stat)
            {
                var live = _currentSnapshot?.Generals.FirstOrDefault(g => g.GeneralId == stat.Id);
                OpenGeneralCardRequested?.Invoke(stat, live);
            }
        });

        OpenTroopCompassCommand = new RelayCommand(() =>
        {
            OpenTroopCompassRequested?.Invoke();
        });

        OpenTroopCounterCommand = new RelayCommand(() =>
        {
            OpenTroopCounterRequested?.Invoke();
        });

        OpenFormationCounterCommand = new RelayCommand(() =>
        {
            OpenFormationCounterRequested?.Invoke();
        });

        AddSquadCommand = new RelayCommand(() =>
        {
            var newSquad = new SquadModel
            {
                Name = $"战队{Squads.Count + 1:D2}",
                LeaderName = "主将待定",
                Notes = "自定义战术梯队"
            };
            Squads.Add(newSquad);
            SelectedSquad = newSquad;
            SaveSquads();
        });

        DeleteSquadCommand = new RelayCommand<SquadModel>(squad =>
        {
            if (squad != null)
            {
                Squads.Remove(squad);
                if (SelectedSquad == squad) SelectedSquad = Squads.FirstOrDefault();
                SaveSquads();
            }
        });

        SelectSquadCommand = new RelayCommand<SquadModel>(squad =>
        {
            if (squad != null) SelectedSquad = squad;
        });

        AddMemberToSquadCommand = new RelayCommand<string>(genName =>
        {
            if (SelectedSquad != null && !string.IsNullOrWhiteSpace(genName) && SelectedSquad.MemberNames.Count < 5)
            {
                if (!SelectedSquad.MemberNames.Contains(genName))
                {
                    SelectedSquad.MemberNames.Add(genName);
                    if (string.IsNullOrWhiteSpace(SelectedSquad.LeaderName) || SelectedSquad.LeaderName == "主将待定")
                    {
                        SelectedSquad.LeaderName = genName;
                    }
                    _squadService.RecalculateSquadMetrics(SelectedSquad);
                    RefreshSelectedSquadMembers();
                    OnPropertyChanged(nameof(SelectedSquad));
                    SaveSquads();
                }
            }
        });

        RemoveMemberFromSquadCommand = new RelayCommand<string>(genName =>
        {
            if (SelectedSquad != null && !string.IsNullOrWhiteSpace(genName))
            {
                SelectedSquad.MemberNames.Remove(genName);
                if (SelectedSquad.LeaderName == genName)
                {
                    SelectedSquad.LeaderName = SelectedSquad.MemberNames.FirstOrDefault() ?? "主将待定";
                }
                _squadService.RecalculateSquadMetrics(SelectedSquad);
                RefreshSelectedSquadMembers();
                OnPropertyChanged(nameof(SelectedSquad));
                SaveSquads();
            }
        });

        SaveSquadsCommand = new RelayCommand(SaveSquads);

        EnterFreeModeCommand = new RelayCommand(() =>
        {
            PopulateStaticGenerals();
            SelectedTabIndex = 0;
            StatusMessage = "天下名将录 · 离线全大典速查模式（共400位名将与潜力梯队）";
        });

        // 默认载入全量400名将静态大典，保证无需存档即可自由排序、筛选与推演
        PopulateStaticGenerals();
        RefreshSlotsList();
        LoadSquads();
    }

    public void RefreshSlotsList()
    {
        var slots = _saveService.GetSlots();
        SaveSlots.Clear();
        foreach (var s in slots)
        {
            SaveSlots.Add(s);
        }
    }

    public async Task LoadSlotAsync(int slot)
    {
        IsLoading = true;
        StatusMessage = $"正在读取第 {slot} 号存档 (等待磁盘完整写入)...";

        try
        {
            var snapshot = await _saveService.LoadSlotSnapshotAsync(slot);
            if (snapshot != null)
            {
                CurrentSnapshot = snapshot;
                SelectedSlotSummary = SaveSlots.FirstOrDefault(s => s.SlotNumber == slot) ?? new SaveSlotSummary
                {
                    SlotNumber = slot,
                    FilePath = snapshot.FilePath,
                    Exists = true,
                    IsEmpty = false,
                    SaveTitle = snapshot.SaveTitle,
                    MonarchName = snapshot.MonarchName,
                    FactionColor = snapshot.FactionColor,
                    GameYear = snapshot.GameYear,
                    CityCount = snapshot.Cities.Count,
                    GeneralCount = snapshot.Generals.Count,
                    ScenarioPeriodName = snapshot.ScenarioPeriodName
                };

                PopulateGeneralsFromSnapshot(snapshot);
                HasSelectedSave = true;
                SelectedTabIndex = 0; // 自动跳转回名将大典，展现最新战力
                StatusMessage = $"已装载存档: {snapshot.ScenarioPeriodName} {snapshot.GameYear}年 · 君主【{snapshot.MonarchName}】 城池{snapshot.Cities.Count}座 武将{snapshot.Generals.Count}员";
            }
            else
            {
                StatusMessage = $"第 {slot} 号存档为空记录，已载入三国群英传2全名将大典";
                PopulateStaticGenerals();
                HasSelectedSave = false;
                SelectedTabIndex = 0;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"读取存档异常: {ex.Message}，已恢复全名将库";
            PopulateStaticGenerals();
            HasSelectedSave = false;
            SelectedTabIndex = 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void PopulateGeneralsFromSnapshot(SaveGameSnapshot snapshot)
    {
        _allGenerals.Clear();
        var staticGenerals = _repository.GetGenerals();
        var liveLookup = snapshot.Generals.ToDictionary(g => g.GeneralId);

        var factionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "全部" };

        foreach (var stat in staticGenerals)
        {
            liveLookup.TryGetValue(stat.Id, out var live);
            var row = new GeneralRowViewModel(stat, live);
            _allGenerals.Add(row);

            if (live != null && !string.IsNullOrWhiteSpace(live.LordName) && live.LordName != "在野")
            {
                factionSet.Add(live.LordName);
            }
        }

        AvailableFactions.Clear();
        foreach (var fac in factionSet.OrderBy(f => f == "全部" ? "0" : f))
        {
            AvailableFactions.Add(fac);
        }

        TotalCount = _allGenerals.Count;
        ApplyFilter();
        RefreshSelectedSquadMembers();

        PopulateFactionsAndCities(snapshot);
    }

    private void PopulateFactionsAndCities(SaveGameSnapshot snapshot)
    {
        FactionsSummary.Clear();
        CitiesSummary.Clear();

        // 1. 势力统计
        var monarchGroups = snapshot.Generals
            .Where(g => !string.IsNullOrWhiteSpace(g.LordName) && g.LordName != "在野" && g.LordName != "无")
            .GroupBy(g => g.LordName)
            .OrderByDescending(g => g.Count());

        foreach (var group in monarchGroups)
        {
            var monarch = group.Key;
            var genList = group.ToList();
            var citiesRuled = snapshot.Cities.Where(c => string.Equals(c.LordName, monarch, StringComparison.OrdinalIgnoreCase)).ToList();

            var summary = new FactionSummaryViewModel
            {
                MonarchName = monarch,
                FactionColor = SaveGameReader.GetMonarchColor(monarch),
                CityCount = citiesRuled.Count,
                GeneralCount = genList.Count,
                TotalTroops = genList.Sum(g => g.TroopCount),
                TotalReserveTroops = citiesRuled.Sum(c => c.ReserveTroops),
                CityNames = citiesRuled.Select(c => c.Name).ToList()
            };
            FactionsSummary.Add(summary);
        }

        // 2. 城池统计
        var genLookupByCity = _allGenerals
            .Where(g => g.LiveRecord != null)
            .GroupBy(g => g.LocationName)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var c in snapshot.Cities)
        {
            genLookupByCity.TryGetValue(c.Name, out var cityGens);
            cityGens ??= new List<GeneralRowViewModel>();

            var cityVm = new CitySummaryViewModel
            {
                CityId = c.CityId,
                CityName = c.Name,
                LordName = c.LordName,
                FactionColor = SaveGameReader.GetMonarchColor(c.LordName),
                Defense = c.Defense,
                ReserveTroops = c.ReserveTroops,
                GeneralCount = cityGens.Count,
                TotalTroops = cityGens.Sum(g => g.TroopCount),
                Generals = cityGens
            };
            CitiesSummary.Add(cityVm);
        }

        SelectedFactionSummary = FactionsSummary.FirstOrDefault();
        SelectedCitySummary = CitiesSummary.FirstOrDefault();
        OnPropertyChanged(nameof(HasCities));
    }

    private void PopulateStaticGenerals()
    {
        _allGenerals.Clear();
        FactionsSummary.Clear();
        CitiesSummary.Clear();
        SelectedFactionSummary = null;
        SelectedCitySummary = null;
        OnPropertyChanged(nameof(HasCities));

        var staticGenerals = _repository.GetGenerals();
        foreach (var stat in staticGenerals)
        {
            _allGenerals.Add(new GeneralRowViewModel(stat, null));
        }

        AvailableFactions.Clear();
        AvailableFactions.Add("全部");
        TotalCount = _allGenerals.Count;
        ApplyFilter();
        RefreshSelectedSquadMembers();
    }

    private void ApplyFilter()
    {
        IEnumerable<GeneralRowViewModel> query = _allGenerals;

        // 姓名与拼音过滤
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var text = SearchText.Trim();
            query = query.Where(g =>
                g.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                g.PinyinIndex.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        // 状态过滤
        if (SelectedStatus != "全部")
        {
            query = query.Where(g => g.StatusText.Contains(SelectedStatus));
        }

        // 势力过滤
        if (SelectedFaction != "全部")
        {
            query = query.Where(g => string.Equals(g.MonarchName, SelectedFaction, StringComparison.OrdinalIgnoreCase));
        }

        // 潜力档位过滤
        if (SelectedPotentialTier != "全部")
        {
            query = query.Where(g => g.PotentialTier.Contains(SelectedPotentialTier));
        }

        // 技能组特色过滤
        if (SelectedSkillFilter != "全部")
        {
            query = SelectedSkillFilter switch
            {
                "伏兵系" => query.Where(g => g.HasAmbush),
                "恢复体力" => query.Where(g => g.HasHpRegen),
                "恢复技力" => query.Where(g => g.HasMpRegen),
                "增员/强化" => query.Where(g => g.HighlightBadgesText.Contains("增员") || g.HighlightBadgesText.Contains("强化")),
                "终极必杀" => query.Where(g => g.MaxSkillLevel >= 38),
                "金蝉脱壳" => query.Where(g => g.HighlightBadgesText.Contains("金蝉脱壳") || g.StaticInfo.SkillTree.Any(s => s.SkillName.Contains("金蝉脱壳"))),
                _ => query
            };
        }

        // 武力与智力范围
        query = query.Where(g => g.Force >= MinForce && g.Force <= MaxForce);
        query = query.Where(g => g.Intellect >= MinIntellect && g.Intellect <= MaxIntellect);

        // 排序
        query = (CurrentSortColumn, IsSortAscending) switch
        {
            ("Force", true) => query.OrderBy(g => g.Force),
            ("Force", false) => query.OrderByDescending(g => g.Force),
            ("Intellect", true) => query.OrderBy(g => g.Intellect),
            ("Intellect", false) => query.OrderByDescending(g => g.Intellect),
            ("PotentialScore", true) => query.OrderBy(g => g.PotentialScore),
            ("PotentialScore", false) => query.OrderByDescending(g => g.PotentialScore),
            ("Level", true) => query.OrderBy(g => g.Level),
            ("Level", false) => query.OrderByDescending(g => g.Level),
            ("Name", true) => query.OrderBy(g => g.Name),
            ("Name", false) => query.OrderByDescending(g => g.Name),
            ("Location", true) => query.OrderBy(g => g.LocationName),
            ("Location", false) => query.OrderByDescending(g => g.LocationName),
            ("Status", true) => query.OrderBy(g => g.StatusText),
            ("Status", false) => query.OrderByDescending(g => g.StatusText),
            _ => query.OrderByDescending(g => g.PotentialScore)
        };

        FilteredGenerals.ReplaceRange(query);
        FilteredCount = FilteredGenerals.Count;
    }

    private void LoadSquads()
    {
        Squads.Clear();
        List<SquadModel> list;
        if (IsGlobalSquadMode)
        {
            list = _squadService.LoadGlobalSquads();
        }
        else
        {
            int slot = SelectedSlotSummary?.SlotNumber ?? 1;
            list = _squadService.LoadSaveSquads(slot);
        }

        foreach (var sq in list)
        {
            Squads.Add(sq);
        }
        SelectedSquad = Squads.FirstOrDefault();
    }

    private void SaveSquads()
    {
        var list = Squads.ToList();
        if (IsGlobalSquadMode)
        {
            _squadService.SaveGlobalSquads(list);
        }
        else
        {
            int slot = SelectedSlotSummary?.SlotNumber ?? 1;
            _squadService.SaveSaveSquads(slot, list);
        }
    }

    private void OnSaveFileUpdated(SaveGameSnapshot snapshot)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            if (HasSelectedSave && SelectedSlotSummary != null && SelectedSlotSummary.SlotNumber == snapshot.SlotNumber)
            {
                CurrentSnapshot = snapshot;
                PopulateGeneralsFromSnapshot(snapshot);
                StatusMessage = $"[已自动刷新] 游戏存档更新: {snapshot.GameYear}年 君主【{snapshot.MonarchName}】 城池{snapshot.Cities.Count}座 武将{snapshot.Generals.Count}员";
            }
            RefreshSlotsList();
        });
    }

    private void OnSlotsRefreshed(IReadOnlyList<SaveSlotSummary> slots)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            SaveSlots.Clear();
            foreach (var s in slots) SaveSlots.Add(s);
        });
    }
}
