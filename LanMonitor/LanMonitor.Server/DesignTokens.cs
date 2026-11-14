using Microsoft.Win32;

namespace LanMonitor.Server;

public enum ThemeMode { Light = 0, Dark = 1, System = 2 }

/// <summary>
/// Hệ thống màu ngữ nghĩa có thể đổi giữa Sáng / Tối lúc đang chạy.
/// Mọi control tự vẽ đều đọc màu từ đây khi Paint, nên chỉ cần gọi Apply() rồi Invalidate().
/// </summary>
public static class DesignTokens
{
    // Bảng màu nhấn (Light = dùng trên nền sáng, Dark = biến thể sáng hơn cho nền tối)
    public static readonly (string Name, Color Light, Color Dark)[] AccentPresets =
    {
        ("Xanh dương",   Hex(0x2563EB), Hex(0x60A5FA)),
        ("Ngọc lục bảo", Hex(0x059669), Hex(0x34D399)),
        ("Tím",          Hex(0x7C3AED), Hex(0xA78BFA)),
        ("Cam",          Hex(0xEA580C), Hex(0xFB923C)),
        ("Hồng",         Hex(0xDB2777), Hex(0xF472B6)),
    };

    public static bool IsDark { get; private set; }

    // Bề mặt
    public static Color SurfaceCanvas { get; private set; }
    public static Color SurfaceElevated { get; private set; }
    public static Color SurfaceSubtle { get; private set; }
    public static Color SurfaceHeader { get; private set; }
    public static Color SurfaceHeaderSecondary { get; private set; }
    public static Color SurfaceTerminal { get; private set; }

    // Viền
    public static Color BorderSubtle { get; private set; }
    public static Color BorderStrong { get; private set; }
    public static Color BorderDark { get; private set; }

    // Chữ
    public static Color TextPrimary { get; private set; }
    public static Color TextSecondary { get; private set; }
    public static Color TextMuted { get; private set; }
    public static Color TextOnDark { get; } = Hex(0xF8FAFC);
    public static Color TextOnDarkMuted { get; } = Hex(0x94A3B8);

    // Màu nhấn
    public static Color AccentPrimary { get; private set; }
    public static Color AccentForeground { get; private set; }
    public static Color AccentSelectionBg { get; private set; }

    // Trạng thái
    public static Color StatusOnlineDot { get; private set; }
    public static Color StatusOnlineBg { get; private set; }
    public static Color StatusOnlineText { get; private set; }
    public static Color StatusWarningBar { get; private set; }
    public static Color StatusWarningBg { get; private set; }
    public static Color StatusWarningText { get; private set; }
    public static Color StatusDangerBar { get; private set; }
    public static Color StatusDangerBg { get; private set; }
    public static Color StatusDangerRowBg { get; private set; }
    public static Color StatusDangerText { get; private set; }
    public static Color StatusOfflineDot { get; private set; }
    public static Color StatusOfflineBg { get; private set; }
    public static Color StatusOfflineText { get; private set; }
    public static Color OfflineRowBg { get; private set; }

    // Thành phần
    public static Color ChartRam { get; private set; }
    public static Color MeterTrack { get; private set; }
    public static Color SegmentTrack { get; private set; }
    public static Color SegmentSelected { get; private set; }
    public static Color ToggleOff { get; private set; }

    // Chữ: một họ font cho giao diện + font đơn cách cho số liệu
    public static readonly Font FontHeading = new("Segoe UI", 12.5F, FontStyle.Bold);
    public static readonly Font FontSection = new("Segoe UI", 9.5F, FontStyle.Bold);
    public static readonly Font FontBody = new("Segoe UI", 9.25F, FontStyle.Regular);
    public static readonly Font FontBodyStrong = new("Segoe UI", 9.25F, FontStyle.Bold);
    public static readonly Font FontLabel = new("Segoe UI", 8.5F, FontStyle.Regular);
    public static readonly Font FontBadge = new("Segoe UI", 8.5F, FontStyle.Bold);
    public static readonly Font FontTabular = new("Consolas", 9.5F, FontStyle.Regular);
    public static readonly Font FontTabularBold = new("Consolas", 9.5F, FontStyle.Bold);

    static DesignTokens() => Apply(false, 0);

    public static void Apply(bool dark, int accentIndex)
    {
        IsDark = dark;
        accentIndex = Math.Clamp(accentIndex, 0, AccentPresets.Length - 1);
        var preset = AccentPresets[accentIndex];

        if (!dark)
        {
            SurfaceCanvas = Hex(0xF8FAFC);
            SurfaceElevated = Hex(0xFFFFFF);
            SurfaceSubtle = Hex(0xF1F5F9);
            SurfaceHeader = Hex(0x0F172A);
            SurfaceHeaderSecondary = Hex(0x1E293B);
            SurfaceTerminal = Hex(0x0F172A);

            BorderSubtle = Hex(0xE2E8F0);
            BorderStrong = Hex(0xCBD5E1);
            BorderDark = Hex(0x334155);

            TextPrimary = Hex(0x0F172A);
            TextSecondary = Hex(0x475569);
            TextMuted = Hex(0x64748B);

            AccentPrimary = preset.Light;
            AccentForeground = Color.White;

            StatusOnlineDot = Hex(0x16A34A);
            StatusOnlineBg = Hex(0xDCFCE7);
            StatusOnlineText = Hex(0x166534);
            StatusWarningBar = Hex(0xD97706);
            StatusWarningBg = Hex(0xFEF3C7);
            StatusWarningText = Hex(0x92400E);
            StatusDangerBar = Hex(0xDC2626);
            StatusDangerBg = Hex(0xFEE2E2);
            StatusDangerRowBg = Hex(0xFEF2F2);
            StatusDangerText = Hex(0x991B1B);
            StatusOfflineDot = Hex(0x94A3B8);
            StatusOfflineBg = Hex(0xE2E8F0);
            StatusOfflineText = Hex(0x475569);
            OfflineRowBg = Hex(0xF8FAFC);

            ChartRam = accentIndex == 1 ? Hex(0x7C3AED) : Hex(0x059669);
            MeterTrack = Hex(0xE2E8F0);
            SegmentTrack = Hex(0xE2E8F0);
            SegmentSelected = Hex(0xFFFFFF);
            ToggleOff = Hex(0xCBD5E1);
        }
        else
        {
            SurfaceCanvas = Hex(0x0B1120);
            SurfaceElevated = Hex(0x111827);
            SurfaceSubtle = Hex(0x172033);
            SurfaceHeader = Hex(0x020617);
            SurfaceHeaderSecondary = Hex(0x1E293B);
            SurfaceTerminal = Hex(0x020617);

            BorderSubtle = Hex(0x1F2937);
            BorderStrong = Hex(0x334155);
            BorderDark = Hex(0x334155);

            TextPrimary = Hex(0xF1F5F9);
            TextSecondary = Hex(0xCBD5E1);
            TextMuted = Hex(0x94A3B8);

            AccentPrimary = preset.Dark;
            AccentForeground = Hex(0x0F172A);

            StatusOnlineDot = Hex(0x22C55E);
            StatusOnlineBg = Hex(0x0F2E1D);
            StatusOnlineText = Hex(0x86EFAC);
            StatusWarningBar = Hex(0xF59E0B);
            StatusWarningBg = Hex(0x3A2A0A);
            StatusWarningText = Hex(0xFCD34D);
            StatusDangerBar = Hex(0xEF4444);
            StatusDangerBg = Hex(0x3F1518);
            StatusDangerRowBg = Hex(0x221318);
            StatusDangerText = Hex(0xFCA5A5);
            StatusOfflineDot = Hex(0x64748B);
            StatusOfflineBg = Hex(0x1E293B);
            StatusOfflineText = Hex(0x94A3B8);
            OfflineRowBg = Hex(0x0D1422);

            ChartRam = accentIndex == 1 ? Hex(0xA78BFA) : Hex(0x34D399);
            MeterTrack = Hex(0x273244);
            SegmentTrack = Hex(0x0B1120);
            SegmentSelected = Hex(0x1E293B);
            ToggleOff = Hex(0x475569);
        }

        AccentSelectionBg = Blend(SurfaceElevated, AccentPrimary, dark ? 0.20 : 0.10);
    }

    public static bool Resolve(ThemeMode mode) =>
        mode == ThemeMode.Dark || (mode == ThemeMode.System && IsWindowsDark());

    // Đọc cài đặt "Choose your app mode" của Windows
    public static bool IsWindowsDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
        }
        catch
        {
            return false;
        }
    }

    public static Color Blend(Color from, Color to, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromArgb(
            (int)Math.Round(from.R + (to.R - from.R) * t),
            (int)Math.Round(from.G + (to.G - from.G) * t),
            (int)Math.Round(from.B + (to.B - from.B) * t));
    }

    private static Color Hex(int rgb) =>
        Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
}
