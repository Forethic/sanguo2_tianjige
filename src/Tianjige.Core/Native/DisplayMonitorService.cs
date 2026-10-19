using System.Runtime.InteropServices;
using Tianjige.Core.Models;

namespace Tianjige.Core.Native;

/// <summary>
/// Windows 多显示器枚举服务
/// </summary>
public static class DisplayMonitorService
{
    private delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, ref RECT lprcMonitor, nint dwData);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    private const uint MONITORINFOF_PRIMARY = 0x00000001;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFOEX lpmi);

    /// <summary>
    /// 枚举当前系统中的所有物理/逻辑显示器拓扑
    /// </summary>
    public static IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();
        int index = 1;

        MonitorEnumProc callback = (nint hMon, nint _, ref RECT _, nint _) =>
        {
            var mi = new MONITORINFOEX();
            mi.cbSize = Marshal.SizeOf<MONITORINFOEX>();

            if (GetMonitorInfo(hMon, ref mi))
            {
                bool isPrimary = (mi.dwFlags & MONITORINFOF_PRIMARY) != 0;
                int width = mi.rcMonitor.Right - mi.rcMonitor.Left;
                int height = mi.rcMonitor.Bottom - mi.rcMonitor.Top;

                var title = isPrimary
                    ? $"屏幕 {index} (主屏 · {width}×{height})"
                    : $"屏幕 {index} (副屏 · {width}×{height})";

                monitors.Add(new MonitorInfo
                {
                    DeviceName = mi.szDevice ?? $"DISPLAY{index}",
                    DisplayTitle = title,
                    Index = index,
                    IsPrimary = isPrimary,
                    Left = mi.rcMonitor.Left,
                    Top = mi.rcMonitor.Top,
                    Width = width,
                    Height = height
                });
                index++;
            }
            return true;
        };

        try
        {
            EnumDisplayMonitors(0, 0, callback, 0);
        }
        catch
        {
            // 防御性异常兜底，防止非 Windows 或测试管道崩溃
        }

        // 如果在非交互环境或未枚举到任何屏幕，提供标准 1080p 安全回退
        if (monitors.Count == 0)
        {
            monitors.Add(new MonitorInfo
            {
                DeviceName = "\\\\.\\DISPLAY1",
                DisplayTitle = "屏幕 1 (默认主屏 · 1920×1080)",
                Index = 1,
                IsPrimary = true,
                Left = 0,
                Top = 0,
                Width = 1920,
                Height = 1080
            });
        }

        return monitors;
    }
}
