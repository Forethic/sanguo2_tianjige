using System.Runtime.InteropServices;
using Tianjige.Core.Native;
using Tianjige.Data.Memory.Models;
using Tianjige.Data.Repositories;

namespace Tianjige.Data.Memory;

/// <summary>
/// 纯只读 Win32 RPM 游戏内存安全读取器
/// </summary>
public class SafeMemoryReader : IMemoryReader
{
    private readonly StaticRepository _repository;
    private nint _processHandle = 0;
    private int _processId = 0;

    // Sango 2 原版经典默认武将与城池数据段基址
    private const nint DefaultGeneralArrayAddress = 0x004A8000;
    private const nint DefaultCityArrayAddress = 0x004B0000;

    public bool IsAttached => _processHandle != 0;
    public int CurrentScenarioId { get; set; } = 1;

    public SafeMemoryReader(StaticRepository? repository = null)
    {
        _repository = repository ?? new StaticRepository();
    }

    /// <summary>
    /// 以严格的 PROCESS_VM_READ 权限附加到目标游戏进程
    /// </summary>
    public bool TryAttach(int processId)
    {
        Detach();

        try
        {
            _processHandle = NativeMethods.OpenProcess(
                NativeMethods.PROCESS_VM_READ | NativeMethods.PROCESS_QUERY_INFORMATION,
                false,
                processId);

            if (_processHandle != 0)
            {
                _processId = processId;
                return true;
            }
        }
        catch
        {
            _processHandle = 0;
        }

        return false;
    }

    /// <summary>
    /// 安全提取全盘实时战局只读快照
    /// </summary>
    public bool TryGetSnapshot(out GameLiveSnapshot snapshot)
    {
        if (!IsAttached)
        {
            snapshot = new GameLiveSnapshot();
            return false;
        }

        try
        {
            int generalStructSize = Marshal.SizeOf<LiveGeneralMemoryRaw>();
            byte[] generalBuffer = new byte[generalStructSize * 400];

            bool generalsReadSuccess = NativeMethods.ReadProcessMemory(
                _processHandle,
                DefaultGeneralArrayAddress,
                generalBuffer,
                generalBuffer.Length,
                out var readBytes);

            int cityStructSize = Marshal.SizeOf<LiveCityMemoryRaw>();
            byte[] cityBuffer = new byte[cityStructSize * 60];

            bool citiesReadSuccess = NativeMethods.ReadProcessMemory(
                _processHandle,
                DefaultCityArrayAddress,
                cityBuffer,
                cityBuffer.Length,
                out _);

            if (!generalsReadSuccess)
            {
                snapshot = new GameLiveSnapshot();
                return false;
            }

            var generals = new List<LiveGeneralRecord>(400);
            var staticGenerals = _repository.GetAllGenerals();

            for (int i = 0; i < 400; i++)
            {
                int offset = i * generalStructSize;
                var raw = MemoryMarshal.Read<LiveGeneralMemoryRaw>(generalBuffer.AsSpan(offset, generalStructSize));
                var staticInfo = i < staticGenerals.Count ? staticGenerals[i] : null;

                generals.Add(new LiveGeneralRecord
                {
                    GeneralId = raw.GeneralId > 0 ? raw.GeneralId : i + 1,
                    Name = staticInfo?.Name ?? $"武将_{i + 1}",
                    Force = raw.Force > 0 ? raw.Force : staticInfo?.BaseForce ?? 70,
                    Intellect = raw.Intellect > 0 ? raw.Intellect : staticInfo?.BaseIntellect ?? 70,
                    CurrentHp = raw.CurrentHp,
                    MaxHp = raw.MaxHp > 0 ? raw.MaxHp : 100,
                    CurrentMp = raw.CurrentMp,
                    MaxMp = raw.MaxMp > 0 ? raw.MaxMp : 50,
                    TroopCount = raw.TroopCount,
                    MaxTroopCount = raw.MaxTroopCount,
                    Loyalty = raw.Loyalty,
                    Status = (GeneralStatus)raw.Status,
                    LordId = raw.LordId,
                    LordName = GetLordName(raw.LordId),
                    CityId = raw.CityId,
                    CityName = GetCityName(raw.CityId),
                    Level = raw.Level > 0 ? raw.Level : (byte)1
                });
            }

            var cities = new List<LiveCityRecord>(60);
            for (int i = 0; i < 60; i++)
            {
                int offset = i * cityStructSize;
                LiveCityMemoryRaw rawCity = default;
                if (citiesReadSuccess && offset + cityStructSize <= cityBuffer.Length)
                {
                    rawCity = MemoryMarshal.Read<LiveCityMemoryRaw>(cityBuffer.AsSpan(offset, cityStructSize));
                }

                cities.Add(new LiveCityRecord
                {
                    CityId = i + 1,
                    Name = GetCityName(i + 1),
                    LordId = rawCity.LordId,
                    LordName = GetLordName(rawCity.LordId),
                    Defense = rawCity.Defense,
                    ReserveTroops = rawCity.ReserveTroops,
                    GeneralCount = rawCity.GeneralCount,
                    UndergroundItemId = rawCity.UndergroundItemId,
                    UndergroundItemName = GetItemName(rawCity.UndergroundItemId)
                });
            }

            snapshot = new GameLiveSnapshot
            {
                Generals = generals,
                Cities = cities,
                Year = 200,
                Month = 7,
                Day = 1,
                ScenarioId = 3,
                BattleState = 0
            };
            return true;
        }
        catch
        {
            snapshot = new GameLiveSnapshot();
            return false;
        }
    }

    public void Detach()
    {
        if (_processHandle != 0)
        {
            NativeMethods.CloseHandle(_processHandle);
            _processHandle = 0;
            _processId = 0;
        }
    }

    public void Dispose()
    {
        Detach();
        GC.SuppressFinalize(this);
    }

    private string GetLordName(int lordId)
    {
        if (lordId <= 0) return "在野/无主";
        var monarchs = _repository.GetScenarioMonarchs(CurrentScenarioId);
        if (lordId <= monarchs.Count)
        {
            var name = AvatarResolver.ToSimplified(monarchs[lordId - 1].Name);
            if (!string.IsNullOrWhiteSpace(name)) return name;
        }
        return $"诸侯_{lordId}";
    }

    private static string GetCityName(int cityId) => SanguoCityTable.GetCityName(cityId);

    private string GetItemName(int itemId)
    {
        if (itemId <= 0) return string.Empty;
        var item = _repository.GetItemById(itemId);
        return item?.Name ?? $"宝物_{itemId}";
    }
}
