using Tianjige.Core.Models;

namespace Tianjige.Core.Services;

/// <summary>
/// 武将拼音首字母与全名快速检索引擎
/// </summary>
public static class PinyinSearchEngine
{
    /// <summary>
    /// 对武将列表执行快速拼音首字母/汉字搜索
    /// </summary>
    public static IReadOnlyList<GeneralStaticInfo> Search(IEnumerable<GeneralStaticInfo> generals, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return generals.ToList();
        }

        var normalized = query.Trim().ToLowerInvariant();

        return generals
            .Select(g => new
            {
                General = g,
                Score = CalculateMatchScore(g, normalized)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => Math.Max(x.General.BaseForce, x.General.BaseIntellect))
            .Select(x => x.General)
            .ToList();
    }

    private static int CalculateMatchScore(GeneralStaticInfo general, string query)
    {
        var name = general.Name.ToLowerInvariant();
        var pinyin = general.PinyinIndex.ToLowerInvariant();

        // 1. 完全匹配汉字名字
        if (name == query) return 1000;

        // 2. 完全匹配拼音首字母
        if (pinyin == query) return 900;

        // 3. 汉字开头包含
        if (name.StartsWith(query)) return 800;

        // 4. 拼音首字母以 query 开头 (例如: "ts" 匹配 "tsc")
        if (pinyin.StartsWith(query)) return 700;

        // 5. 汉字任意位置包含
        if (name.Contains(query)) return 500;

        // 6. 拼音任意位置包含
        if (pinyin.Contains(query)) return 400;

        return 0;
    }
}
