using System.Runtime.InteropServices;

namespace Tianjige.Core.Native;

/// <summary>
/// Win32 底层 P/Invoke 原生互操作接口
/// </summary>
public static class NativeMethods
{
    public const uint PROCESS_VM_READ = 0x0010;
    public const uint PROCESS_QUERY_INFORMATION = 0x0400;

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(nint hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReadProcessMemory(
        nint hProcess,
        nint lpBaseAddress,
        [Out] byte[] lpBuffer,
        int dwSize,
        out nint lpNumberOfBytesRead);

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_BASIC_INFORMATION
    {
        public nint BaseAddress;
        public nint AllocationBase;
        public uint AllocationProtect;
        public nint RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint VirtualQueryEx(
        nint hProcess,
        nint lpAddress,
        out MEMORY_BASIC_INFORMATION lpBuffer,
        uint dwLength);
}
