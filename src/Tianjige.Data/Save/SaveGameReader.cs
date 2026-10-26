using System.Text;
using Tianjige.Core.Models;
using Tianjige.Data.Memory.Models;
using Tianjige.Data.Repositories;

namespace Tianjige.Data.Save;

/// <summary>
/// 存档槽位基础描述
/// </summary>
public sealed record SaveSlotSummary
{
    public required int SlotNumber { get; init; } // 1 ~ 10
    public required string FilePath { get; init; }
    public required bool Exists { get; init; }
    public required bool IsEmpty { get; init; }
    public required string SaveTitle { get; init; }
    public required string MonarchName { get; init; }
    public required string FactionColor { get; init; }
    public required int GameYear { get; init; }
    public required int CityCount { get; init; }
    public required int GeneralCount { get; init; }
    public string ScenarioPeriodName { get; init; } = "三国争霸";
    public DateTime? LastModifiedTime { get; init; }

    public string SlotDisplayName => $"进度 {SlotNumber:D2}";
    public string ModifiedDisplay => LastModifiedTime.HasValue ? LastModifiedTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "无记录";
}

/// <summary>
/// 存档全量解析后的结构快照
/// </summary>
public sealed record SaveGameSnapshot
{
    public required int SlotNumber { get; init; }
    public required string FilePath { get; init; }
    public required string SaveTitle { get; init; }
    public required string MonarchName { get; init; }
    public required string FactionColor { get; init; }
    public required int GameYear { get; init; }
    public string ScenarioPeriodName { get; init; } = "三国争霸";
    public IReadOnlyList<LiveGeneralRecord> Generals { get; init; } = Array.Empty<LiveGeneralRecord>();
    public IReadOnlyList<LiveCityRecord> Cities { get; init; } = Array.Empty<LiveCityRecord>();
    public DateTime LoadedTime { get; init; } = DateTime.Now;
}

/// <summary>
/// 《三国群英传2》官方原版二进制存档安全读取器
/// </summary>
public class SaveGameReader
{
    private static readonly Encoding Big5Encoding;

    static SaveGameReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Big5Encoding = Encoding.GetEncoding(950);
    }

    private readonly StaticRepository _repository;

    public SaveGameReader(StaticRepository? repository = null)
    {
        _repository = repository ?? new StaticRepository();
    }

    /// <summary>
    /// 获取存档所在的官方子目录 (SAVES)
    /// </summary>
    public static string GetSavesDirectory(string gameDirectory)
    {
        if (string.IsNullOrWhiteSpace(gameDirectory)) return string.Empty;
        var directSaves = Path.Combine(gameDirectory, "SAVES");
        if (Directory.Exists(directSaves)) return directSaves;
        var lowerSaves = Path.Combine(gameDirectory, "saves");
        if (Directory.Exists(lowerSaves)) return lowerSaves;
        return directSaves;
    }

    /// <summary>
    /// 定位指定槽位的存档物理文件路径（自动兼容 SANGO01.SAV 与 SANGO00.SAV 两种索引）
    /// </summary>
    public static string ResolveSlotFilePath(string savesDir, int slot)
    {
        if (string.IsNullOrWhiteSpace(savesDir)) return string.Empty;

        // 优先 1-based (SANGO01.SAV)
        var p1 = Path.Combine(savesDir, $"SANGO{slot:D2}.SAV");
        if (File.Exists(p1)) return p1;

        // 兼容 0-based (SANGO00.SAV)
        var p0 = Path.Combine(savesDir, $"SANGO{slot - 1:D2}.SAV");
        if (File.Exists(p0)) return p0;

        return p1;
    }

    /// <summary>
    /// 获取指定君主势力的代表色
    /// </summary>
    public static string GetMonarchColor(string monarchName)
    {
        if (string.IsNullOrWhiteSpace(monarchName)) return "#8D6E63";
        var name = AvatarResolver.ToSimplified(monarchName.Trim());

        if (name.Contains("刘备")) return "#2E7D32";      // 翠绿
        if (name.Contains("曹操")) return "#C62828";      // 赤红
        if (name.Contains("孙策") || name.Contains("孙坚") || name.Contains("孙权")) return "#1565C0"; // 靛蓝
        if (name.Contains("袁绍")) return "#F9A825";      // 帝金黄
        if (name.Contains("董卓")) return "#4A148C";      // 幽紫黑
        if (name.Contains("吕布")) return "#E64A19";      // 烈炎红
        if (name.Contains("马腾")) return "#795548";      // 塞外褐
        if (name.Contains("公孙瓒")) return "#78909C";    // 白马银灰
        if (name.Contains("袁术")) return "#EF6C00";      // 僭号橙
        if (name.Contains("刘表")) return "#00838F";      // 荆襄碧
        if (name.Contains("刘璋")) return "#558B2F";      // 蜀中墨绿
        if (name.Contains("孟获")) return "#4E342E";      // 南蛮赭

        return "#8D6E63"; // 诸侯古铜色
    }

    /// <summary>
    /// 扫描全量 1 ~ 10 存档槽位元信息
    /// </summary>
    public IReadOnlyList<SaveSlotSummary> ScanAllSlots(string gameDirectory)
    {
        var savesDir = GetSavesDirectory(gameDirectory);
        var list = new List<SaveSlotSummary>();

        for (int i = 1; i <= 10; i++)
        {
            var filePath = ResolveSlotFilePath(savesDir, i);
            var summary = ReadSlotSummary(filePath, i);
            list.Add(summary);
        }

        return list;
    }

    /// <summary>
    /// 安全带防抖重试读取单个槽位文件简述
    /// </summary>
    public SaveSlotSummary ReadSlotSummary(string filePath, int slot)
    {
        if (!File.Exists(filePath))
        {
            return new SaveSlotSummary
            {
                SlotNumber = slot,
                FilePath = filePath,
                Exists = false,
                IsEmpty = true,
                SaveTitle = "＜空 记 录＞",
                MonarchName = "空置",
                FactionColor = "#5C5C5C",
                GameYear = 0,
                CityCount = 0,
                GeneralCount = 0,
                LastModifiedTime = null
            };
        }

        try
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length < 0x80)
            {
                return new SaveSlotSummary
                {
                    SlotNumber = slot,
                    FilePath = filePath,
                    Exists = true,
                    IsEmpty = true,
                    SaveTitle = "＜空 记 录＞",
                    MonarchName = "空置",
                    FactionColor = "#5C5C5C",
                    GameYear = 0,
                    CityCount = 0,
                    GeneralCount = 0,
                    LastModifiedTime = fileInfo.LastWriteTime
                };
            }

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var br = new BinaryReader(fs);

            // 读取头 4 字节
            var magic = new string(br.ReadChars(4));
            br.ReadBytes(8); // version + empty

            var titleBytes = br.ReadBytes(0x40);
            var rawTitle = Big5Encoding.GetString(titleBytes).Trim('\0', ' ');
            var simpTitle = AvatarResolver.ToSimplified(rawTitle);

            if (string.IsNullOrWhiteSpace(rawTitle) || rawTitle.Contains("空 記 錄") || rawTitle.Contains("空 记录"))
            {
                return new SaveSlotSummary
                {
                    SlotNumber = slot,
                    FilePath = filePath,
                    Exists = true,
                    IsEmpty = true,
                    SaveTitle = "＜空 记 录＞",
                    MonarchName = "空置",
                    FactionColor = "#5C5C5C",
                    GameYear = 0,
                    CityCount = 0,
                    GeneralCount = 0,
                    LastModifiedTime = fileInfo.LastWriteTime
                };
            }

            br.ReadBytes(4); // unknown
            uint monarchIndex = br.ReadUInt32();
            uint rogueIndex = br.ReadUInt32();
            uint currentYear = br.ReadUInt32();

            // 解析摘要文本中的君主、城数、武将数 (例如: "刘备   城数05 武将024 时间208年")
            string monarch = "诸侯";
            int cityCount = 0;
            int generalCount = 0;
            int year = (int)currentYear;

            ParseTitleDetails(simpTitle, out monarch, out cityCount, out generalCount, out var parsedYear);
            if (year <= 0) year = parsedYear;

            string period = year switch
            {
                <= 189 => "黄巾之乱",
                <= 193 => "讨伐董卓",
                <= 207 => "群雄割据",
                <= 219 => "赤壁之战",
                _ => "三国鼎立"
            };

            return new SaveSlotSummary
            {
                SlotNumber = slot,
                FilePath = filePath,
                Exists = true,
                IsEmpty = false,
                SaveTitle = simpTitle,
                MonarchName = monarch,
                FactionColor = GetMonarchColor(monarch),
                GameYear = year,
                CityCount = cityCount,
                GeneralCount = generalCount,
                ScenarioPeriodName = $"{period} ({year}年)",
                LastModifiedTime = fileInfo.LastWriteTime
            };
        }
        catch
        {
            return new SaveSlotSummary
            {
                SlotNumber = slot,
                FilePath = filePath,
                Exists = true,
                IsEmpty = true,
                SaveTitle = "＜读取中断＞",
                MonarchName = "异常",
                FactionColor = "#5C5C5C",
                GameYear = 0,
                CityCount = 0,
                GeneralCount = 0,
                LastModifiedTime = DateTime.Now
            };
        }
    }

    private static void ParseTitleDetails(string title, out string monarch, out int cityCount, out int generalCount, out int year)
    {
        monarch = "诸侯";
        cityCount = 0;
        generalCount = 0;
        year = 190;

        try
        {
            // 典型结构: "%-6s 城数%02d 武将%03d 时间%3d年"
            var parts = title.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                monarch = parts[0];
            }

            foreach (var part in parts)
            {
                if (part.Contains("城数"))
                {
                    var digits = new string(part.Where(char.IsDigit).ToArray());
                    int.TryParse(digits, out cityCount);
                }
                else if (part.Contains("武将"))
                {
                    var digits = new string(part.Where(char.IsDigit).ToArray());
                    int.TryParse(digits, out generalCount);
                }
                else if (part.Contains("年") || part.Contains("时间"))
                {
                    var digits = new string(part.Where(char.IsDigit).ToArray());
                    int.TryParse(digits, out year);
                }
            }
        }
        catch { }
    }

    public static string ExtractMonarchFromSummary(string title)
    {
        ParseTitleDetails(title, out var monarch, out _, out _, out _);
        return monarch;
    }

    public static int ExtractCityCountFromSummary(string title)
    {
        ParseTitleDetails(title, out _, out var city, out _, out _);
        return city;
    }

    public static int ExtractGeneralCountFromSummary(string title)
    {
        ParseTitleDetails(title, out _, out _, out var gen, out _);
        return gen;
    }

    public static int ExtractYearFromSummary(string title)
    {
        ParseTitleDetails(title, out _, out _, out _, out var year);
        return year;
    }

    /// <summary>
    /// 防抖等待文件写入解锁并解析完整快照
    /// </summary>
    public async Task<SaveGameSnapshot?> ReadSnapshotWithDebounceAsync(string filePath, int slot, CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) return null;

        // 核心防抖与互斥检测：重试轮询直到游戏进程完成写操作并完全释放文件句柄
        bool ready = false;
        long lastSize = -1;

        for (int i = 0; i < 8; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var fi = new FileInfo(filePath);
                if (fi.Length > 0 && fi.Length == lastSize)
                {
                    using var testStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    ready = true;
                    break;
                }
                lastSize = fi.Length;
            }
            catch (IOException)
            {
                // 文件正被 Sango2 独占写入中，继续退避等待
            }

            await Task.Delay(250, ct).ConfigureAwait(false);
        }

        if (!ready) return null;

        return ReadSnapshot(filePath, slot);
    }

    /// <summary>
    /// 解析存档全量武将与城池快照
    /// </summary>
    public SaveGameSnapshot ReadSnapshot(string filePath, int slot)
    {
        var summary = ReadSlotSummary(filePath, slot);
        if (summary.IsEmpty)
        {
            return new SaveGameSnapshot
            {
                SlotNumber = slot,
                FilePath = filePath,
                SaveTitle = summary.SaveTitle,
                MonarchName = summary.MonarchName,
                FactionColor = summary.FactionColor,
                GameYear = summary.GameYear,
                ScenarioPeriodName = summary.ScenarioPeriodName,
                Generals = Array.Empty<LiveGeneralRecord>(),
                Cities = Array.Empty<LiveCityRecord>()
            };
        }

        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var br = new BinaryReader(fs);

            fs.Seek(0x5C, SeekOrigin.Begin);
            uint currentYear = br.ReadUInt32();

            fs.Seek(0x6C, SeekOrigin.Begin);
            uint monarchNumMax = br.ReadUInt32();
            uint monarchFileOffset = br.ReadUInt32();
            br.ReadUInt32();
            br.ReadUInt32();
            uint monarchTailIndex = br.ReadUInt32();

            uint cityNumMax = br.ReadUInt32();
            uint cityFileOffset = br.ReadUInt32();
            br.ReadUInt32();
            br.ReadUInt32();
            uint cityTailIndex = br.ReadUInt32();

            // 跳过 city_index_inlist
            fs.Seek(cityNumMax * 4, SeekOrigin.Current);

            uint troopNumMax = br.ReadUInt32();
            uint troopFileOffset = br.ReadUInt32();
            br.ReadUInt32();
            br.ReadUInt32();
            br.ReadUInt32();

            uint generalsNumMax = br.ReadUInt32();
            uint generalsFileOffset = br.ReadUInt32();
            br.ReadUInt32();
            br.ReadUInt32();
            uint generalsTailIndex = br.ReadUInt32();

            // 1. 读取君主表
            var monarchNames = new Dictionary<int, string>();
            if (monarchFileOffset > 0 && monarchFileOffset < fs.Length)
            {
                fs.Seek(monarchFileOffset, SeekOrigin.Begin);
                for (int i = 0; i < Math.Min(monarchNumMax, 40); i++)
                {
                    if (fs.Position + 0x10 > fs.Length) break;
                    uint mSize = br.ReadUInt32();
                    var nameBytes = br.ReadBytes(0x0C);
                    var mName = AvatarResolver.ToSimplified(Big5Encoding.GetString(nameBytes).Trim('\0', ' '));
                    if (!string.IsNullOrEmpty(mName)) monarchNames[i] = mName;
                    if (mSize > 0x10) fs.Seek(mSize - 0x10, SeekOrigin.Current);
                }
            }

            // 2. 读取城池表
            var cityList = new List<LiveCityRecord>();
            var cityNames = new Dictionary<int, string>();

            if (cityFileOffset > 0 && cityFileOffset < fs.Length)
            {
                fs.Seek(cityFileOffset, SeekOrigin.Begin);
                for (int i = 0; i < Math.Min(cityNumMax, 70); i++)
                {
                    if (fs.Position + 0x20 > fs.Length) break;
                    long entryStart = fs.Position;
                    uint cSize = br.ReadUInt32();
                    uint cUsed = br.ReadUInt32();
                    var cNameBytes = br.ReadBytes(0x0C);
                    var cName = AvatarResolver.ToSimplified(Big5Encoding.GetString(cNameBytes).Trim('\0', ' '));

                    if (string.IsNullOrEmpty(cName))
                    {
                        var staticCity = _repository.GetCityById(i + 1);
                        cName = staticCity?.Name ?? $"城池_{i + 1}";
                    }
                    cityNames[i] = cName;

                    int defense = 500;
                    int reserve = 200;
                    int lordId = 0;

                    // 移动到城防与人口段
                    if (cSize >= 0x60 && entryStart + 0x60 <= fs.Length)
                    {
                        try
                        {
                            fs.Seek(entryStart + 0x48, SeekOrigin.Begin);
                            reserve = (int)br.ReadUInt32();
                            br.ReadUInt32(); // people
                            defense = (int)br.ReadUInt32();
                            br.ReadUInt32(); // money
                            br.ReadBytes(12);
                            lordId = (int)br.ReadUInt32();
                        }
                        catch { }
                    }

                    string lordName = monarchNames.GetValueOrDefault(lordId, "在野/中立");

                    cityList.Add(new LiveCityRecord
                    {
                        CityId = i + 1,
                        Name = cName,
                        LordId = lordId,
                        LordName = lordName,
                        Defense = Math.Max(0, defense),
                        ReserveTroops = Math.Max(0, reserve),
                        GeneralCount = 0,
                        UndergroundItemId = 0
                    });

                    if (cSize > 0 && entryStart + cSize <= fs.Length)
                    {
                        fs.Seek(entryStart + cSize, SeekOrigin.Begin);
                    }
                }
            }

            // 3. 读取全量武将表
            var generalList = new List<LiveGeneralRecord>();
            var staticGenerals = _repository.GetAllGenerals();

            if (generalsFileOffset > 0 && generalsFileOffset < fs.Length)
            {
                fs.Seek(generalsFileOffset, SeekOrigin.Begin);
                for (int i = 0; i < Math.Min(generalsNumMax, 420); i++)
                {
                    if (fs.Position + 0x40 > fs.Length) break;
                    long entryStart = fs.Position;
                    uint gSize = br.ReadUInt32();
                    var gNameBytes = br.ReadBytes(0x0C);
                    var gName = AvatarResolver.ToSimplified(Big5Encoding.GetString(gNameBytes).Trim('\0', ' '));

                    var staticInfo = i < staticGenerals.Count ? staticGenerals[i] : null;
                    if (string.IsNullOrEmpty(gName))
                    {
                        gName = staticInfo?.Name ?? $"武将_{i + 1}";
                    }

                    ushort hpMax = 100, hpCur = 100, mpMax = 50, mpCur = 50;
                    ushort level = 1, force = staticInfo?.BaseForce ?? 70, intel = staticInfo?.BaseIntellect ?? 70;
                    ushort loyalty = 100, troopCur = 0, troopMax = 50;
                    uint status = 0, cityIndex = 0, monarchIndexEntry = 0;

                    try
                    {
                        fs.Seek(entryStart + 0x30, SeekOrigin.Begin);
                        hpMax = br.ReadUInt16();
                        hpCur = br.ReadUInt16();
                        mpMax = br.ReadUInt16();
                        mpCur = br.ReadUInt16();

                        fs.Seek(entryStart + 0x4E, SeekOrigin.Begin);
                        uint exp = br.ReadUInt32();
                        level = br.ReadUInt16();
                        force = br.ReadUInt16();
                        intel = br.ReadUInt16();
                        br.ReadUInt16(); // current force
                        br.ReadUInt16(); // current intel
                        br.ReadUInt16(); // morale
                        br.ReadUInt16(); // fatigue
                        loyalty = br.ReadUInt16();

                        fs.Seek(entryStart + 0x64, SeekOrigin.Begin);
                        troopMax = br.ReadUInt16();
                        troopCur = br.ReadUInt16();

                        fs.Seek(entryStart + 0x88, SeekOrigin.Begin);
                        status = br.ReadUInt32();

                        fs.Seek(entryStart + 0xDC, SeekOrigin.Begin);
                        cityIndex = br.ReadUInt32();
                        monarchIndexEntry = br.ReadUInt32();
                    }
                    catch { }

                    string cityName = cityNames.GetValueOrDefault((int)cityIndex, "巡行天下");
                    string lordName = monarchNames.GetValueOrDefault((int)monarchIndexEntry, summary.MonarchName);

                    GeneralStatus gStatus = status switch
                    {
                        1 => GeneralStatus.Captured,
                        2 => GeneralStatus.Wild,
                        3 => GeneralStatus.Dead,
                        4 => GeneralStatus.Wandering,
                        _ => GeneralStatus.Active
                    };

                    generalList.Add(new LiveGeneralRecord
                    {
                        GeneralId = i + 1,
                        Name = gName,
                        Force = (byte)Math.Clamp((int)force, 1, 120),
                        Intellect = (byte)Math.Clamp((int)intel, 1, 120),
                        CurrentHp = Math.Max(0, (int)hpCur),
                        MaxHp = Math.Max(1, (int)hpMax),
                        CurrentMp = Math.Max(0, (int)mpCur),
                        MaxMp = Math.Max(1, (int)mpMax),
                        TroopCount = Math.Max(0, (int)troopCur),
                        MaxTroopCount = Math.Max(0, (int)troopMax),
                        Loyalty = (byte)Math.Clamp((int)loyalty, 0, 100),
                        Status = gStatus,
                        LordId = (int)monarchIndexEntry,
                        LordName = lordName,
                        CityId = (int)cityIndex + 1,
                        CityName = cityName,
                        Level = (byte)Math.Clamp((int)level, 1, 99)
                    });

                    if (gSize > 0 && entryStart + gSize <= fs.Length)
                    {
                        fs.Seek(entryStart + gSize, SeekOrigin.Begin);
                    }
                }
            }

            // 回退补齐：若存档中武将数较少，自动结合静态库 400 武将对齐
            if (generalList.Count < 400)
            {
                for (int i = generalList.Count; i < staticGenerals.Count; i++)
                {
                    var sg = staticGenerals[i];
                    generalList.Add(new LiveGeneralRecord
                    {
                        GeneralId = sg.Id,
                        Name = sg.Name,
                        Force = sg.BaseForce,
                        Intellect = sg.BaseIntellect,
                        CurrentHp = 100,
                        MaxHp = 100,
                        CurrentMp = 50,
                        MaxMp = 50,
                        TroopCount = 0,
                        MaxTroopCount = 50,
                        Loyalty = 75,
                        Status = GeneralStatus.Active,
                        LordId = 0,
                        LordName = summary.MonarchName,
                        CityId = 1,
                        CityName = "未出仕",
                        Level = 1
                    });
                }
            }

            return new SaveGameSnapshot
            {
                SlotNumber = slot,
                FilePath = filePath,
                SaveTitle = summary.SaveTitle,
                MonarchName = summary.MonarchName,
                FactionColor = summary.FactionColor,
                GameYear = summary.GameYear,
                ScenarioPeriodName = summary.ScenarioPeriodName,
                Generals = generalList,
                Cities = cityList.Count > 0 ? cityList : _repository.GetAllCities().Select(c => new LiveCityRecord
                {
                    CityId = c.Id,
                    Name = c.Name,
                    LordId = 1,
                    LordName = summary.MonarchName,
                    Defense = 500,
                    ReserveTroops = 200,
                    GeneralCount = 1,
                    UndergroundItemId = 0
                }).ToList()
            };
        }
        catch
        {
            // 异常兜底，返回根据静态数据初始化的安全快照
            return CreateFallbackSnapshot(summary);
        }
    }

    private SaveGameSnapshot CreateFallbackSnapshot(SaveSlotSummary summary)
    {
        var staticGenerals = _repository.GetAllGenerals();
        var generals = staticGenerals.Select(g => new LiveGeneralRecord
        {
            GeneralId = g.Id,
            Name = g.Name,
            Force = g.BaseForce,
            Intellect = g.BaseIntellect,
            CurrentHp = 100,
            MaxHp = 100,
            CurrentMp = 50,
            MaxMp = 50,
            TroopCount = 30,
            MaxTroopCount = 50,
            Loyalty = 85,
            Status = GeneralStatus.Active,
            LordId = 1,
            LordName = summary.MonarchName,
            CityId = 1,
            CityName = "许昌",
            Level = 1
        }).ToList();

        var cities = _repository.GetAllCities().Select(c => new LiveCityRecord
        {
            CityId = c.Id,
            Name = c.Name,
            LordId = 1,
            LordName = summary.MonarchName,
            Defense = 600,
            ReserveTroops = 300,
            GeneralCount = 2,
            UndergroundItemId = 0
        }).ToList();

        return new SaveGameSnapshot
        {
            SlotNumber = summary.SlotNumber,
            FilePath = summary.FilePath,
            SaveTitle = summary.SaveTitle,
            MonarchName = summary.MonarchName,
            FactionColor = summary.FactionColor,
            GameYear = summary.GameYear,
            ScenarioPeriodName = summary.ScenarioPeriodName,
            Generals = generals,
            Cities = cities
        };
    }
}
