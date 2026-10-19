using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tianjige.Core.Models;

namespace Tianjige.Data.Repositories;

/// <summary>
/// 静态典籍仓储实现（加载内置 Generals, Skills, Items, Formations）
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

    public StaticRepository()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (_initialized) return;

        LoadGenerals();
        LoadSkills();
        LoadItems();
        LoadFormations();

        _initialized = true;
    }

    private void LoadGenerals()
    {
        var json = ReadEmbeddedResource("Tianjige.Data.Resources.Generals.json");
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
        var json = ReadEmbeddedResource("Tianjige.Data.Resources.Skills.json");
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
        var json = ReadEmbeddedResource("Tianjige.Data.Resources.Items.json");
        var list = JsonSerializer.Deserialize<List<ItemInfo>>(json, JsonOptions) ?? new();
        _items.Clear();
        _itemsById.Clear();

        foreach (var item in list)
        {
            _items.Add(item);
            _itemsById[item.Id] = item;
        }
    }

    private void LoadFormations()
    {
        var json = ReadEmbeddedResource("Tianjige.Data.Resources.Formations.json");
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

        if (root.TryGetProperty("troopCounters", out var troopCountersElement))
        {
            var troopCounters = JsonSerializer.Deserialize<List<TroopCounterInfo>>(troopCountersElement.GetRawText(), JsonOptions);
            if (troopCounters != null)
            {
                _troopCounters.Clear();
                _troopCounters.AddRange(troopCounters);
            }
        }
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
