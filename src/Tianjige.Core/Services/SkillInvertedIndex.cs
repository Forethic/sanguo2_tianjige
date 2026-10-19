using Tianjige.Core.Models;

namespace Tianjige.Core.Services;

/// <summary>
/// 掌握该技能的武将精简明细
/// </summary>
public sealed record SkillMasteryInfo
{
    public required int GeneralId { get; init; }
    public required string GeneralName { get; init; }
    public required string SealTier { get; init; }
    public required byte BaseForce { get; init; }
    public required byte BaseIntellect { get; init; }
    public required byte UnlockLevel { get; init; }
    public required SkillCategory Category { get; init; }
    public required byte MpCost { get; init; }
}

/// <summary>
/// 技能-武将倒排检索引擎（点击任意技能名称瞬间反查全图武将清单）
/// </summary>
public sealed class SkillInvertedIndex
{
    private readonly Dictionary<string, List<SkillMasteryInfo>> _index = new(StringComparer.OrdinalIgnoreCase);

    public SkillInvertedIndex(IEnumerable<GeneralStaticInfo> generals)
    {
        BuildIndex(generals);
    }

    private void BuildIndex(IEnumerable<GeneralStaticInfo> generals)
    {
        _index.Clear();

        foreach (var general in generals)
        {
            foreach (var node in general.SkillTree)
            {
                if (!_index.TryGetValue(node.SkillName, out var list))
                {
                    list = new List<SkillMasteryInfo>();
                    _index[node.SkillName] = list;
                }

                list.Add(new SkillMasteryInfo
                {
                    GeneralId = general.Id,
                    GeneralName = general.Name,
                    SealTier = general.SealTier,
                    BaseForce = general.BaseForce,
                    BaseIntellect = general.BaseIntellect,
                    UnlockLevel = node.Level,
                    Category = node.Category,
                    MpCost = node.MpCost
                });
            }
        }

        // 对每个技能下的武将按解锁等级升序、资质能力降序排序
        foreach (var entry in _index.Values)
        {
            entry.Sort((a, b) =>
            {
                var cmp = a.UnlockLevel.CompareTo(b.UnlockLevel);
                if (cmp != 0) return cmp;
                return Math.Max(b.BaseForce, b.BaseIntellect).CompareTo(Math.Max(a.BaseForce, a.BaseIntellect));
            });
        }
    }

    /// <summary>
    /// 获取掌握特定技能的所有武将清单
    /// </summary>
    public IReadOnlyList<SkillMasteryInfo> GetMastersOfSkill(string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return Array.Empty<SkillMasteryInfo>();
        }

        return _index.TryGetValue(skillName.Trim(), out var list) 
            ? list 
            : Array.Empty<SkillMasteryInfo>();
    }

    /// <summary>
    /// 获取当前索引中已记录的所有独立技能名称
    /// </summary>
    public IReadOnlyCollection<string> IndexedSkillNames => _index.Keys;
}
