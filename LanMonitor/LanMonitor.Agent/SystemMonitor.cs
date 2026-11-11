using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LanMonitor.Agent;

public class SystemMonitor : IDisposable
{
    private readonly PerformanceCounter _cpu =
        new("Processor", "% Processor Time", "_Total");

    public SystemMonitor()
    {
        _cpu.NextValue(); // lần gọi đầu luôn trả về 0, nên gọi bỏ một lần
    }

    // % CPU đang dùng
    public int GetCpu() => (int)Math.Round(_cpu.NextValue());

    // % RAM đang dùng
    public int GetRam()
    {
        var m = new MEMORYSTATUSEX();
        GlobalMemoryStatusEx(m);
        return (int)m.dwMemoryLoad;
    }

    // % ổ đĩa hệ thống (thường là C:) đã dùng
    public int GetDisk()
    {
        var d = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)!);
        double used = d.TotalSize - d.TotalFreeSpace;
        return (int)Math.Round(used / d.TotalSize * 100);
    }

    public void Dispose() => _cpu.Dispose();

    // ---- Gọi hàm của Windows để lấy % RAM ----
    [StructLayout(LayoutKind.Sequential)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
}