namespace Tianjige.Core.Services;

/// <summary>
/// 汉末三国古典纪年与时令映射引擎
/// </summary>
public static class AncientCalendarService
{
    private static readonly string[] HeavenlyStems = { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
    private static readonly string[] EarthlyBranches = { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };

    private static readonly string[] ChineseMonths =
    {
        "孟春正月", "仲春二月", "季春三月",
        "孟夏四月", "仲夏五月", "季夏六月",
        "孟秋七月", "仲秋八月", "季秋九月",
        "孟冬十月", "仲冬十一月", "季冬腊月"
    };

    /// <summary>
    /// 将公元纪年与月份格式化为古风时节（例如：建安五年 · 岁在庚辰 · 孟秋七月）
    /// </summary>
    public static string FormatYearMonth(int year, int month)
    {
        string eraName = GetEraName(year);
        string ganzhi = GetGanZhi(year);
        int mIndex = Math.Clamp(month - 1, 0, 11);
        string monthName = ChineseMonths[mIndex];

        return $"{eraName} · 岁在{ganzhi} · {monthName}";
    }

    /// <summary>
    /// 干支纪年推导
    /// </summary>
    public static string GetGanZhi(int year)
    {
        int offset = year - 4;
        int stem = (offset % 10 + 10) % 10;
        int branch = (offset % 12 + 12) % 12;
        return $"{HeavenlyStems[stem]}{EarthlyBranches[branch]}";
    }

    /// <summary>
    /// 汉献帝与三国正统年号推导
    /// </summary>
    private static string GetEraName(int year)
    {
        if (year <= 189)
        {
            int n = year - 183;
            return n == 1 ? "中平元年" : $"中平{ToChineseNumber(n)}年";
        }
        if (year <= 193)
        {
            int n = year - 189;
            return n == 1 ? "初平元年" : $"初平{ToChineseNumber(n)}年";
        }
        if (year <= 195)
        {
            int n = year - 193;
            return n == 1 ? "兴平元年" : $"兴平{ToChineseNumber(n)}年";
        }
        if (year <= 220)
        {
            int n = year - 195;
            return n == 1 ? "建安元年" : $"建安{ToChineseNumber(n)}年";
        }
        if (year <= 226)
        {
            int n = year - 219;
            return n == 1 ? "黄初元年" : $"黄初{ToChineseNumber(n)}年";
        }

        int post = year - 226;
        return $"太和{ToChineseNumber(post)}年";
    }

    private static string ToChineseNumber(int num) => num switch
    {
        1 => "元",
        2 => "二",
        3 => "三",
        4 => "四",
        5 => "五",
        6 => "六",
        7 => "七",
        8 => "八",
        9 => "九",
        10 => "十",
        11 => "十一",
        12 => "十二",
        13 => "十三",
        14 => "十四",
        15 => "十五",
        16 => "十六",
        17 => "十七",
        18 => "十八",
        19 => "十九",
        20 => "二十",
        21 => "二十一",
        22 => "二十二",
        23 => "二十三",
        24 => "二十四",
        25 => "二十五",
        _ => num.ToString()
    };
}
