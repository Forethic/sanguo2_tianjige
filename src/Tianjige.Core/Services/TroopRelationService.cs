using Tianjige.Core.Models;

namespace Tianjige.Core.Services;

/// <summary>
/// 兵种相对克制关系
/// </summary>
public enum TroopRelation
{
    Self = 0,    // 自身
    Strong = 1,  // 我克制对方（战局优势）
    Weak = 2,    // 被对方克制（战局劣势）
    Even = 3     // 平手或互不相克
}

/// <summary>
/// 单一兵种全盘相克客观推演结果
/// </summary>
public sealed record TroopAnalysis
{
    public required TroopCounterInfo Troop { get; init; }
    public IReadOnlyList<string> StrongAgainst { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> WeakAgainst { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> EvenAgainst { get; init; } = Array.Empty<string>();
}

/// <summary>
/// 兵种相克纯客观推演引擎
/// </summary>
public class TroopRelationService
{
    private readonly IReadOnlyList<TroopCounterInfo> _troops;
    private readonly Dictionary<string, TroopCounterInfo> _troopsByName;

    public TroopRelationService(IReadOnlyList<TroopCounterInfo> troops)
    {
        _troops = troops ?? throw new ArgumentNullException(nameof(troops));
        _troopsByName = _troops.ToDictionary(t => t.TroopName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取原兵种对目标兵种的克制关系
    /// </summary>
    public TroopRelation GetRelation(string sourceTroopName, string targetTroopName)
    {
        if (string.Equals(sourceTroopName, targetTroopName, StringComparison.OrdinalIgnoreCase))
        {
            return TroopRelation.Self;
        }

        var source = _troopsByName.GetValueOrDefault(sourceTroopName);
        var target = _troopsByName.GetValueOrDefault(targetTroopName);

        if (source == null || target == null)
        {
            return TroopRelation.Even;
        }

        if (source.AdvantageMatrix != null && source.AdvantageMatrix.TryGetValue(target.TroopName, out int adv))
        {
            if (adv > 0) return TroopRelation.Strong;
            if (adv < 0) return TroopRelation.Weak;
            return TroopRelation.Even;
        }

        bool sourceStrong = MatchesAny(source.StrongAgainst, target.TroopName) ||
                            MatchesAny(target.WeakAgainst, source.TroopName);

        bool sourceWeak = MatchesAny(source.WeakAgainst, target.TroopName) ||
                          MatchesAny(target.StrongAgainst, source.TroopName);

        if (sourceStrong && !sourceWeak) return TroopRelation.Strong;
        if (sourceWeak && !sourceStrong) return TroopRelation.Weak;
        return TroopRelation.Even;
    }

    private static bool MatchesAny(IReadOnlyList<string> list, string candidate)
    {
        var candTrim = candidate.Replace("兵", "");
        foreach (var item in list)
        {
            if (string.Equals(item, candidate, StringComparison.OrdinalIgnoreCase)) return true;
            if (item.Contains(candTrim) || candidate.Contains(item.Replace("兵", ""))) return true;
            if (item == "一般步兵" && (candidate.Contains("刀") || candidate.Contains("枪"))) return true;
        }
        return false;
    }

    /// <summary>
    /// 客观推演指定兵种的全盘相克格局
    /// </summary>
    public TroopAnalysis AnalyzeTroop(string troopName)
    {
        var troop = _troopsByName.GetValueOrDefault(troopName)
            ?? throw new ArgumentException($"未找到兵种数据: {troopName}", nameof(troopName));

        var strong = new List<string>();
        var weak = new List<string>();
        var even = new List<string>();

        foreach (var other in _troops)
        {
            if (other.TroopName == troop.TroopName) continue;
            var rel = GetRelation(troop.TroopName, other.TroopName);
            switch (rel)
            {
                case TroopRelation.Strong:
                    strong.Add(other.TroopName);
                    break;
                case TroopRelation.Weak:
                    weak.Add(other.TroopName);
                    break;
                default:
                    even.Add(other.TroopName);
                    break;
            }
        }

        return new TroopAnalysis
        {
            Troop = troop,
            StrongAgainst = strong,
            WeakAgainst = weak,
            EvenAgainst = even
        };
    }
}
