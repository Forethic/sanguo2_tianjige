using System.IO;
using Tianjige.Data.Repositories;

namespace Tianjige.Data.Save;

/// <summary>
/// 存档管理与实时文件变动守护监听服务
/// </summary>
public class SaveGameService : IDisposable
{
    private readonly SaveGameReader _reader;
    private FileSystemWatcher? _watcher;
    private string _gameDirectory = string.Empty;
    private int _monitoredSlot = 1;
    private System.Threading.Timer? _debounceTimer;
    private readonly object _lock = new();

    public event Action<SaveGameSnapshot>? OnSaveUpdated;
    public event Action<IReadOnlyList<SaveSlotSummary>>? OnSlotsRefreshed;

    public SaveGameService(StaticRepository? repository = null)
    {
        _reader = new SaveGameReader(repository);
    }

    public void Initialize(string gameDirectory, int activeSlot = 1)
    {
        _gameDirectory = gameDirectory;
        _monitoredSlot = activeSlot;

        SetupWatcher();
    }

    public void SetActiveSlot(int slot)
    {
        _monitoredSlot = slot;
    }

    public IReadOnlyList<SaveSlotSummary> GetSlots()
    {
        if (string.IsNullOrWhiteSpace(_gameDirectory))
        {
            return Enumerable.Range(1, 10).Select(i => new SaveSlotSummary
            {
                SlotNumber = i,
                FilePath = string.Empty,
                Exists = false,
                IsEmpty = true,
                SaveTitle = "＜空 记 录＞",
                MonarchName = "空置",
                FactionColor = "#5C5C5C",
                GameYear = 0,
                CityCount = 0,
                GeneralCount = 0
            }).ToList();
        }

        return _reader.ScanAllSlots(_gameDirectory);
    }

    public async Task<SaveGameSnapshot?> LoadSlotSnapshotAsync(int slot, CancellationToken ct = default)
    {
        _monitoredSlot = slot;
        var savesDir = SaveGameReader.GetSavesDirectory(_gameDirectory);
        var filePath = SaveGameReader.ResolveSlotFilePath(savesDir, slot);

        return await _reader.ReadSnapshotWithDebounceAsync(filePath, slot, ct);
    }

    private void SetupWatcher()
    {
        _watcher?.Dispose();
        _watcher = null;

        if (string.IsNullOrWhiteSpace(_gameDirectory) || !Directory.Exists(_gameDirectory)) return;

        var savesDir = SaveGameReader.GetSavesDirectory(_gameDirectory);
        if (!Directory.Exists(savesDir))
        {
            try { Directory.CreateDirectory(savesDir); } catch { return; }
        }

        try
        {
            _watcher = new FileSystemWatcher(savesDir)
            {
                Filter = "*.SAV",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
        }
        catch
        {
            // 防御性异常处理
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // 延时防抖：群英传2写入文件需耗费数十毫秒，等待释放文件锁后再行读取
        lock (_lock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new System.Threading.Timer(async _ =>
            {
                await HandleFileChangedAsync(e.FullPath);
            }, null, 400, Timeout.Infinite);
        }
    }

    private async Task HandleFileChangedAsync(string filePath)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            var slots = _reader.ScanAllSlots(_gameDirectory);
            OnSlotsRefreshed?.Invoke(slots);

            // 若更新的正是当前选中的槽位，触发快照热加载
            var monitoredFile = $"SANGO{_monitoredSlot:D2}.SAV";
            if (string.Equals(fileName, monitoredFile, StringComparison.OrdinalIgnoreCase))
            {
                var snapshot = await _reader.ReadSnapshotWithDebounceAsync(filePath, _monitoredSlot);
                if (snapshot != null)
                {
                    OnSaveUpdated?.Invoke(snapshot);
                }
            }
        }
        catch
        {
            // 防御性处理写入中的争用
        }
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _watcher = null;
        _debounceTimer?.Dispose();
        _debounceTimer = null;
        GC.SuppressFinalize(this);
    }
}
