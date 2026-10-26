using System.Collections.ObjectModel;
using System.Windows.Input;
using Tianjige.Core.Models;
using Tianjige.Core.Services;
using Tianjige.Data.Memory.Models;

namespace Tianjige.UI.ViewModels;

public sealed class SkillDisplayItem
{
    public required string SkillName { get; init; }
    public required byte Level { get; init; }
    public required bool IsLearned { get; init; }
    public required SkillCategory Category { get; init; }
    public required byte MpCost { get; init; }
    public required string Description { get; init; }
    public required string DamageDetail { get; init; }

    public string LevelDisplay => $"Lv.{Level}";
    public string CostDisplay => MpCost > 0 ? $"MP:{MpCost}" : "军师策谋";
    public string TooltipText => $"{SkillName} ({CostDisplay})\n【效能】{DamageDetail}\n【说明】{Description}";
}

public class GeneralCardViewModel : ViewModelBase
{
    private readonly LiveGeneralRecord? _live;
    private readonly GeneralStaticInfo _static;

    public GeneralStaticInfo StaticInfo => _static;
    public LiveGeneralRecord? LiveInfo => _live;

    public int Id => _static.Id;
    public int GeneralId => _static.Id;
    public string Name => _static.Name;
    public string AvatarPath => _static.Avatar;
    public string FirstChar => !string.IsNullOrEmpty(Name) ? Name.Substring(0, 1) : "将";
    public bool HasAvatar => !string.IsNullOrEmpty(AvatarPath) && System.IO.File.Exists(AvatarPath);
    public bool HasNoAvatar => !HasAvatar;
    public string PinyinIndex => _static.PinyinIndex;
    public string SealTier => _static.SealTier;

    public string LocationName { get => _live != null ? _live.CityName : "游历天下"; set {} }
    public string LordName { get => _live != null ? _live.LordName : "在野"; set {} }
    public string FactionColor { get => _live != null ? Tianjige.Data.Save.SaveGameReader.GetMonarchColor(_live.LordName) : "#8D6E63"; set {} }

    public byte Force { get => _live != null ? _live.Force : _static.BaseForce; set {} }
    public byte Intellect { get => _live != null ? _live.Intellect : _static.BaseIntellect; set {} }
    public int CurrentHp { get => _live != null ? _live.CurrentHp : 100; set {} }
    public int MaxHp { get => _live != null ? _live.MaxHp : 100; set {} }
    public int CurrentMp { get => _live != null ? _live.CurrentMp : 50; set {} }
    public int MaxMp { get => _live != null ? _live.MaxMp : 50; set {} }
    public int TroopCount { get => _live != null ? _live.TroopCount : 0; set {} }
    public int MaxTroopCount { get => _live != null ? _live.MaxTroopCount : 50; set {} }
    public byte Loyalty { get => _live != null ? _live.Loyalty : (byte)75; set {} }
    public byte Level { get => _live != null ? _live.Level : (byte)1; set {} }
    public string StatusDisplayName { get => _live?.StatusDisplayName ?? "未出仕"; set {} }

    public GeneralPotentialInfo Potential { get; }

    public ObservableCollection<SkillDisplayItem> GeneralSkills { get; } = new();
    public ObservableCollection<SkillDisplayItem> AdvisorSkills { get; } = new();

    private bool _isTopmost = true;
    public bool IsTopmost
    {
        get => _isTopmost;
        set => SetProperty(ref _isTopmost, value);
    }

    public ICommand ToggleTopmostCommand { get; }

    public GeneralCardViewModel(GeneralStaticInfo staticInfo, LiveGeneralRecord? liveInfo = null)
    {
        _static = staticInfo ?? throw new ArgumentNullException(nameof(staticInfo));
        _live = liveInfo;

        Potential = _static.Potential ?? GeneralPotentialService.Evaluate(_static);

        ToggleTopmostCommand = new RelayCommand(() => IsTopmost = !IsTopmost);

        int currentLvl = Level;

        foreach (var node in _static.SkillTree)
        {
            var item = new SkillDisplayItem
            {
                SkillName = node.SkillName,
                Level = node.Level,
                IsLearned = currentLvl >= node.Level,
                Category = node.Category,
                MpCost = node.MpCost,
                Description = node.Description,
                DamageDetail = node.DamageDetail
            };

            if (node.Category == SkillCategory.General)
            {
                GeneralSkills.Add(item);
            }
            else
            {
                AdvisorSkills.Add(item);
            }
        }
    }
}
