namespace LanMonitor.Server;

public class AgentInfo
{
    public const int MaxHistoryPoints = 30;

    public string Key { get; set; } = "";
    public string Name { get; set; } = "Unknown";
    public string Ip { get; set; } = "unknown";
    public string Os { get; set; } = "";
    public int Cpu { get; set; }
    public int Ram { get; set; }
    public int Disk { get; set; }
    public bool Online { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.Now;
    public DateTime LastSeen { get; set; } = DateTime.Now;

    // Thống kê hiệu năng trong phiên để phục vụ So sánh giữa 2 máy
    public int PeakCpu { get; private set; }
    public int PeakRam { get; private set; }
    public long TotalCpuSamples { get; private set; }
    public long TotalRamSamples { get; private set; }
    public int SampleCount { get; private set; }
    public long? LastPingMs { get; set; }

    public int AvgCpu => SampleCount > 0 ? (int)Math.Round((double)TotalCpuSamples / SampleCount) : Cpu;
    public int AvgRam => SampleCount > 0 ? (int)Math.Round((double)TotalRamSamples / SampleCount) : Ram;

    // Lịch sử mẫu đo gần nhất để vẽ đồ thị xu hướng thời gian thực
    public List<int> CpuHistory { get; } = new();
    public List<int> RamHistory { get; } = new();

    // Lưu trạng thái cảnh báo lần trước để tránh ghi log lặp lại liên tục mỗi chu kỳ
    public bool WasAlerting { get; set; }

    public void RecordSample(int cpu, int ram, int disk)
    {
        Cpu = Math.Clamp(cpu, 0, 100);
        Ram = Math.Clamp(ram, 0, 100);
        Disk = Math.Clamp(disk, 0, 100);

        if (Cpu > PeakCpu) PeakCpu = Cpu;
        if (Ram > PeakRam) PeakRam = Ram;
        TotalCpuSamples += Cpu;
        TotalRamSamples += Ram;
        SampleCount++;

        lock (CpuHistory)
        {
            CpuHistory.Add(Cpu);
            if (CpuHistory.Count > MaxHistoryPoints)
                CpuHistory.RemoveAt(0);

            RamHistory.Add(Ram);
            if (RamHistory.Count > MaxHistoryPoints)
                RamHistory.RemoveAt(0);
        }
    }

    public string GetAlertText(int cpuLimit, int ramLimit, int diskLimit)
    {
        if (!Online) return "Ngắt kết nối (Offline)";

        var alerts = new List<string>();
        if (Cpu >= cpuLimit) alerts.Add($"CPU {Cpu}%");
        if (Ram >= ramLimit) alerts.Add($"RAM {Ram}%");
        if (Disk >= diskLimit) alerts.Add($"DISK {Disk}%");

        return alerts.Count > 0
            ? "Vượt ngưỡng: " + string.Join(" · ", alerts)
            : "Hoạt động ổn định";
    }

    public bool IsAlerting(int cpuLimit, int ramLimit, int diskLimit) =>
        Online && (Cpu >= cpuLimit || Ram >= ramLimit || Disk >= diskLimit);
}
