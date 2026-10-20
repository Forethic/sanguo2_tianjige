using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tianjige.Core.Models;

namespace Tianjige.Data.Repositories;

/// <summary>
/// 静态典籍仓储实现（优先加载外部 data/*.json 文件，数据修改无需重新编译；回退使用内置嵌入式资源）
/// </summary>
public class StaticRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly List<GeneralStaticInfo> _generals = new();
    private readonly Dictionary<int, GeneralStaticInfo> _generalsById = new();
    private readonly Dictionary<string, GeneralStaticInfo> _generalsByName = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<SkillInfo> _skills = new();
    private readonly Dictionary<string, SkillInfo> _skillsByName = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<ItemInfo> _items = new();
    private readonly Dictionary<int, ItemInfo> _itemsById = new();

    private readonly List<FormationInfo> _formations = new();
    private readonly List<TroopCounterInfo> _troopCounters = new();

    private bool _initialized;
    private string? _resolvedDataDirectory;

    public string DataDirectory => _resolvedDataDirectory ??= ResolveDataDirectory();

    public StaticRepository()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (_initialized) return;
        Reload();
        _initialized = true;
    }

    /// <summary>
    /// 热重新加载所有外部 JSON 配置（修改文件后即时生效，无需重编代码）
    /// </summary>
    public void Reload()
    {
        LoadGenerals();
        LoadSkills();
        LoadItems();
        LoadFormationsAndTroops();
    }

    private void LoadGenerals()
    {
        var json = LoadJsonContent("Generals.json", "Tianjige.Data.Resources.Generals.json");
        var list = JsonSerializer.Deserialize<List<GeneralStaticInfo>>(json, JsonOptions) ?? new();
        _generals.Clear();
        _generalsById.Clear();
        _generalsByName.Clear();

        foreach (var general in list)
        {
            _generals.Add(general);
            _generalsById[general.Id] = general;
            _generalsByName[general.Name] = general;
        }
    }

    private void LoadSkills()
    {
        var json = LoadJsonContent("Skills.json", "Tianjige.Data.Resources.Skills.json");
        var list = JsonSerializer.Deserialize<List<SkillInfo>>(json, JsonOptions) ?? new();
        _skills.Clear();
        _skillsByName.Clear();

        foreach (var skill in list)
        {
            _skills.Add(skill);
            _skillsByName[skill.Name] = skill;
        }
    }

    private void LoadItems()
    {
        var json = LoadJsonContent("Items.json", "Tianjige.Data.Resources.Items.json");
        var list = JsonSerializer.Deserialize<List<ItemInfo>>(json, JsonOptions) ?? new();
        _items.Clear();
        _itemsById.Clear();

        foreach (var item in list)
        {
            _items.Add(item);
            _itemsById[item.Id] = item;
        }
    }

    private void LoadFormationsAndTroops()
    {
        // 优先尝试独立的 troops.json（兵种相克表）
        var troopsJson = TryLoadExternalJson("Troops.json");
        if (!string.IsNullOrEmpty(troopsJson))
        {
            var troopCounters = JsonSerializer.Deserialize<List<TroopCounterInfo>>(troopsJson, JsonOptions);
            if (troopCounters != null)
            {
                _troopCounters.Clear();
                _troopCounters.AddRange(troopCounters);
            }
        }

        var json = LoadJsonContent("Formations.json", "Tianjige.Data.Resources.Formations.json");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("formations", out var formationsElement))
        {
            var formations = JsonSerializer.Deserialize<List<FormationInfo>>(formationsElement.GetRawText(), JsonOptions);
            if (formations != null)
            {
                _formations.Clear();
                _formations.AddRange(formations);
            }
        }

        // 如果没有外部独立的 troops.json，从 formations.json 中读取 troopCounters
        if (_troopCounters.Count == 0 && root.TryGetProperty("troopCounters", out var troopCountersElement))
        {
            var troopCounters = JsonSerializer.Deserialize<List<TroopCounterInfo>>(troopCountersElement.GetRawText(), JsonOptions);
            if (troopCounters != null)
            {
                _troopCounters.Clear();
                _troopCounters.AddRange(troopCounters);
            }
        }
    }

    private string LoadJsonContent(string filename, string embeddedResourceName)
    {
        var externalContent = TryLoadExternalJson(filename);
        if (!string.IsNullOrEmpty(externalContent))
        {
            return externalContent;
        }

        return ReadEmbeddedResource(embeddedResourceName);
    }

    private string? TryLoadExternalJson(string filename)
    {
        try
        {
            var directPath = Path.Combine(DataDirectory, filename);
            if (File.Exists(directPath)) return File.ReadAllText(directPath, Encoding.UTF8);

            var lowerPath = Path.Combine(DataDirectory, filename.ToLowerInvariant());
            if (File.Exists(lowerPath)) return File.ReadAllText(lowerPath, Encoding.UTF8);
        }
        catch { }

        return null;
    }

    private string ResolveDataDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "data"),
            Path.Combine(Directory.GetCurrentDirectory(), "data"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "data"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data")
        };

        foreach (var dir in candidates)
        {
            try
            {
                if (Directory.Exists(dir)) return Path.GetFullPath(dir);
            }
            catch { }
        }

        return Path.Combine(AppContext.BaseDirectory, "data");
    }

    /// <summary>
    /// 解析武将头像实际物理路径（优先检测 data/avatars 或 data/ 目录）
    /// </summary>
    public string? ResolveAvatarPath(string avatarRelativePath)
    {
        if (string.IsNullOrWhiteSpace(avatarRelativePath)) return null;

        var candidates = new[]
        {
            Path.Combine(DataDirectory, avatarRelativePath),
            Path.Combine(DataDirectory, "avatars", Path.GetFileName(avatarRelativePath)),
            Path.Combine(AppContext.BaseDirectory, avatarRelativePath),
            Path.Combine(AppContext.BaseDirectory, "data", avatarRelativePath),
            Path.Combine(Directory.GetCurrentDirectory(), avatarRelativePath)
        };

        foreach (var path in candidates)
        {
            try
            {
                if (File.Exists(path)) return Path.GetFullPath(path);
            }
            catch { }
        }

        return null;
    }

    private static string ReadEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"找不到嵌入式典籍资源: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public IReadOnlyList<GeneralStaticInfo> GetAllGenerals() => _generals;
    public GeneralStaticInfo? GetGeneralById(int id) => _generalsById.GetValueOrDefault(id);
    public GeneralStaticInfo? GetGeneralByName(string name) => _generalsByName.GetValueOrDefault(name);

    public IReadOnlyList<SkillInfo> GetAllSkills() => _skills;
    public SkillInfo? GetSkillByName(string name) => _skillsByName.GetValueOrDefault(name);

    public IReadOnlyList<ItemInfo> GetAllItems() => _items;
    public ItemInfo? GetItemById(int id) => _itemsById.GetValueOrDefault(id);

    public IReadOnlyList<FormationInfo> GetFormations() => _formations;
    public IReadOnlyList<TroopCounterInfo> GetTroopCounters() => _troopCounters;
}
