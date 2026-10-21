using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization.Metadata;
using System.Collections.Generic;
using System.Linq;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

const string pakFile = "Sango2.PAK";
const string outputDir = "./Data";

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
};

Console.WriteLine("===============================================================");
Console.WriteLine(" 《天机阁》Sango2.PAK 官方 10 大 INI 纯正全量提取器 ");
Console.WriteLine("===============================================================");

if (!File.Exists(pakFile))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"【错误】在当前目录下未找到 {pakFile}！");
    Console.ResetColor();
    return;
}

try
{
    Console.WriteLine($"正在载入 {pakFile} 全文至高速内存流 (113 MB)...");
    byte[] pakBytes = File.ReadAllBytes(pakFile);
    Console.WriteLine($"载入成功！实际大小: {pakBytes.Length} 字节。\n");

    // -------------------------------------------------------------
    // 1. 提取技能库 (MAGIC.INI) -> 武将技、军师技
    // -------------------------------------------------------------
    string magicIni = ExtractFileContent(pakBytes, "MAGIC.INI") 
        ?? throw new FileNotFoundException("未在封包中定位到 MAGIC.INI");

    var magicBlocks = ParseIniBlocks(magicIni);
    var generalSkills = new List<GeneralSkillRecord>();
    var advisorSkills = new List<AdvisorSkillRecord>();
    var generalSkillDict = new Dictionary<int, string>();
    var advisorSkillDict = new Dictionary<int, string>();
    var seenMagic = new HashSet<int>();
    var seenWise = new HashSet<int>();

    foreach (var b in magicBlocks)
    {
        var sec = b.GetValueOrDefault("__SECTION__");
        int seq = ToInt(b, "SEQUENCE");
        string name = GetStr(b, "NAME");

        if (sec == "MAGIC" && seq > 0 && seenMagic.Add(seq))
        {
            generalSkills.Add(new GeneralSkillRecord(
                seq, 
                $"R{seq:D3}", 
                name, 
                ToInt(b, "MP"), 
                ToInt(b, "POWER"), 
                ToInt(b, "ATTACK"), 
                GetStr(b, "NOTE")
            ));
            generalSkillDict[seq] = name;
        }
        else if (sec == "WISE" && seq > 0 && seenWise.Add(seq))
        {
            int wiseLevel = ToInt(b, "WISELEVEL");
            string fullName = wiseLevel switch 
            { 
                1 => $"{name}(初)", 
                2 => $"{name}(中)", 
                3 => $"{name}(高)", 
                _ => name 
            };
            advisorSkills.Add(new AdvisorSkillRecord(
                seq, 
                fullName, 
                ToInt(b, "EXECUTE"), 
                wiseLevel, 
                ToInt(b, "LEVEL")
            ));
            advisorSkillDict[seq] = fullName;
        }
    }
    SaveJson("GeneralSkills.json", generalSkills, jsonOptions);
    SaveJson("AdvisorSkills.json", advisorSkills, jsonOptions);

    // -------------------------------------------------------------
    // 2. 提取物品库 (SANGO.INI) -> 武器、书籍、坐骑、珠宝
    // -------------------------------------------------------------
    string sangoIni = ExtractFileContent(pakBytes, "SANGO.INI") 
        ?? throw new FileNotFoundException("未在封包中定位到 SANGO.INI");

    var sangoBlocks = ParseIniBlocks(sangoIni);
    var weapons = new List<ItemRecord>();
    var books = new List<ItemRecord>();
    var horses = new List<ItemRecord>();
    var jewelry = new List<ItemRecord>();
    var seenItems = new HashSet<int>();

    foreach (var b in sangoBlocks)
    {
        if (b.GetValueOrDefault("__SECTION__") != "ITEM") continue;
        int seq = ToInt(b, "SEQUENCE");
        if (seq <= 0 || !seenItems.Add(seq)) continue;

        string itype = GetStr(b, "TYPE").ToUpperInvariant();
        var item = new ItemRecord(
            seq, 
            GetStr(b, "NAME"), 
            itype, 
            ToInt(b, "LEVEL"), 
            ToInt(b, "STR"), 
            ToInt(b, "INT"), 
            ToInt(b, "SPEED"), 
            ToInt(b, "MAXHP"), 
            ToInt(b, "MAXMP"), 
            ToInt(b, "LOYAL"), 
            GetStr(b, "DESCRIPTION")
        );

        if (itype == "JEWEL_WEAPON" || (seq is >= 14 and <= 47)) weapons.Add(item);
        else if (itype == "JEWEL_THING" || (seq is >= 48 and <= 55)) books.Add(item);
        else if (itype == "JEWEL_HORSE" || (seq is >= 56 and <= 63)) horses.Add(item);
        else if (itype == "JEWEL_MISC" || (seq is >= 73 and <= 77)) jewelry.Add(item);
    }
    SaveJson("Weapons.json", weapons, jsonOptions);
    SaveJson("Books.json", books, jsonOptions);
    SaveJson("Horses.json", horses, jsonOptions);
    SaveJson("Jewelry.json", jewelry, jsonOptions);

    // -------------------------------------------------------------
    // 3. 循环遍历提取全部 5 个历史剧本 (TIMES1 ~ TIMES5)
    // -------------------------------------------------------------
    var scenarioNames = new Dictionary<int, string>
    {
        [1] = "黄巾之乱 (184年)",
        [2] = "讨伐董卓 (190年)",
        [3] = "群雄割据 (194年)",
        [4] = "赤壁之战 (208年)",
        [5] = "三国鼎立 (220年)"
    };

    var masterCityDict = new Dictionary<int, string>();

    for (int t = 1; t <= 5; t++)
    {
        string fileName = $"TIMES{t}.INI";
        string? timesContent = ExtractFileContent(pakBytes, fileName);

        if (string.IsNullOrWhiteSpace(timesContent))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n⚠️ {fileName} 在封包内为空或无有效配置段落，跳过该剧本导出。");
            Console.ResetColor();
            continue;
        }

        var blocks = ParseIniBlocks(timesContent);
        string scenarioTitle = scenarioNames[t];

        // 尝试从文件内部 [SYSTEM] 节拉取官方真实定义的时期名
        var sysBlock = blocks.FirstOrDefault(x => x.GetValueOrDefault("__SECTION__") == "SYSTEM");
        if (sysBlock != null)
        {
            string timeName = GetStr(sysBlock, "TIME");
            int year = ToInt(sysBlock, "YEAR");
            if (!string.IsNullOrEmpty(timeName))
            {
                scenarioTitle = year > 0 ? $"{timeName} ({year}年)" : timeName;
            }
        }

        Console.WriteLine($"\n>> 正在解析剧本 {t}：【{scenarioTitle}】...");

        var monarchs = new List<MonarchRecord>();
        var generals = new List<GeneralRecord>();
        var cities = new List<CityRecord>();

        var seenKing = new HashSet<string>();
        var seenGen = new HashSet<int>();
        var seenCity = new HashSet<int>();

        foreach (var b in blocks)
        {
            string sec = b.GetValueOrDefault("__SECTION__", "");

            if (sec.StartsWith("KING") && seenKing.Add(sec))
            {
                monarchs.Add(new MonarchRecord(
                    sec, 
                    GetStr(b, "NAME"), 
                    ToInt(b, "FLAGNUM"), 
                    ToInt(b, "TYPE"), 
                    ToInt(b, "SELECT")
                ));
            }
            else if (sec == "GENERAL")
            {
                int seq = ToInt(b, "SEQUENCE");
                if (seq <= 0 || !seenGen.Add(seq)) continue;

                generals.Add(new GeneralRecord(
                    Sequence: seq,
                    Face: ToInt(b, "FACE"),
                    Name: GetStr(b, "NAME"),
                    Str: ToInt(b, "STR"),
                    Int: ToInt(b, "INT"),
                    BaseHp: ToInt(b, "BASEHP"),
                    BaseMp: ToInt(b, "BASEMP"),
                    City: ToInt(b, "CITY"),
                    Status: GetStr(b, "STATUS"),
                    Weapon: ToInt(b, "WEAPON"),
                    Horse: ToInt(b, "HORSE"),
                    Thing: ToInt(b, "THING"),
                    Loyal: ToInt(b, "LOYAL"),
                    GeneralSkillTree: ParseSkillPairs(GetStr(b, "SKILL"), generalSkillDict),
                    AdvisorSkillTree: ParseSkillPairs(GetStr(b, "WISESKILL"), advisorSkillDict)
                ));
            }
            else if (sec.StartsWith("CITY"))
            {
                int cityId = ToInt(b, "NO");
                if (cityId == 0) cityId = ToInt(b, "SEQUENCE");
                if (cityId == 0)
                {
                    var digits = new string(sec.Where(char.IsDigit).ToArray());
                    int.TryParse(digits, out cityId);
                }

                if (cityId <= 0 || !seenCity.Add(cityId)) continue;

                string cityName = GetStr(b, "NAME");
                if (!string.IsNullOrEmpty(cityName) && !masterCityDict.ContainsKey(cityId))
                    masterCityDict[cityId] = cityName;

                int lord = ToInt(b, "LORD");
                if (lord == 0) lord = ToInt(b, "KING");

                int chief = ToInt(b, "CHIEF");
                if (chief == 0) chief = ToInt(b, "PREFECT");

                int adviser = ToInt(b, "ADVISER");
                if (adviser == 0) adviser = ToInt(b, "ADVISOR");

                int reserve = ToInt(b, "RESERVESOLDIERNUMCUR");
                if (reserve == 0) reserve = ToInt(b, "RESERVE");

                cities.Add(new CityRecord(
                    Id: cityId,
                    Name: cityName,
                    Lord: lord,
                    Chief: chief,
                    Adviser: adviser,
                    Money: ToInt(b, "MONEY"),
                    People: ToInt(b, "PEOPLE"),
                    Defend: ToInt(b, "DEFEND"),
                    Economics: ToInt(b, "ECONOMICS"),
                    ReserveSoldier: reserve
                ));
            }
        }

        SaveJson($"Generals_Scenario{t}.json", generals, jsonOptions);
        SaveJson($"Monarchs_Scenario{t}.json", monarchs, jsonOptions);
        SaveJson($"Cities_Scenario{t}.json", cities, jsonOptions);

        if (t == 1)
        {
            SaveJson("Generals.json", generals, jsonOptions);
            SaveJson("Monarchs.json", monarchs, jsonOptions);
        }
    }

    // 导出全图城池名录总表 (纯 Id + Name，严格剔除坐标)
    var masterCities = masterCityDict.OrderBy(x => x.Key)
                                     .Select(x => new CityMasterRecord(x.Key, x.Value))
                                     .ToList();
    SaveJson("Cities.json", masterCities, jsonOptions);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n===============================================================");
    Console.WriteLine(" [全部完成] 官方 10 大 INI 核心数据已全量落地至 ./Data ！");
    Console.WriteLine("===============================================================");
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n[提取中断] {ex.Message}");
    Console.ResetColor();
}

// =========================================================================
// 核心穿透解析器与底层辅助函数
// =========================================================================

static string? ExtractFileContent(byte[] pakBytes, string targetFileName)
{
    Console.Write($"正在定位 {targetFileName}... ");
    byte[] nameBytes = Encoding.ASCII.GetBytes(targetFileName.ToUpperInvariant());

    for (int i = 0; i <= pakBytes.Length - nameBytes.Length; i++)
    {
        bool match = true;
        for (int j = 0; j < nameBytes.Length; j++)
        {
            if (char.ToUpperInvariant((char)pakBytes[i + j]) != (char)nameBytes[j])
            {
                match = false;
                break;
            }
        }
        if (!match) continue;

        // 探测目录项结构中的 offset 与 size (不再限制必须大于200字节)
        for (int delta = nameBytes.Length; delta <= 80; delta++)
        {
            int p = i + delta;
            if (p + 8 > pakBytes.Length) break;

            uint cand1 = BitConverter.ToUInt32(pakBytes, p);
            uint cand2 = BitConverter.ToUInt32(pakBytes, p + 4);

            if (TrySliceIni(pakBytes, cand1, cand2, out string? text) || 
                TrySliceIni(pakBytes, cand2, cand1, out text))
            {
                Console.WriteLine($"[成功锁定: 偏移 0x{Math.Min(cand1, cand2):X}, 大小 {text?.Length ?? 0} 字符]");
                return text;
            }
        }
    }
    Console.WriteLine("[未发现有效偏移]");
    return null;
}

static bool TrySliceIni(byte[] pakBytes, uint offset, uint size, out string? text)
{
    text = null;
    // 解除 200 字节门槛：只要 size > 0 且在封包范围内即可抽取
    if (offset > 0 && offset + size <= pakBytes.Length && size > 0 && size < 6 * 1024 * 1024)
    {
        byte[] slice = new byte[size];
        Array.Copy(pakBytes, offset, slice, 0, size);
        text = Encoding.GetEncoding(950).GetString(slice);
        return true;
    }
    return false;
}

static List<SkillNode> ParseSkillPairs(string rawStr, Dictionary<int, string> nameDict)
{
    var list = new List<SkillNode>();
    if (string.IsNullOrWhiteSpace(rawStr)) return list;

    var pairs = rawStr.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    foreach (var pair in pairs)
    {
        var parts = pair.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && int.TryParse(parts[0], out int level) && int.TryParse(parts[1], out int skillId))
        {
            string name = nameDict.TryGetValue(skillId, out var n) ? n : $"技能{skillId}";
            list.Add(new SkillNode(level, skillId, name));
        }
    }
    return list.OrderBy(x => x.Level).ToList();
}

static List<Dictionary<string, string>> ParseIniBlocks(string rawText)
{
    var blocks = new List<Dictionary<string, string>>();
    Dictionary<string, string>? currentBlock = null;

    using var reader = new StringReader(rawText);
    string? rawLine;

    while ((rawLine = reader.ReadLine()) != null)
    {
        var line = rawLine.Trim();
        if (string.IsNullOrEmpty(line) || line.StartsWith(';') || line.StartsWith("//") || line.StartsWith('#'))
            continue;

        if (line.StartsWith('[') && line.EndsWith(']'))
        {
            var secName = line[1..^1].Trim().ToUpperInvariant();
            currentBlock = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["__SECTION__"] = secName
            };
            blocks.Add(currentBlock);
            continue;
        }

        if (currentBlock != null)
        {
            var splitIdx = line.IndexOf('=');
            if (splitIdx > 0)
            {
                var k = line[..splitIdx].Trim().ToUpperInvariant();
                var v = line[(splitIdx + 1)..].Trim();

                if (k is "SKILL" or "WISESKILL")
                {
                    if (currentBlock.TryGetValue(k, out var existing))
                        currentBlock[k] = $"{existing};{v}";
                    else
                        currentBlock[k] = v;
                }
                else
                {
                    currentBlock[k] = v;
                }
            }
        }
    }

    return blocks;
}

static void SaveJson<T>(string fileName, List<T> data, JsonSerializerOptions options)
{
    if (!Directory.Exists(outputDir))
        Directory.CreateDirectory(outputDir);

    var path = Path.Combine(outputDir, fileName);
    File.WriteAllText(path, JsonSerializer.Serialize(data, options), Encoding.UTF8);
    Console.WriteLine($"  --> [生成] {fileName} (记录: {data.Count} 条)");
}

static int ToInt(Dictionary<string, string> dict, string key) =>
    dict.TryGetValue(key, out var val) && int.TryParse(val, out var res) ? res : 0;

static string GetStr(Dictionary<string, string> dict, string key) =>
    dict.TryGetValue(key, out var val) ? val : string.Empty;

// 强类型领域模型定义
record CityRecord(int Id, string Name, int Lord, int Chief, int Adviser, int Money, int People, int Defend, int Economics, int ReserveSoldier);
record CityMasterRecord(int Id, string Name);
record SkillNode(int Level, int SkillId, string Name);
record ItemRecord(int Sequence, string Name, string Type, int Level, int Str, int Int, int Speed, int MaxHp, int MaxMp, int Loyal, string Description);
record GeneralSkillRecord(int Sequence, string Code, string Name, int Mp, int Power, int Attack, string Note);
record AdvisorSkillRecord(int Sequence, string Name, int Execute, int WiseLevel, int Level);
record MonarchRecord(string Section, string Name, int FlagNum, int Type, int Select);
record GeneralRecord(int Sequence, int Face, string Name, int Str, int Int, int BaseHp, int BaseMp, int City, string Status, int Weapon, int Horse, int Thing, int Loyal, List<SkillNode> GeneralSkillTree, List<SkillNode> AdvisorSkillTree);