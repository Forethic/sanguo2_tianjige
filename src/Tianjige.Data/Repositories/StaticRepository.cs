using Tianjige.Core.Models;

namespace Tianjige.Data.Repositories;

/// <summary>
/// 静态典籍库仓储（负责加载 Generals.json, Skills.json 等原生典籍）
/// </summary>
public class StaticRepository
{
    private readonly List<GeneralStaticInfo> _generals = new();

    public IReadOnlyList<GeneralStaticInfo> GetAllGenerals() => _generals;
}
