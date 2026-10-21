using System.Collections.ObjectModel;
using System.Windows.Input;
using Tianjige.Core.Models;
using Tianjige.Core.Services;
using Tianjige.Data.Repositories;

namespace Tianjige.UI.ViewModels;

public class TroopNodeItem : ViewModelBase
{
    public required string TroopName { get; init; }
    public required TroopCategory Category { get; init; }
    public required string CategoryName { get; init; }
    public double X { get; init; }
    public double Y { get; init; }

    private TroopRelation _relation = TroopRelation.Even;
    public TroopRelation Relation
    {
        get => _relation;
        set
        {
            if (SetProperty(ref _relation, value))
            {
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(IsStrong));
                OnPropertyChanged(nameof(IsWeak));
                OnPropertyChanged(nameof(IsEven));
                OnPropertyChanged(nameof(RelationBadgeText));
            }
        }
    }

    public bool IsSelected => Relation == TroopRelation.Self;
    public bool IsStrong => Relation == TroopRelation.Strong;
    public bool IsWeak => Relation == TroopRelation.Weak;
    public bool IsEven => Relation == TroopRelation.Even;

    public string RelationBadgeText => Relation switch
    {
        TroopRelation.Self => "主选",
        TroopRelation.Strong => "我克",
        TroopRelation.Weak => "受制",
        _ => "平"
    };
}

public class FormationItemViewModel : ViewModelBase
{
    public FormationInfo Formation { get; }
    public string Name => Formation.Name;
    public string CenterGravity => Formation.CenterGravity;
    public string Description => Formation.Description;
    public string Advantages => Formation.Advantages;
    public string Disadvantages => Formation.Disadvantages;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public FormationItemViewModel(FormationInfo formation, bool isSelected = false)
    {
        Formation = formation;
        _isSelected = isSelected;
    }
}

public class TroopCompassViewModel : ViewModelBase
{
    private readonly StaticRepository _repository;
    private readonly TroopRelationService _relationService;

    public IReadOnlyList<FormationInfo> Formations { get; }
    public ObservableCollection<FormationItemViewModel> FormationItems { get; } = new();
    public IReadOnlyList<TroopCounterInfo> Troops { get; }
    public ObservableCollection<TroopNodeItem> TroopNodes { get; } = new();

    private TroopCounterInfo? _selectedTroop;
    public TroopCounterInfo? SelectedTroop
    {
        get => _selectedTroop;
        set
        {
            if (SetProperty(ref _selectedTroop, value) && value != null)
            {
                CurrentAnalysis = _relationService.AnalyzeTroop(value.TroopName);
                UpdateTroopNodeRelations(value.TroopName);
            }
        }
    }

    private TroopAnalysis? _currentAnalysis;
    public TroopAnalysis? CurrentAnalysis
    {
        get => _currentAnalysis;
        private set => SetProperty(ref _currentAnalysis, value);
    }

    private FormationInfo? _selectedFormation;
    public FormationInfo? SelectedFormation
    {
        get => _selectedFormation;
        set
        {
            if (SetProperty(ref _selectedFormation, value))
            {
                foreach (var item in FormationItems)
                {
                    item.IsSelected = (item.Name == value?.Name);
                }
            }
        }
    }

    public ICommand SelectTroopCommand { get; }
    public ICommand SelectFormationCommand { get; }

    public TroopCompassViewModel() : this(null)
    {
    }

    public TroopCompassViewModel(StaticRepository? repository = null)
    {
        _repository = repository ?? new StaticRepository();
        Troops = _repository.GetTroopCounters();
        Formations = _repository.GetFormations();
        _relationService = new TroopRelationService(Troops);

        foreach (var f in Formations)
        {
            FormationItems.Add(new FormationItemViewModel(f));
        }

        SelectTroopCommand = new RelayCommand<string>(name =>
        {
            if (!string.IsNullOrEmpty(name))
            {
                var troop = Troops.FirstOrDefault(t => t.TroopName == name);
                if (troop != null) SelectedTroop = troop;
            }
        });

        SelectFormationCommand = new RelayCommand<object>(param =>
        {
            if (param is FormationItemViewModel item) SelectedFormation = item.Formation;
            else if (param is FormationInfo info) SelectedFormation = info;
            else if (param is string name) SelectedFormation = Formations.FirstOrDefault(f => f.Name == name);
        });

        InitializeNodes();

        // 默认选中长枪兵与方形阵
        SelectedTroop = Troops.FirstOrDefault(t => t.TroopName == "长枪兵") ?? Troops.FirstOrDefault();
        SelectedFormation = Formations.FirstOrDefault(f => f.Name == "方形阵") ?? Formations.FirstOrDefault();
    }

    private void InitializeNodes()
    {
        // 8 兵种八卦环形坐标 (中心 140, 140，半径 100，按钮 68x32)
        // 角度偏移从正上方 (-90 deg) 顺时针递增 45 deg
        var nodeSpecs = new (string Name, double Left, double Top)[]
        {
            ("朴刀兵", 106, 24),   // 0: N
            ("长枪兵", 177, 53),   // 1: NE
            ("弓箭兵", 206, 124),  // 2: E
            ("铁骑兵", 177, 195),  // 3: SE
            ("藤甲兵", 106, 224),  // 4: S
            ("飞刀兵", 35, 195),   // 5: SW
            ("武斗兵", 6, 124),    // 6: W
            ("蛮族兵", 35, 53)     // 7: NW
        };

        foreach (var (name, left, top) in nodeSpecs)
        {
            var counter = Troops.FirstOrDefault(t => t.TroopName == name);
            if (counter != null)
            {
                TroopNodes.Add(new TroopNodeItem
                {
                    TroopName = counter.TroopName,
                    Category = counter.Category,
                    CategoryName = GetCategoryName(counter.Category),
                    X = left,
                    Y = top
                });
            }
        }
    }

    private void UpdateTroopNodeRelations(string selectedTroopName)
    {
        foreach (var node in TroopNodes)
        {
            node.Relation = _relationService.GetRelation(selectedTroopName, node.TroopName);
        }
    }

    private static string GetCategoryName(TroopCategory category) => category switch
    {
        TroopCategory.Short => "短兵",
        TroopCategory.Long => "长兵",
        TroopCategory.Ranged => "远兵",
        TroopCategory.Cavalry => "骑兵",
        TroopCategory.Special => "特殊",
        _ => "部曲"
    };
}
