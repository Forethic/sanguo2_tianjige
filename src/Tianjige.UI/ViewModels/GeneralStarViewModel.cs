using System.Collections.ObjectModel;
using System.Windows.Input;
using Tianjige.Core.Models;
using Tianjige.Core.Services;
using Tianjige.Data.Repositories;

namespace Tianjige.UI.ViewModels;

public sealed class GeneralStarViewModel : ViewModelBase
{
    private readonly StaticRepository _repository;
    private readonly SkillInvertedIndex _invertedIndex;
    private readonly List<GeneralStaticInfo> _allGenerals;

    private string _searchText = string.Empty;
    private GeneralStaticInfo? _selectedGeneral;
    private string _reverseLookupSkillName = string.Empty;
    private bool _isReverseLookupOpen;
    private IReadOnlyList<SkillMasteryInfo> _reverseLookupMasters = Array.Empty<SkillMasteryInfo>();

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
                ApplyFilter();
            }
        }
    }

    public GeneralStaticInfo? SelectedGeneral
    {
        get => _selectedGeneral;
        set
        {
            if (SetProperty(ref _selectedGeneral, value))
            {
                UpdateDisplayedSkills();
                OnPropertyChanged(nameof(SelectedGeneralAvatarPath));
                OnPropertyChanged(nameof(SelectedGeneralInitial));
                OnPropertyChanged(nameof(AllSkillsCount));
                OnPropertyChanged(nameof(GeneralSkillsCount));
                OnPropertyChanged(nameof(MilitarySkillsCount));
            }
        }
    }

    public string? SelectedGeneralAvatarPath =>
        _selectedGeneral != null ? _repository.ResolveAvatarPath(_selectedGeneral.Avatar) : null;

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
    public ICommand LookupSkillCommand { get; }
    public ICommand CloseReverseLookupCommand { get; }
    public ICommand SetSkillFilterCommand { get; }

    public GeneralStarViewModel() : this(null)
    {
    }

    public GeneralStarViewModel(StaticRepository? repository = null)
    {
        _repository = repository ?? new StaticRepository();
        _allGenerals = _repository.GetAllGenerals().ToList();
        _invertedIndex = new SkillInvertedIndex(_allGenerals);

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

    private void ApplyFilter()
    {
        var matched = PinyinSearchEngine.Search(_allGenerals, _searchText);
        FilteredGenerals.Clear();
        foreach (var g in matched)
        {
            FilteredGenerals.Add(g);
        }

        if (SelectedGeneral != null && !FilteredGenerals.Contains(SelectedGeneral) && FilteredGenerals.Count > 0)
        {
            SelectedGeneral = FilteredGenerals[0];
        }
    }
}
