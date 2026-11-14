using System.Text.Json;

namespace LanMonitor.Server;

/// <summary>
/// Lưu trữ cấu hình người dùng ra file JSON tại %AppData%\LanMonitor\settings.json
/// để tự động khôi phục theme, màu nhấn và ngưỡng cảnh báo ở lần mở sau.
/// </summary>
public class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LanMonitor",
        "settings.json");

    public ThemeMode Theme { get; set; } = ThemeMode.Light;
    public int AccentIndex { get; set; } = 0;

    public int CpuLimit { get; set; } = 80;
    public int RamLimit { get; set; } = 85;
    public int DiskLimit { get; set; } = 90;
    public int ReportIntervalSeconds { get; set; } = 3;
    public int OfflineTimeoutMultiplier { get; set; } = 3;

    public bool NotifyOnThreshold { get; set; } = true;
    public bool NotifyOnOffline { get; set; } = true;
    public bool SoundAlert { get; set; } = false;
    public bool CompactRows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = false;
    public bool ShowThresholdLineOnChart { get; set; } = true;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    loaded.CpuLimit = Math.Clamp(loaded.CpuLimit, 10, 100);
                    loaded.RamLimit = Math.Clamp(loaded.RamLimit, 10, 100);
                    loaded.DiskLimit = Math.Clamp(loaded.DiskLimit, 10, 100);
                    loaded.ReportIntervalSeconds = Math.Clamp(loaded.ReportIntervalSeconds, 1, 60);
                    loaded.OfflineTimeoutMultiplier = Math.Clamp(loaded.OfflineTimeoutMultiplier, 2, 10);
                    loaded.AccentIndex = Math.Clamp(loaded.AccentIndex, 0, DesignTokens.AccentPresets.Length - 1);
                    return loaded;
                }
            }
        }
        catch { }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string? dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }

    public void ResetToDefaults()
    {
        Theme = ThemeMode.Light;
        AccentIndex = 0;
        CpuLimit = 80;
        RamLimit = 85;
        DiskLimit = 90;
        ReportIntervalSeconds = 3;
        OfflineTimeoutMultiplier = 3;
        NotifyOnThreshold = true;
        NotifyOnOffline = true;
        SoundAlert = false;
        CompactRows = false;
        MinimizeToTray = false;
        ShowThresholdLineOnChart = true;
    }
}
