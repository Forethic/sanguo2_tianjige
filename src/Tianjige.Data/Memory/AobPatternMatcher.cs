namespace Tianjige.Data.Memory;

/// <summary>
/// AOB 动态特征码内存模式匹配器
/// </summary>
public static class AobPatternMatcher
{
    /// <summary>
    /// 在指定字节流中查找特征码匹配偏移（支持 ?? 通配符）
    /// </summary>
    public static int FindPattern(ReadOnlySpan<byte> data, string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var tokens = pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var patternBytes = new byte?[tokens.Length];

        for (int i = 0; i < tokens.Length; i++)
        {
            if (tokens[i] == "??" || tokens[i] == "?")
            {
                patternBytes[i] = null;
            }
            else
            {
                patternBytes[i] = Convert.ToByte(tokens[i], 16);
            }
        }

        int maxIndex = data.Length - patternBytes.Length;
        for (int i = 0; i <= maxIndex; i++)
        {
            bool match = true;
            for (int j = 0; j < patternBytes.Length; j++)
            {
                if (patternBytes[j].HasValue && data[i + j] != patternBytes[j]!.Value)
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }
}
