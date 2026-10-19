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
        set => SetProperty(ref _selectedGeneral, value);
    }

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

    public GeneralStarViewModel() : this(null)
    {
    }

    public GeneralStarViewModel(StaticRepository? repository = null)
    {
        _repository = repository ?? new StaticRepository();
        _allGenerals = _repository.GetAllGenerals().ToList();
        _invertedIndex = new SkillInvertedIndex(_allGenerals);

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
