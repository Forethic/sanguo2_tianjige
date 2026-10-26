using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Tianjige.Core.Models;
using Tianjige.Core.Services;
using Tianjige.Data.Repositories;

namespace Tianjige.UI.ViewModels;

/// <summary>
/// 兵种罗盘连线模型（带向弧线连接两节点，描述生克流向）
/// </summary>
public class TroopChordLine : ViewModelBase
{
    public required string FromTroop { get; init; }
    public required string ToTroop { get; init; }
    public required int AdvantageScore { get; init; }
    public required string PathData { get; init; } // SVG 贝塞尔曲线: "M x1,y1 Q cx,cy x2,y2"
    public double StartX { get; init; }
    public double StartY { get; init; }
    public double EndX { get; init; }
    public double EndY { get; init; }

    private double _opacity = 0.18;
    public double Opacity
    {
        get => _opacity;
        set => SetProperty(ref _opacity, value);
    }

    private string _stroke = "#33C8A355";
    public string Stroke
    {
        get => _stroke;
        set => SetProperty(ref _stroke, value);
    }

    private double _strokeThickness = 1.0;
    public double StrokeThickness
    {
        get => _strokeThickness;
        set => SetProperty(ref _strokeThickness, value);
    }

    private bool _isAdvantage;
    public bool IsAdvantage
    {
        get => _isAdvantage;
        set => SetProperty(ref _isAdvantage, value);
    }

    private bool _isDisadvantage;
    public bool IsDisadvantage
    {
        get => _isDisadvantage;
        set => SetProperty(ref _isDisadvantage, value);
    }
}

/// <summary>
/// 圆周兵种节点
/// </summary>
public class TroopNodeItem : ViewModelBase
{
    public required string TroopName { get; init; }
    public required string ShortName { get; init; }
    public required TroopCategory Category { get; init; }
    public required string CategoryName { get; init; }
    public required string CategoryShort { get; init; }
    public required string CategoryColor { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Left => X - 28.0;
    public double Top => Y - 28.0;
    public double AngleDeg { get; init; }

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
                OnPropertyChanged(nameof(BorderColor));
                OnPropertyChanged(nameof(RelationBadgeText));
            }
        }
    }

    public bool IsSelected => Relation == TroopRelation.Self;
    public bool IsStrong => Relation == TroopRelation.Strong;
    public bool IsWeak => Relation == TroopRelation.Weak;
    public bool IsEven => Relation == TroopRelation.Even;

    public string BorderColor => Relation switch
    {
        TroopRelation.Self => "#FFD700",
        TroopRelation.Strong => "#00E676", // 翠绿：我克对方
        TroopRelation.Weak => "#FF5252",   // 绯红：对方克我
        _ => "#554433"
    };

    public string RelationBadgeText => Relation switch
    {
        TroopRelation.Self => "主选",
        TroopRelation.Strong => "+胜",
        TroopRelation.Weak => "-败",
        _ => "平"
    };
}

public class TroopCounterItemBadge
{
    public required string TargetTroopName { get; init; }
    public required int AdvantageScore { get; init; }
    public required string DisplayText { get; init; }
    public required string BadgeBackground { get; init; }
    public required string BadgeBorder { get; init; }
    public required string BadgeForeground { get; init; }
}

public class TroopDetailCardModel : ViewModelBase
{
    public required string TroopName { get; init; }
    public required string CategoryName { get; init; }
    public required string CategoryColor { get; init; }
    public required string ChantQuote { get; init; }
    public required string Characteristics { get; init; }
    public List<TroopCounterItemBadge> StrongBadges { get; init; } = new();
    public List<TroopCounterItemBadge> WeakBadges { get; init; } = new();
    public List<TroopCounterItemBadge> EvenBadges { get; init; } = new();
    public string StrongSummary { get; init; } = string.Empty;
    public string WeakSummary { get; init; } = string.Empty;
}

public class FormationDetailCardModel : ViewModelBase
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string CenterGravity { get; init; }
    public required string Advantages { get; init; }
    public required string Disadvantages { get; init; }
    public required string Description { get; init; }
}

public class MatrixCellModel
{
    public required string Text { get; init; }
    public required string Background { get; init; }
    public required string Foreground { get; init; }
    public required string ToolTip { get; init; }
}

public class MatrixRowModel
{
    public required string RowHeader { get; init; }
    public List<MatrixCellModel> Cells { get; init; } = new();
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
    private readonly Dictionary<string, TroopCounterInfo> _troopsByName;

    public IReadOnlyList<FormationInfo> Formations { get; }
    public ObservableCollection<FormationItemViewModel> FormationItems { get; } = new();
    public IReadOnlyList<TroopCounterInfo> Troops { get; }
    public ObservableCollection<TroopNodeItem> TroopNodes { get; } = new();
    public ObservableCollection<TroopChordLine> ChordLines { get; } = new();

    public ObservableCollection<TroopAdvantageScore> AdvantageList { get; } = new();
    public ObservableCollection<TroopAdvantageScore> DisadvantageList { get; } = new();
    public ObservableCollection<TroopAdvantageScore> EvenList { get; } = new();

    // 静态直接罗列卡片与全局大矩阵（免点击查看全部生克）
    public ObservableCollection<TroopDetailCardModel> AllTroopCards { get; } = new();
    public ObservableCollection<FormationDetailCardModel> AllFormationCards { get; } = new();
    public ObservableCollection<MatrixRowModel> TroopMatrixRows { get; } = new();
    public List<string> TroopMatrixHeaders { get; } = new();
    public ObservableCollection<MatrixRowModel> FormationMatrixRows { get; } = new();
    public List<string> FormationMatrixHeaders { get; } = new();

    private int _troopViewMode = 0;
    public int TroopViewMode
    {
        get => _troopViewMode;
        set => SetProperty(ref _troopViewMode, value);
    }

    private int _formationViewMode = 0;
    public int FormationViewMode
    {
        get => _formationViewMode;
        set => SetProperty(ref _formationViewMode, value);
    }

    private int _selectedTabIndex = 0;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    private TroopCounterInfo? _selectedTroop;
    public TroopCounterInfo? SelectedTroop
    {
        get => _selectedTroop;
        set
        {
            if (SetProperty(ref _selectedTroop, value) && value != null)
            {
                CurrentAnalysis = _relationService.AnalyzeTroop(value.TroopName);
                UpdateSelectedTroopDetails(value);
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

    private bool _isTopmost = true;
    public bool IsTopmost
    {
        get => _isTopmost;
        set => SetProperty(ref _isTopmost, value);
    }

    private string _activeCategoryFilter = "全部";
    public string ActiveCategoryFilter
    {
        get => _activeCategoryFilter;
        set
        {
            if (SetProperty(ref _activeCategoryFilter, value))
            {
                ApplyCategoryFilter(value);
            }
        }
    }

    public ICommand SelectTroopCommand { get; }
    public ICommand SelectFormationCommand { get; }
    public ICommand ToggleTopmostCommand { get; }
    public ICommand FilterCategoryCommand { get; }
    public ICommand SwitchToTabCommand { get; }

    public TroopCompassViewModel() : this(null)
    {
    }

    public TroopCompassViewModel(StaticRepository? repository = null)
    {
        _repository = repository ?? new StaticRepository();
        Troops = _repository.GetTroopCounters();
        Formations = _repository.GetFormations();
        _relationService = new TroopRelationService(Troops);
        _troopsByName = Troops.ToDictionary(t => t.TroopName, StringComparer.OrdinalIgnoreCase);

        ToggleTopmostCommand = new RelayCommand(() => IsTopmost = !IsTopmost);

        SwitchToTabCommand = new RelayCommand<object>(param =>
        {
            if (param is int i) SelectedTabIndex = i;
            else if (param is string s && int.TryParse(s, out var v)) SelectedTabIndex = v;
        });

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

        FilterCategoryCommand = new RelayCommand<string>(cat =>
        {
            if (!string.IsNullOrEmpty(cat)) ActiveCategoryFilter = cat;
        });

        BuildCircularNodesAndChords();
        BuildStaticCardsAndMatrix();

        // 默认选中长枪兵与方形阵
        SelectedTroop = Troops.FirstOrDefault(t => t.TroopName == "长枪兵") ?? Troops.FirstOrDefault();
        SelectedFormation = Formations.FirstOrDefault(f => f.Name == "方形阵") ?? Formations.FirstOrDefault();
    }

    private void BuildCircularNodesAndChords()
    {
        TroopNodes.Clear();
        ChordLines.Clear();

        // 圆心坐标与半径 (Canvas 宽 640, 高 640, 中心 320, 320, 半径 230)
        double cx = 320;
        double cy = 320;
        double radius = 228;
        int count = Troops.Count;
        if (count == 0) return;

        var nodePositions = new Dictionary<string, (double X, double Y)>();

        for (int i = 0; i < count; i++)
        {
            var counter = Troops[i];
            double angleDeg = -90.0 + (i * 360.0 / count);
            double angleRad = angleDeg * Math.PI / 180.0;
            double nodeX = cx + radius * Math.Cos(angleRad);
            double nodeY = cy + radius * Math.Sin(angleRad);

            nodePositions[counter.TroopName] = (nodeX, nodeY);

            TroopNodes.Add(new TroopNodeItem
            {
                TroopName = counter.TroopName,
                ShortName = counter.TroopName.Replace("兵", "").Replace("贼", ""),
                Category = counter.Category,
                CategoryName = counter.CategoryName,
                CategoryShort = GetCategoryShort(counter.Category),
                CategoryColor = GetCategoryColor(counter.Category),
                X = Math.Round(nodeX, 1),
                Y = Math.Round(nodeY, 1),
                AngleDeg = angleDeg
            });
        }

        // 构建克制贝塞尔曲线 (每条有向连接由克制方指向受制方，弧线向中心弓起)
        var addedPairs = new HashSet<string>();
        foreach (var source in Troops)
        {
            if (source.AdvantageMatrix == null) continue;

            foreach (var (targetName, score) in source.AdvantageMatrix)
            {
                if (score <= 0) continue; // 仅为克制关系连线 (大于0代表 source 克制 target)
                if (!nodePositions.TryGetValue(source.TroopName, out var p1) ||
                    !nodePositions.TryGetValue(targetName, out var p2)) continue;

                var pairKey = $"{source.TroopName}->{targetName}";
                if (!addedPairs.Add(pairKey)) continue;

                // 计算弧度控制点 (向圆心内凹)
                double midX = (p1.X + p2.X) / 2.0;
                double midY = (p1.Y + p2.Y) / 2.0;
                double ctrlX = midX * 0.35 + cx * 0.65;
                double ctrlY = midY * 0.35 + cy * 0.65;

                string path = string.Format(CultureInfo.InvariantCulture,
                    "M {0:F1},{1:F1} Q {2:F1},{3:F1} {4:F1},{5:F1}",
                    p1.X, p1.Y, ctrlX, ctrlY, p2.X, p2.Y);

                ChordLines.Add(new TroopChordLine
                {
                    FromTroop = source.TroopName,
                    ToTroop = targetName,
                    AdvantageScore = score,
                    PathData = path,
                    StartX = p1.X,
                    StartY = p1.Y,
                    EndX = p2.X,
                    EndY = p2.Y,
                    Stroke = "#33C8A355",
                    StrokeThickness = 1.0,
                    Opacity = 0.18
                });
            }
        }
    }

    private void UpdateSelectedTroopDetails(TroopCounterInfo troop)
    {
        // 1. 更新圆周节点状态
        foreach (var node in TroopNodes)
        {
            node.Relation = _relationService.GetRelation(troop.TroopName, node.TroopName);
        }

        // 2. 更新连线高亮（绿色为我克对方，红色为对方克我，其余虚化）
        foreach (var line in ChordLines)
        {
            if (line.FromTroop == troop.TroopName)
            {
                // 我方克制对方 (出线) -> 翡翠绿高亮
                line.IsAdvantage = true;
                line.IsDisadvantage = false;
                line.Stroke = "#00E676";
                line.StrokeThickness = 2.4;
                line.Opacity = 0.95;
            }
            else if (line.ToTroop == troop.TroopName)
            {
                // 对方克制我方 (入线) -> 珊瑚红高亮
                line.IsAdvantage = false;
                line.IsDisadvantage = true;
                line.Stroke = "#FF5252";
                line.StrokeThickness = 2.4;
                line.Opacity = 0.95;
            }
            else
            {
                // 无关连线 -> 极低透明度
                line.IsAdvantage = false;
                line.IsDisadvantage = false;
                line.Stroke = "#2A2218";
                line.StrokeThickness = 0.8;
                line.Opacity = 0.04;
            }
        }

        // 3. 更新右侧详细数据看板 (包含胜率、分类与平局)
        AdvantageList.Clear();
        DisadvantageList.Clear();
        EvenList.Clear();

        if (troop.AdvantageMatrix != null && troop.AdvantageMatrix.Count > 0)
        {
            var sortedAdv = troop.AdvantageMatrix
                .Where(kv => kv.Value > 0)
                .OrderByDescending(kv => kv.Value)
                .Select(kv => new TroopAdvantageScore
                {
                    Opponent = kv.Key,
                    AdvantageScore = kv.Value,
                    CategoryName = _troopsByName.GetValueOrDefault(kv.Key)?.CategoryName ?? "常规兵"
                });

            foreach (var item in sortedAdv) AdvantageList.Add(item);

            var sortedDis = troop.AdvantageMatrix
                .Where(kv => kv.Value < 0)
                .OrderBy(kv => kv.Value)
                .Select(kv => new TroopAdvantageScore
                {
                    Opponent = kv.Key,
                    AdvantageScore = kv.Value,
                    CategoryName = _troopsByName.GetValueOrDefault(kv.Key)?.CategoryName ?? "常规兵"
                });

            foreach (var item in sortedDis) DisadvantageList.Add(item);

            var sortedEven = troop.AdvantageMatrix
                .Where(kv => kv.Value == 0 && kv.Key != troop.TroopName)
                .Select(kv => new TroopAdvantageScore
                {
                    Opponent = kv.Key,
                    AdvantageScore = 0,
                    CategoryName = _troopsByName.GetValueOrDefault(kv.Key)?.CategoryName ?? "常规兵"
                });

            foreach (var item in sortedEven) EvenList.Add(item);
        }
        else
        {
            foreach (var s in troop.StrongAgainst)
            {
                AdvantageList.Add(new TroopAdvantageScore
                {
                    Opponent = s,
                    AdvantageScore = 35,
                    CategoryName = _troopsByName.GetValueOrDefault(s)?.CategoryName ?? "常规兵"
                });
            }
            foreach (var w in troop.WeakAgainst)
            {
                DisadvantageList.Add(new TroopAdvantageScore
                {
                    Opponent = w,
                    AdvantageScore = -35,
                    CategoryName = _troopsByName.GetValueOrDefault(w)?.CategoryName ?? "常规兵"
                });
            }
        }
    }

    private void ApplyCategoryFilter(string filter)
    {
        if (filter == "全部")
        {
            return;
        }

        var match = Troops.FirstOrDefault(t => t.CategoryName == filter);
        if (match != null)
        {
            SelectedTroop = match;
        }
    }

    private static string GetCategoryShort(TroopCategory category) => category switch
    {
        TroopCategory.Long => "长",
        TroopCategory.Short => "短",
        TroopCategory.Ranged => "远",
        _ => "特"
    };

    private static string GetCategoryColor(TroopCategory category) => category switch
    {
        TroopCategory.Long => "#4A90E2",   // 蔚蓝
        TroopCategory.Short => "#E06A3B",  // 赭红
        TroopCategory.Ranged => "#48C774", // 翡翠绿
        _ => "#B07D48"                     // 古黄
    };

    private void BuildStaticCardsAndMatrix()
    {
        AllTroopCards.Clear();
        TroopMatrixHeaders.Clear();
        TroopMatrixRows.Clear();

        // 1. 构建全兵种免点击卡片数据
        foreach (var t in Troops)
        {
            var strongBadges = new List<TroopCounterItemBadge>();
            var weakBadges = new List<TroopCounterItemBadge>();
            var evenBadges = new List<TroopCounterItemBadge>();

            if (t.AdvantageMatrix != null && t.AdvantageMatrix.Count > 0)
            {
                foreach (var kv in t.AdvantageMatrix.Where(x => x.Value > 0).OrderByDescending(x => x.Value))
                {
                    strongBadges.Add(new TroopCounterItemBadge
                    {
                        TargetTroopName = kv.Key,
                        AdvantageScore = kv.Value,
                        DisplayText = $"+{kv.Value}% {kv.Key}",
                        BadgeBackground = "#1E3B27",
                        BadgeBorder = "#00E676",
                        BadgeForeground = "#69F0AE"
                    });
                }

                foreach (var kv in t.AdvantageMatrix.Where(x => x.Value < 0).OrderBy(x => x.Value))
                {
                    weakBadges.Add(new TroopCounterItemBadge
                    {
                        TargetTroopName = kv.Key,
                        AdvantageScore = kv.Value,
                        DisplayText = $"{kv.Value}% {kv.Key}",
                        BadgeBackground = "#3B1E1E",
                        BadgeBorder = "#FF5252",
                        BadgeForeground = "#FF8A80"
                    });
                }

                foreach (var kv in t.AdvantageMatrix.Where(x => x.Value == 0 && x.Key != t.TroopName))
                {
                    evenBadges.Add(new TroopCounterItemBadge
                    {
                        TargetTroopName = kv.Key,
                        AdvantageScore = 0,
                        DisplayText = $"平局 {kv.Key}",
                        BadgeBackground = "#2A2218",
                        BadgeBorder = "#554433",
                        BadgeForeground = "#C8A355"
                    });
                }
            }
            else
            {
                foreach (var s in t.StrongAgainst)
                {
                    strongBadges.Add(new TroopCounterItemBadge
                    {
                        TargetTroopName = s,
                        AdvantageScore = 35,
                        DisplayText = $"+胜 {s}",
                        BadgeBackground = "#1E3B27",
                        BadgeBorder = "#00E676",
                        BadgeForeground = "#69F0AE"
                    });
                }
                foreach (var w in t.WeakAgainst)
                {
                    weakBadges.Add(new TroopCounterItemBadge
                    {
                        TargetTroopName = w,
                        AdvantageScore = -35,
                        DisplayText = $"-败 {w}",
                        BadgeBackground = "#3B1E1E",
                        BadgeBorder = "#FF5252",
                        BadgeForeground = "#FF8A80"
                    });
                }
            }

            string strongSummary = strongBadges.Count > 0
                ? $"优势克制 ({strongBadges.Count}类): " + string.Join("、", strongBadges.Select(b => $"{b.TargetTroopName}(+{b.AdvantageScore}%)"))
                : "无显著克制优势";

            string weakSummary = weakBadges.Count > 0
                ? $"劣势受制 ({weakBadges.Count}类): " + string.Join("、", weakBadges.Select(b => $"{b.TargetTroopName}({b.AdvantageScore}%)"))
                : "无显著受制劣势";

            AllTroopCards.Add(new TroopDetailCardModel
            {
                TroopName = t.TroopName,
                CategoryName = t.CategoryName,
                CategoryColor = GetCategoryColor(t.Category),
                ChantQuote = GetTroopChant(t.TroopName),
                Characteristics = GetTroopCharacteristics(t.TroopName),
                StrongBadges = strongBadges,
                WeakBadges = weakBadges,
                EvenBadges = evenBadges,
                StrongSummary = strongSummary,
                WeakSummary = weakSummary
            });
        }

        // 2. 构建 13x13 兵种全局矩阵
        foreach (var t in Troops)
        {
            TroopMatrixHeaders.Add(t.TroopName.Replace("兵", "").Replace("贼", ""));
        }

        foreach (var rowTroop in Troops)
        {
            var cells = new List<MatrixCellModel>();
            foreach (var colTroop in Troops)
            {
                if (rowTroop.TroopName == colTroop.TroopName)
                {
                    cells.Add(new MatrixCellModel
                    {
                        Text = "——",
                        Background = "#181410",
                        Foreground = "#4A4036",
                        ToolTip = $"{rowTroop.TroopName} 对自身"
                    });
                    continue;
                }

                int score = 0;
                if (rowTroop.AdvantageMatrix != null && rowTroop.AdvantageMatrix.TryGetValue(colTroop.TroopName, out var s))
                {
                    score = s;
                }
                else if (rowTroop.StrongAgainst.Contains(colTroop.TroopName))
                {
                    score = 35;
                }
                else if (rowTroop.WeakAgainst.Contains(colTroop.TroopName))
                {
                    score = -35;
                }

                string text;
                string bg;
                string fg;
                if (score > 15)
                {
                    text = $"+{score}";
                    bg = "#1B3B24"; // 胜 绿
                    fg = "#69F0AE";
                }
                else if (score > 0)
                {
                    text = $"+{score}";
                    bg = "#152C1C";
                    fg = "#A7F3D0";
                }
                else if (score < -15)
                {
                    text = $"{score}";
                    bg = "#3E1818"; // 败 红
                    fg = "#FF8A80";
                }
                else if (score < 0)
                {
                    text = $"{score}";
                    bg = "#2D1414";
                    fg = "#FCA5A5";
                }
                else
                {
                    text = "平";
                    bg = "#1E1A16";
                    fg = "#78716C";
                }

                string tip = score > 0
                    ? $"我方【{rowTroop.TroopName}】克制 敌方【{colTroop.TroopName}】(+{score}%)"
                    : (score < 0 ? $"我方【{rowTroop.TroopName}】受制于 敌方【{colTroop.TroopName}】({score}%)" : $"我方【{rowTroop.TroopName}】与 敌方【{colTroop.TroopName}】势均力敌");

                cells.Add(new MatrixCellModel
                {
                    Text = text,
                    Background = bg,
                    Foreground = fg,
                    ToolTip = tip
                });
            }

            TroopMatrixRows.Add(new MatrixRowModel
            {
                RowHeader = rowTroop.TroopName,
                Cells = cells
            });
        }

        // 3. 构建九大阵法免点击卡片数据与 9x9 官方底层生克矩阵
        AllFormationCards.Clear();
        FormationMatrixHeaders.Clear();
        FormationMatrixRows.Clear();

        foreach (var f in Formations)
        {
            AllFormationCards.Add(new FormationDetailCardModel
            {
                Id = f.Id,
                Name = f.Name,
                CenterGravity = f.CenterGravity,
                Advantages = f.Advantages,
                Disadvantages = f.Disadvantages,
                Description = f.Description
            });
            FormationMatrixHeaders.Add(f.Name.Replace("阵", ""));
        }

        // 《三国群英传2》官方原版 9x9 底层阵法克制数值矩阵 (正数代表我方克制敌方，负数代表我方受制敌方)
        // 宏观规律：平头阵(方/圆/鱼丽) 克 凹头阵(雁形/玄襄/钩形) ； 凹头阵 克 尖头阵(锥形/冲锋/箭矢) ； 尖头阵 克 平头阵
        var formationOfficialMatrix = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase)
        {
            ["方形阵"] = new() { ["圆形阵"] = 7, ["锥形阵"] = -18, ["雁形阵"] = 19, ["玄襄阵"] = 13, ["鱼丽阵"] = -6, ["钩形阵"] = 23, ["冲锋阵"] = -23, ["箭矢阵"] = -15 },
            ["圆形阵"] = new() { ["方形阵"] = -7, ["锥形阵"] = -12, ["雁形阵"] = 22, ["玄襄阵"] = 21, ["鱼丽阵"] = 6, ["钩形阵"] = 12, ["冲锋阵"] = -24, ["箭矢阵"] = -18 },
            ["锥形阵"] = new() { ["方形阵"] = 18, ["圆形阵"] = 12, ["雁形阵"] = -19, ["玄襄阵"] = -11, ["鱼丽阵"] = 25, ["钩形阵"] = -25, ["冲锋阵"] = -7, ["箭矢阵"] = 6 },
            ["雁形阵"] = new() { ["方形阵"] = -19, ["圆形阵"] = -22, ["锥形阵"] = 19, ["玄襄阵"] = -5, ["鱼丽阵"] = -11, ["钩形阵"] = 3, ["冲锋阵"] = 15, ["箭矢阵"] = 20 },
            ["玄襄阵"] = new() { ["方形阵"] = -13, ["圆形阵"] = -21, ["锥形阵"] = 11, ["雁形阵"] = 5, ["鱼丽阵"] = -18, ["钩形阵"] = -6, ["冲锋阵"] = 24, ["箭矢阵"] = 19 },
            ["鱼丽阵"] = new() { ["方形阵"] = 6, ["圆形阵"] = -6, ["锥形阵"] = -25, ["雁形阵"] = 11, ["玄襄阵"] = 18, ["钩形阵"] = 23, ["冲锋阵"] = -10, ["箭矢阵"] = -17 },
            ["钩形阵"] = new() { ["方形阵"] = -23, ["圆形阵"] = -12, ["锥形阵"] = 25, ["雁形阵"] = -3, ["玄襄阵"] = 6, ["鱼丽阵"] = -23, ["冲锋阵"] = 14, ["箭矢阵"] = 15 },
            ["冲锋阵"] = new() { ["方形阵"] = 23, ["圆形阵"] = 24, ["锥形阵"] = 7, ["雁形阵"] = -15, ["玄襄阵"] = -24, ["鱼丽阵"] = 10, ["钩形阵"] = -14, ["箭矢阵"] = -12 },
            ["箭矢阵"] = new() { ["方形阵"] = 15, ["圆形阵"] = 18, ["锥形阵"] = -6, ["雁形阵"] = -20, ["玄襄阵"] = -19, ["鱼丽阵"] = 17, ["钩形阵"] = -15, ["冲锋阵"] = 12 }
        };

        foreach (var rowF in Formations)
        {
            var cells = new List<MatrixCellModel>();
            formationOfficialMatrix.TryGetValue(rowF.Name, out var targetRow);

            foreach (var colF in Formations)
            {
                if (rowF.Name == colF.Name)
                {
                    cells.Add(new MatrixCellModel
                    {
                        Text = "——",
                        Background = "#181410",
                        Foreground = "#4A4036",
                        ToolTip = $"{rowF.Name} 对自身"
                    });
                    continue;
                }

                int score = 0;
                if (targetRow != null && targetRow.TryGetValue(colF.Name, out var s))
                {
                    score = s;
                }

                string text;
                string bg;
                string fg;
                string tip;
                if (score > 0)
                {
                    text = $"+{score}";
                    bg = "#1B3B24";
                    fg = "#69F0AE";
                    tip = $"我方【{rowF.Name}】克制 敌方【{colF.Name}】(+{score}%)";
                }
                else if (score < 0)
                {
                    text = $"{score}";
                    bg = "#3E1818";
                    fg = "#FF8A80";
                    tip = $"我方【{rowF.Name}】受制于 敌方【{colF.Name}】({score}%)";
                }
                else
                {
                    text = "平";
                    bg = "#1E1A16";
                    fg = "#78716C";
                    tip = $"我方【{rowF.Name}】与 敌方【{colF.Name}】伯仲之间";
                }

                cells.Add(new MatrixCellModel
                {
                    Text = text,
                    Background = bg,
                    Foreground = fg,
                    ToolTip = tip
                });
            }

            FormationMatrixRows.Add(new MatrixRowModel
            {
                RowHeader = rowF.Name,
                Cells = cells
            });
        }
    }

    private static string GetTroopChant(string troopName) => troopName switch
    {
        "长枪兵" => "长枪刺步勇无双，远避飞矢莫当弓。",
        "朴刀兵" => "朴刀坚盾逼弓弩，最惧长枪大刀劈。",
        "大刀兵" => "大刀对女是短板，重劈藤甲与长枪。",
        "弓箭兵" => "远克长枪近畏刀，百步穿杨箭如雨。",
        "弩兵" => "弩兵全能很能干，碰到飞弓就完蛋。",
        "飞刀兵" => "飞刀爆的链锤惨，远避藤甲近身斩。",
        "武斗兵" => "武斗不惧锤和缒，近破飞弓震敌胆。",
        "蛮族兵" => "蛮勇扑杀破飞弓，正规枪阵最难攻。",
        "藤甲兵" => "藤甲坚韧箭难透，生克飞弓莫对枪。",
        "铁锤兵" => "铁锤砸碎重盾甲，长刃回旋防破防。",
        "链锤兵" => "铁索挥舞破长兵，最怕飞刀爆得惨。",
        "黄巾兵" => "黄巾只克女飞蛮，正规军阵势如破竹。",
        "女兵" => "女兵轻捷克大刀，飞刀弓矢亦难当。",
        _ => "兵无常势，水无常形；因敌制胜，存乎一心。"
    };

    private static string GetTroopCharacteristics(string troopName) => troopName switch
    {
        "长枪兵" => "攻击距离较远，列阵正面刺杀威力极大，对短兵器（朴刀+42%、藤甲+66%、蛮族+49%、武斗+60%）大优，但受弓弩远程箭雨克制(-58%)。",
        "朴刀兵" => "配有坚木圆盾，格挡率高，近身强破远程（弓箭+44%、飞刀+47%），但面对长枪(-42%)与大刀(-58%)处于显著劣势。",
        "大刀兵" => "攻击范围宽，劈砍威压极强，完克藤甲(+68%)、黄巾(+68%)与短兵，但对女兵(-43%)为天然短板，且惧怕弓箭齐射(-66%)。",
        "弓箭兵" => "超远距离抛射，长兵器克星（大刀+66%、长枪+58%、链锤+54%），但一旦被朴刀(-44%)、藤甲(-54%)或武斗(-56%)近身则伤亡惨重。",
        "弩兵" => "泛用性极强的穿透远程，对长枪(+52%)、链锤(+45%)等绝大多数地面部队皆占优，但与弓箭(-32%)、飞刀(-56%)对射处于下风。",
        "飞刀兵" => "中距特种投掷兵，强破大刀(+64%)、弩兵(+56%)、长枪(+53%)与链锤(+48%)，但被高防贴身短兵（尤其藤甲兵-67%与武斗兵-55%）严重克制。",
        "武斗兵" => "极高敏捷格斗兵，接敌极快，不惧锤缒（铁缒+42%、链锤+26%），破飞刀(+55%)与弓箭(+56%)，但正面拼刺被大刀(-63%)与长枪(-60%)克制。",
        "蛮族兵" => "体质强健血量厚重，肉搏撕扯凶悍，对飞刀(+39%)与弓箭(+36%)表现优异，但对抗黄巾(-62%)与正规长枪(-49%)处于劣势。",
        "藤甲兵" => "刀盾高防特种步兵，防箭防刃极坚，完克飞刀(+67%)与弓箭(+54%)，但面对长枪(-66%)、大刀(-68%)与链锤(-65%)等长兵器严重受创。",
        "铁锤兵" => "重型破甲兵，沉重钝击克制藤甲(+64%)与朴刀(+51%)，但攻速较慢，被弓箭(-47%)、武斗(-42%)与飞刀(-39%)克制。",
        "链锤兵" => "范围回旋攻击，破坏长兵步调，克制藤甲(+65%)与朴刀(+47%)，但被飞刀(-48%)与弓箭(-54%)克制。",
        "黄巾兵" => "属性平庸的乱军，唯一优势在于克制蛮族(+62%)、女兵(+56%)与飞刀(+24%)（黄巾只克女飞蛮），面对其他9大正规兵种全线处于劣势。",
        "女兵" => "身形轻捷闪避极高，针对大刀兵有天然克制短板(+43%)，对飞刀(+32%)与弓箭(+29%)占优，但血薄防低受多数重兵器克制。",
        _ => "三国常规作战兵种，需善用相克之道取胜。"
    };
}

