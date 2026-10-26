using System.Collections.ObjectModel;
using System.Windows.Input;
using Tianjige.Core.Models;
using Tianjige.Core.Services;
using Tianjige.Data.Repositories;

namespace Tianjige.UI.ViewModels;

public sealed class RosterGeneralItem : ViewModelBase
{
    public GeneralStaticInfo General { get; }
    public string Name => General.Name;
    public string SealTier => General.SealTier;
    public byte BaseForce => General.BaseForce;
    public byte BaseIntellect => General.BaseIntellect;
    public string? AvatarPath { get; }
    public string Initial => !string.IsNullOrWhiteSpace(Name) ? Name[..1] : "将";
    public string MainStatText => BaseForce >= BaseIntellect ? $"武{BaseForce}" : $"智{BaseIntellect}";
    public string MainStatColor => BaseForce >= BaseIntellect ? "#E57373" : "#81C784";

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public RosterGeneralItem(GeneralStaticInfo general, bool isSelected = false, string? avatarPath = null)
    {
        General = general;
        _isSelected = isSelected;
        AvatarPath = avatarPath;
    }
}

public sealed class GeneralStarViewModel : ViewModelBase
{
    private static readonly HashSet<string> FeaturedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "关羽", "诸葛亮", "赵云", "曹操", "周瑜", "吕布", "司马懿", "张飞",
        "陆逊", "郭嘉", "孙策", "貂蝉", "华佗", "夏侯惇", "甘宁", "太史慈",
        "黄忠", "马超", "庞统", "刘备", "孙权", "张辽", "魏延", "姜维"
    };

    private static readonly HashSet<string> MonarchNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "曹操", "刘备", "孙权", "袁绍", "董卓", "孙坚", "袁术", "刘表",
        "马腾", "公孙瓒", "陶谦", "孔融", "刘璋", "张鲁", "孟获", "严白虎", "韩玄"
    };

    private readonly StaticRepository _repository;
    private readonly SkillInvertedIndex _invertedIndex;
    private readonly List<GeneralStaticInfo> _allGenerals;

    private string _searchText = string.Empty;
    private string _rosterCategory = "精选"; // "精选", "无双", "谋主", "君主", "良将", "全谱"
    private string _searchStatusText = string.Empty;
    private GeneralStaticInfo? _selectedGeneral;
    private string _reverseLookupSkillName = string.Empty;
    private bool _isReverseLookupOpen;
    private IReadOnlyList<SkillMasteryInfo> _reverseLookupMasters = Array.Empty<SkillMasteryInfo>();

    private bool _isRosterExpanded = false;
    public bool IsRosterExpanded
    {
        get => _isRosterExpanded;
        set
        {
            if (SetProperty(ref _isRosterExpanded, value))
            {
                OnPropertyChanged(nameof(RosterMaxHeight));
                OnPropertyChanged(nameof(RosterToggleText));
            }
        }
    }

    public double RosterMaxHeight => _isRosterExpanded ? 260.0 : 108.0;
    public string RosterToggleText => _isRosterExpanded ? "收起名册 ▲" : "展开名册 ▼";

    public RangeObservableCollection<RosterGeneralItem> DisplayedGenerals { get; } = new();
    public ObservableCollection<GeneralStaticInfo> FilteredGenerals { get; } = new();
    public ObservableCollection<SkillTreeNode> DisplayedSkills { get; } = new();

    private int _skillFilter = 0; // 0: 全部, 1: 仅武将技, 2: 仅军师技
    public int SkillFilter
    {
        get => _skillFilter;
        set
        {
            if (SetProperty(ref _skillFilter, value))
            {
                UpdateDisplayedSkills();
                OnPropertyChanged(nameof(IsFilterAll));
                OnPropertyChanged(nameof(IsFilterGeneral));
                OnPropertyChanged(nameof(IsFilterMilitary));
            }
        }
    }

    public bool IsFilterAll => _skillFilter == 0;
    public bool IsFilterGeneral => _skillFilter == 1;
    public bool IsFilterMilitary => _skillFilter == 2;

    public int AllSkillsCount => SelectedGeneral?.SkillTree.Count ?? 0;
    public int GeneralSkillsCount => SelectedGeneral?.SkillTree.Count(s => s.Category == SkillCategory.General) ?? 0;
    public int MilitarySkillsCount => SelectedGeneral?.SkillTree.Count(s => s.Category == SkillCategory.Military) ?? 0;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OnPropertyChanged(nameof(IsSearching));
                OnPropertyChanged(nameof(IsBrowsing));
                ApplyFilter();
            }
        }
    }

    public bool IsSearching => !string.IsNullOrWhiteSpace(_searchText);
    public bool IsBrowsing => string.IsNullOrWhiteSpace(_searchText);

    public string RosterCategory
    {
        get => _rosterCategory;
        set
        {
            if (SetProperty(ref _rosterCategory, value))
            {
                OnPropertyChanged(nameof(IsCategoryFeatured));
                OnPropertyChanged(nameof(IsCategoryWuShuang));
                OnPropertyChanged(nameof(IsCategoryMouZhu));
                OnPropertyChanged(nameof(IsCategoryMonarch));
                OnPropertyChanged(nameof(IsCategoryLiangJiang));
                OnPropertyChanged(nameof(IsCategoryAll));
                ApplyFilter();
            }
        }
    }

    public bool IsCategoryFeatured => _rosterCategory == "精选";
    public bool IsCategoryWuShuang => _rosterCategory == "无双";
    public bool IsCategoryMouZhu => _rosterCategory == "谋主";
    public bool IsCategoryMonarch => _rosterCategory == "君主";
    public bool IsCategoryLiangJiang => _rosterCategory == "良将";
    public bool IsCategoryAll => _rosterCategory == "全谱";

    public string RosterSummaryText
    {
        get
        {
            if (IsSearching)
            {
                return $"◈ 检索匹配 · 共 {DisplayedGenerals.Count} 位武将";
            }
            return _rosterCategory switch
            {
                "无双" => $"⚔ 无双猛将 · 共 {DisplayedGenerals.Count} 位",
                "谋主" => $"☯ 智囊谋主 · 共 {DisplayedGenerals.Count} 位",
                "君主" => $"⚑ 乱世君主 · 共 {DisplayedGenerals.Count} 位",
                "良将" => $"✦ 当世良将 · 共 {DisplayedGenerals.Count} 位",
                "全谱" => $"☰ 登场全谱 · 共 {DisplayedGenerals.Count} 位",
                _ => $"★ 星选名将 · 共 {DisplayedGenerals.Count} 位"
            };
        }
    }

    public string SearchStatusText
    {
        get => _searchStatusText;
        private set => SetProperty(ref _searchStatusText, value);
    }

    public GeneralStaticInfo? SelectedGeneral
    {
        get => _selectedGeneral;
        set
        {
            if (SetProperty(ref _selectedGeneral, value))
            {
                UpdateDisplayedSkills();
                SyncRosterSelection();
                OnPropertyChanged(nameof(SelectedGeneralAvatarPath));
                OnPropertyChanged(nameof(SelectedGeneralInitial));
                OnPropertyChanged(nameof(AllSkillsCount));
                OnPropertyChanged(nameof(GeneralSkillsCount));
                OnPropertyChanged(nameof(MilitarySkillsCount));
            }
        }
    }

    public string? SelectedGeneralAvatarPath =>
        _selectedGeneral != null 
            ? (_repository.ResolveAvatarByName(_selectedGeneral.Name) ?? _repository.ResolveAvatarPath(_selectedGeneral.Avatar)) 
            : null;

    public string SelectedGeneralInitial =>
        !string.IsNullOrWhiteSpace(_selectedGeneral?.Name) ? _selectedGeneral.Name[..1] : "将";

    public string ReverseLookupSkillName
    {
        get => _reverseLookupSkillName;
        set => SetProperty(ref _reverseLookupSkillName, value);
    }

    public bool IsReverseLookupOpen
    {
        get => _isReverseLookupOpen;
        set => SetProperty(ref _isReverseLookupOpen, value);
    }

    public IReadOnlyList<SkillMasteryInfo> ReverseLookupMasters
    {
        get => _reverseLookupMasters;
        set => SetProperty(ref _reverseLookupMasters, value);
    }

    public ICommand SelectGeneralCommand { get; }
    public ICommand SetCategoryCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand LookupSkillCommand { get; }
    public ICommand CloseReverseLookupCommand { get; }
    public ICommand SetSkillFilterCommand { get; }
    public ICommand ToggleRosterExpandedCommand { get; }

    public GeneralStarViewModel() : this(null)
    {
    }

    public GeneralStarViewModel(StaticRepository? repository = null)
    {
        _repository = repository ?? new StaticRepository();
        _allGenerals = _repository.GetAllGenerals().ToList();
        _invertedIndex = new SkillInvertedIndex(_allGenerals);

        SetCategoryCommand = new RelayCommand<string>(cat =>
        {
            if (!string.IsNullOrWhiteSpace(cat))
            {
                RosterCategory = cat;
            }
        });

        ToggleRosterExpandedCommand = new RelayCommand(() =>
        {
            IsRosterExpanded = !IsRosterExpanded;
        });

        ClearSearchCommand = new RelayCommand(() =>
        {
            SearchText = string.Empty;
        });

        SetSkillFilterCommand = new RelayCommand<object>(param =>
        {
            if (param is int i) SkillFilter = i;
            else if (param is string s && int.TryParse(s, out var parsed)) SkillFilter = parsed;
        });

        SelectGeneralCommand = new RelayCommand<GeneralStaticInfo>(general =>
        {
            if (general != null)
            {
                SelectedGeneral = general;
                IsReverseLookupOpen = false;
            }
        });

        LookupSkillCommand = new RelayCommand<string>(skillName =>
        {
            if (!string.IsNullOrWhiteSpace(skillName))
            {
                ReverseLookupSkillName = skillName;
                ReverseLookupMasters = _invertedIndex.GetMastersOfSkill(skillName);
                IsReverseLookupOpen = true;
            }
        });

        CloseReverseLookupCommand = new RelayCommand(() =>
        {
            IsReverseLookupOpen = false;
        });

        ApplyFilter();
        SelectedGeneral = _allGenerals.FirstOrDefault(g => g.Name == "关羽") ?? _allGenerals.FirstOrDefault();
    }

    private void UpdateDisplayedSkills()
    {
        DisplayedSkills.Clear();
        if (SelectedGeneral != null)
        {
            var items = _skillFilter switch
            {
                1 => SelectedGeneral.SkillTree.Where(s => s.Category == SkillCategory.General),
                2 => SelectedGeneral.SkillTree.Where(s => s.Category == SkillCategory.Military),
                _ => SelectedGeneral.SkillTree
            };
            foreach (var s in items)
            {
                DisplayedSkills.Add(s);
            }
        }

        OnPropertyChanged(nameof(AllSkillsCount));
        OnPropertyChanged(nameof(GeneralSkillsCount));
        OnPropertyChanged(nameof(MilitarySkillsCount));
    }

    private void SyncRosterSelection()
    {
        var targetId = _selectedGeneral?.Id;
        var foundInDisplayed = false;

        foreach (var item in DisplayedGenerals)
        {
            var isCurrent = item.General.Id == targetId;
            item.IsSelected = isCurrent;
            if (isCurrent) foundInDisplayed = true;
        }

        // 如果外部逆查或全局检索选中的武将不在当前名录列表中，插入到首位以便清晰聚焦高亮
        if (!foundInDisplayed && _selectedGeneral != null)
        {
            var avatarPath = _repository.ResolveAvatarByName(_selectedGeneral.Name) ?? _repository.ResolveAvatarPath(_selectedGeneral.Avatar);
            DisplayedGenerals.Insert(0, new RosterGeneralItem(_selectedGeneral, true, avatarPath));
            OnPropertyChanged(nameof(RosterSummaryText));
        }
    }

    private void ApplyFilter()
    {
        List<GeneralStaticInfo> list;

        if (IsSearching)
        {
            var matched = PinyinSearchEngine.Search(_allGenerals, _searchText).ToList();
            list = matched;
            SearchStatusText = list.Count > 0 
                ? $"◈ 检索结果 (共 {list.Count} 位匹配):" 
                : "未找到匹配武将，可尝试拼音缩写 (如 gy/zgl)";
        }
        else
        {
            SearchStatusText = string.Empty;
            list = _rosterCategory switch
            {
                "无双" => _allGenerals.Where(g => g.SealTier == "无双" || g.BaseForce >= 95).OrderByDescending(g => g.BaseForce).ToList(),
                "谋主" => _allGenerals.Where(g => g.SealTier == "谋主" || g.BaseIntellect >= 94).OrderByDescending(g => g.BaseIntellect).ToList(),
                "君主" => _allGenerals.Where(g => MonarchNames.Contains(g.Name)).OrderByDescending(g => g.BaseForce + g.BaseIntellect).ToList(),
                "良将" => _allGenerals.Where(g => (g.SealTier == "良将" || (g.BaseForce >= 85 && g.BaseForce < 95)) && !FeaturedNames.Contains(g.Name)).OrderByDescending(g => g.BaseForce).Take(36).ToList(),
                "全谱" => _allGenerals.OrderByDescending(g => g.BaseForce + g.BaseIntellect).ToList(),
                _ => _allGenerals.Where(g => FeaturedNames.Contains(g.Name)).OrderByDescending(g => g.BaseForce + g.BaseIntellect).ToList()
            };
        }

        FilteredGenerals.Clear();
        var rosterItems = new List<RosterGeneralItem>(list.Count);

        foreach (var g in list)
        {
            var avatarPath = _repository.ResolveAvatarByName(g.Name) ?? _repository.ResolveAvatarPath(g.Avatar);
            FilteredGenerals.Add(g);
            rosterItems.Add(new RosterGeneralItem(g, g.Id == _selectedGeneral?.Id, avatarPath));
        }

        DisplayedGenerals.ReplaceRange(rosterItems);

        OnPropertyChanged(nameof(RosterSummaryText));

        if (SelectedGeneral != null && !FilteredGenerals.Contains(SelectedGeneral) && FilteredGenerals.Count > 0)
        {
            SelectedGeneral = FilteredGenerals[0];
        }
        else if (SelectedGeneral == null && FilteredGenerals.Count > 0)
        {
            SelectedGeneral = FilteredGenerals[0];
        }
        else
        {
            SyncRosterSelection();
        }
    }
}
