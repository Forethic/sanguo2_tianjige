using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tianjige.Core.Models;
using Tianjige.Data.Models;

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

    private readonly List<CityInfo> _cities = new();
    private readonly Dictionary<int, CityInfo> _citiesById = new();
    private readonly Dictionary<string, CityInfo> _citiesByName = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<FormationInfo> _formations = new();
    private readonly List<TroopCounterInfo> _troopCounters = new();

    private readonly AvatarResolver _avatarResolver;

    private bool _initialized;
    private string? _resolvedDataDirectory;

    public string DataDirectory => _resolvedDataDirectory ??= ResolveDataDirectory();
    public AvatarResolver AvatarResolver => _avatarResolver;

    public StaticRepository(string? dataDirectory = null)
    {
        if (!string.IsNullOrEmpty(dataDirectory))
        {
            _resolvedDataDirectory = Path.GetFullPath(dataDirectory);
        }
        _avatarResolver = new AvatarResolver(Path.Combine(DataDirectory, "avatars"));
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
        _avatarResolver.Refresh();
        LoadSkills();
        LoadItems();
        LoadGenerals();
        LoadCities();
        LoadFormationsAndTroops();
    }

    private void LoadGenerals()
    {
        _generals.Clear();
        _generalsById.Clear();
        _generalsByName.Clear();

        var externalJson = TryLoadExternalJson("Generals.json");
        if (!string.IsNullOrEmpty(externalJson) && (externalJson.Contains("\"Sequence\"") || externalJson.Contains("\"GeneralSkillTree\"")))
        {
            var rawGenerals = JsonSerializer.Deserialize<List<RawGeneralDto>>(externalJson, JsonOptions) ?? new();
            foreach (var raw in rawGenerals)
            {
                var simpName = AvatarResolver.ToSimplified(raw.Name);
                var pinyin = GeneralPinyinTable.GetPinyinIndex(simpName);
                if (string.IsNullOrEmpty(pinyin))
                {
                    pinyin = GeneralPinyinTable.GetPinyinIndex(raw.Name);
                }

                var sealTier = raw.Str switch
                {
                    >= 95 => "无双",
                    _ when raw.Int >= 95 => "谋主",
                    >= 85 => "良将",
                    _ when raw.Int >= 85 => "谋士",
                    >= 70 => "裨将",
                    _ => "白身"
                };

                // 组装双技能树并按习得等级升序排列
                var skillNodes = new List<SkillTreeNode>();
                foreach (var gSkill in raw.GeneralSkillTree)
                {
                    var skillName = AvatarResolver.ToSimplified(gSkill.Name);
                    var skill = _skillsByName.GetValueOrDefault(skillName) ?? _skillsByName.GetValueOrDefault(gSkill.Name);
                    skillNodes.Add(new SkillTreeNode
                    {
                        Level = gSkill.Level,
                        SkillName = skillName,
                        Category = SkillCategory.General,
                        MpCost = skill?.MpCost ?? (byte)20,
                        Description = skill?.Description ?? "武将技",
                        DamageDetail = skill?.DamageDetail ?? string.Empty
                    });
                }

                foreach (var aSkill in raw.AdvisorSkillTree)
                {
                    var skillName = AvatarResolver.ToSimplified(aSkill.Name);
                    var skill = _skillsByName.GetValueOrDefault(skillName) ?? _skillsByName.GetValueOrDefault(aSkill.Name);
                    skillNodes.Add(new SkillTreeNode
                    {
                        Level = aSkill.Level,
                        SkillName = skillName,
                        Category = SkillCategory.Military,
                        MpCost = 0,
                        Description = skill?.Description ?? "军师技",
                        DamageDetail = skill?.DamageDetail ?? string.Empty
                    });
                }

                var avatarPath = _avatarResolver.ResolveByName(raw.Name) ?? $"avatars/{simpName}.bmp";

                var general = new GeneralStaticInfo
                {
                    Id = raw.Sequence,
                    Name = simpName,
                    PinyinIndex = pinyin,
                    SealTier = sealTier,
                    BaseForce = raw.Str,
                    BaseIntellect = raw.Int,
                    Comment = $"三国群英传2名将（忠诚:{raw.Loyal}）",
                    Avatar = avatarPath,
                    SkillTree = skillNodes.OrderBy(s => s.Level).ToList()
                };

                _generals.Add(general);
                _generalsById[general.Id] = general;
                _generalsByName[general.Name] = general;
                if (raw.Name != general.Name)
                {
                    _generalsByName[raw.Name] = general;
                }
            }
            return;
        }

        // 回退到旧 Generals.json / 嵌入式资源
        var json = LoadJsonContent("Generals.json", "Tianjige.Data.Resources.Generals.json");
        var list = JsonSerializer.Deserialize<List<GeneralStaticInfo>>(json, JsonOptions) ?? new();
        foreach (var general in list)
        {
            _generals.Add(general);
            _generalsById[general.Id] = general;
            _generalsByName[general.Name] = general;
        }
    }

    private void LoadSkills()
    {
        _skills.Clear();
        _skillsByName.Clear();

        var generalSkillsJson = TryLoadExternalJson("GeneralSkills.json");
        var advisorSkillsJson = TryLoadExternalJson("AdvisorSkills.json");

        if (!string.IsNullOrEmpty(generalSkillsJson))
        {
            var rawGeneralSkills = JsonSerializer.Deserialize<List<RawGeneralSkillDto>>(generalSkillsJson, JsonOptions) ?? new();
            foreach (var raw in rawGeneralSkills)
            {
                var simpName = AvatarResolver.ToSimplified(raw.Name);
                var skill = new SkillInfo
                {
                    Id = raw.Sequence,
                    Name = simpName,
                    Category = SkillCategory.General,
                    MpCost = raw.Mp,
                    Description = raw.Note,
                    Tag = "武将技",
                    DamageDetail = $"基础威力: {raw.Power} | 杀伤: {raw.Attack}"
                };
                _skills.Add(skill);
                _skillsByName[skill.Name] = skill;
                if (raw.Name != skill.Name) _skillsByName[raw.Name] = skill;
            }

            if (!string.IsNullOrEmpty(advisorSkillsJson))
            {
                var rawAdvisorSkills = JsonSerializer.Deserialize<List<RawAdvisorSkillDto>>(advisorSkillsJson, JsonOptions) ?? new();
                foreach (var raw in rawAdvisorSkills)
                {
                    var simpName = AvatarResolver.ToSimplified(raw.Name);
                    var skill = new SkillInfo
                    {
                        Id = raw.Sequence + 1000,
                        Name = simpName,
                        Category = SkillCategory.Military,
                        MpCost = 0,
                        Description = $"军师策谋（指令:{raw.Execute}，要求等级:{raw.WiseLevel}）",
                        Tag = "军师技",
                        DamageDetail = $"策谋熟练要求: {raw.Level}"
                    };
                    _skills.Add(skill);
                    _skillsByName[skill.Name] = skill;
                    if (raw.Name != skill.Name) _skillsByName[raw.Name] = skill;
                }
            }
            return;
        }

        // 回退到 Skills.json / 嵌入式资源
        var json = LoadJsonContent("Skills.json", "Tianjige.Data.Resources.Skills.json");
        var list = JsonSerializer.Deserialize<List<SkillInfo>>(json, JsonOptions) ?? new();
        foreach (var skill in list)
        {
            _skills.Add(skill);
            _skillsByName[skill.Name] = skill;
        }
    }

    private void LoadItems()
    {
        _items.Clear();
        _itemsById.Clear();

        var weaponsJson = TryLoadExternalJson("Weapons.json");
        if (!string.IsNullOrEmpty(weaponsJson))
        {
            LoadRawItemsFile("Weapons.json", ItemCategory.Weapon);
            LoadRawItemsFile("Horses.json", ItemCategory.Horse);
            LoadRawItemsFile("Books.json", ItemCategory.Book);
            LoadRawItemsFile("Jewelry.json", ItemCategory.Special);
            return;
        }

        // 回退到 Items.json / 嵌入式资源
        var json = LoadJsonContent("Items.json", "Tianjige.Data.Resources.Items.json");
        var list = JsonSerializer.Deserialize<List<ItemInfo>>(json, JsonOptions) ?? new();
        foreach (var item in list)
        {
            _items.Add(item);
            _itemsById[item.Id] = item;
        }
    }

    private void LoadRawItemsFile(string fileName, ItemCategory defaultCategory)
    {
        var json = TryLoadExternalJson(fileName);
        if (string.IsNullOrEmpty(json)) return;

        var rawList = JsonSerializer.Deserialize<List<RawItemDto>>(json, JsonOptions) ?? new();
        foreach (var raw in rawList)
        {
            var category = raw.Type switch
            {
                "JEWEL_WEAPON" => ItemCategory.Weapon,
                "JEWEL_HORSE" => ItemCategory.Horse,
                "JEWEL_THING" => ItemCategory.Book,
                _ => defaultCategory
            };

            var item = new ItemInfo
            {
                Id = raw.Sequence,
                Name = AvatarResolver.ToSimplified(raw.Name),
                Category = category,
                ForceBonus = raw.Str,
                IntellectBonus = raw.Int,
                HpBonus = raw.MaxHp,
                SpeedBonus = raw.Speed,
                Description = raw.Description
            };

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
    /// 按武将姓名直接解析武将头像物理路径（支持简繁自动匹配与扩展名降级）
    /// </summary>
    public string? ResolveAvatarByName(string generalName)
    {
        return _avatarResolver.ResolveByName(generalName);
    }

    /// <summary>
    /// 解析武将头像实际物理路径（优先检测直接名称索引，次优先检测 data/avatars 或 data/ 相对路径）
    /// </summary>
    public string? ResolveAvatarPath(string avatarRelativePathOrGeneralName)
    {
        if (string.IsNullOrWhiteSpace(avatarRelativePathOrGeneralName)) return null;

        // 1. 优先尝试直接按名称索引头像
        var resolvedByName = _avatarResolver.ResolveByName(avatarRelativePathOrGeneralName);
        if (resolvedByName != null) return resolvedByName;

        // 2. 按路径 candidates 遍历
        var candidates = new[]
        {
            Path.Combine(DataDirectory, avatarRelativePathOrGeneralName),
            Path.Combine(DataDirectory, "avatars", Path.GetFileName(avatarRelativePathOrGeneralName)),
            Path.Combine(AppContext.BaseDirectory, avatarRelativePathOrGeneralName),
            Path.Combine(AppContext.BaseDirectory, "data", avatarRelativePathOrGeneralName),
            Path.Combine(Directory.GetCurrentDirectory(), avatarRelativePathOrGeneralName)
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

    public IReadOnlyList<CityInfo> GetAllCities() => _cities;
    public CityInfo? GetCityById(int id) => _citiesById.GetValueOrDefault(id);
    public CityInfo? GetCityByName(string name) => _citiesByName.GetValueOrDefault(name);

    public IReadOnlyList<FormationInfo> GetFormations() => _formations;
    public IReadOnlyList<TroopCounterInfo> GetTroopCounters() => _troopCounters;

    public static readonly IReadOnlyList<ScenarioMetaInfo> Scenarios = new[]
    {
        new ScenarioMetaInfo { ScenarioId = 1, Title = "黄巾之乱", StartYear = 184, Description = "黄巾倡乱，天下骚然；何进总兵，群雄并起。" },
        new ScenarioMetaInfo { ScenarioId = 2, Title = "讨伐董卓", StartYear = 190, Description = "董卓擅权，迁都长安；十八路诸侯并起伐董。" },
        new ScenarioMetaInfo { ScenarioId = 3, Title = "群雄割据", StartYear = 196, Description = "吕布袭徐州，曹操据许昌；天子流离，诸侯逐鹿。" },
        new ScenarioMetaInfo { ScenarioId = 4, Title = "赤壁之战", StartYear = 208, Description = "曹公兵临赤壁，孙刘联军火烧连营；鼎足初具。" },
        new ScenarioMetaInfo { ScenarioId = 5, Title = "三国鼎立", StartYear = 219, Description = "汉中争霸，关公北伐；三分天下，国祚分张。" }
    };

    private void LoadCities()
    {
        _cities.Clear();
        _citiesById.Clear();
        _citiesByName.Clear();

        var json = TryLoadExternalJson("Cities.json");
        if (!string.IsNullOrEmpty(json))
        {
            var rawList = JsonSerializer.Deserialize<List<RawCityDto>>(json, JsonOptions) ?? new();
            foreach (var raw in rawList)
            {
                var simpName = AvatarResolver.ToSimplified(raw.Name);
                if (simpName == "予章") simpName = "豫章";
                if (simpName == "鄴") simpName = "邺城";
                if (simpName == "長板") simpName = "长坂";

                var city = new CityInfo
                {
                    Id = raw.Id,
                    Name = simpName
                };

                _cities.Add(city);
                _citiesById[city.Id] = city;
                _citiesByName[city.Name] = city;
                if (raw.Name != city.Name)
                {
                    _citiesByName[raw.Name] = city;
                }
            }
        }
    }

    public IReadOnlyList<RawScenarioCityDto> GetScenarioCities(int scenarioId = 1)
    {
        var json = TryLoadExternalJson($"Cities_Scenario{scenarioId}.json");
        if (string.IsNullOrEmpty(json)) return Array.Empty<RawScenarioCityDto>();
        return JsonSerializer.Deserialize<List<RawScenarioCityDto>>(json, JsonOptions) ?? (IReadOnlyList<RawScenarioCityDto>)Array.Empty<RawScenarioCityDto>();
    }

    public IReadOnlyList<RawGeneralDto> GetScenarioGenerals(int scenarioId = 1)
    {
        var json = TryLoadExternalJson($"Generals_Scenario{scenarioId}.json");
        if (string.IsNullOrEmpty(json)) return Array.Empty<RawGeneralDto>();
        return JsonSerializer.Deserialize<List<RawGeneralDto>>(json, JsonOptions) ?? (IReadOnlyList<RawGeneralDto>)Array.Empty<RawGeneralDto>();
    }

    public IReadOnlyList<RawMonarchDto> GetScenarioMonarchs(int scenarioId = 1)
    {
        var json = TryLoadExternalJson($"Monarchs_Scenario{scenarioId}.json");
        if (string.IsNullOrEmpty(json)) return Array.Empty<RawMonarchDto>();
        return JsonSerializer.Deserialize<List<RawMonarchDto>>(json, JsonOptions) ?? (IReadOnlyList<RawMonarchDto>)Array.Empty<RawMonarchDto>();
    }
}
