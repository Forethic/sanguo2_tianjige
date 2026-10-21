namespace Tianjige.Core.Models;

/// <summary>
/// 郡城关隘静态档案
/// </summary>
public sealed record CityInfo
{
    public required int Id { get; init; }
    public required string Name { get; init; }
}

/// <summary>
/// 历史剧本元数据信息
/// </summary>
public sealed record ScenarioMetaInfo
{
    public required int ScenarioId { get; init; } // 1 ~ 5
    public required string Title { get; init; } // "黄巾之乱", "讨伐董卓", "群雄割据", "赤壁之战", "三国鼎立"
    public required int StartYear { get; init; } // 184, 190, 196, 208, 219
    public required string Description { get; init; }
}
