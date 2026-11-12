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
    public DateTime LastSeen { get; set; } = DateTime.Now;

    // Lịch sử mẫu đo gần nhất để vẽ đồ thị xu hướng thời gian thực khi chọn máy
    public List<int> CpuHistory { get; } = new();
    public List<int> RamHistory { get; } = new();

    // Lưu trạng thái cảnh báo lần trước để tránh ghi log lặp lại liên tục mỗi chu kỳ
    public bool WasAlerting { get; set; }

    public void RecordSample(int cpu, int ram, int disk)
    {
        Cpu = Math.Clamp(cpu, 0, 100);
        Ram = Math.Clamp(ram, 0, 100);
        Disk = Math.Clamp(disk, 0, 100);

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

    // Kiểm tra xem có thông số nào vượt ngưỡng không
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
