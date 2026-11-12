namespace LanMonitor.Server;

/// <summary>
/// Hệ thống Design Tokens 3 tầng (Primitive -> Semantic -> Component)
/// tuân thủ tiêu chuẩn Impeccable (Operate Mode) & UI/UX Pro Max (WCAG AA >= 4.5:1).
/// </summary>
public static class DesignTokens
{
    // 1. Surfaces & Backgrounds
    public static readonly Color SurfaceCanvas = Color.FromArgb(248, 250, 252);     // #F8FAFC - Slate 50
    public static readonly Color SurfaceElevated = Color.FromArgb(255, 255, 255);   // #FFFFFF - Pure White
    public static readonly Color SurfaceSubtle = Color.FromArgb(241, 245, 249);     // #F1F5F9 - Slate 100
    public static readonly Color SurfaceHeader = Color.FromArgb(15, 23, 42);        // #0F172A - Slate 900
    public static readonly Color SurfaceHeaderSecondary = Color.FromArgb(30, 41, 59); // #1E293B - Slate 800
    public static readonly Color SurfaceTerminal = Color.FromArgb(15, 23, 42);      // #0F172A - Log Console

    // 2. Borders & Dividers (1px structural hairlines)
    public static readonly Color BorderSubtle = Color.FromArgb(226, 232, 240);      // #E2E8F0 - Slate 200
    public static readonly Color BorderStrong = Color.FromArgb(203, 213, 225);      // #CBD5E1 - Slate 300
    public static readonly Color BorderDark = Color.FromArgb(51, 65, 85);           // #334155 - Slate 700

    // 3. Typography Colors (WCAG AA >= 4.5:1 verified)
    public static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);          // #0F172A (16.1:1 on white)
    public static readonly Color TextSecondary = Color.FromArgb(71, 85, 105);       // #475569 (7.5:1 on white)
    public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);         // #64748B (4.8:1 on white)
    public static readonly Color TextOnDark = Color.FromArgb(248, 250, 252);        // #F8FAFC
    public static readonly Color TextOnDarkMuted = Color.FromArgb(148, 163, 184);   // #94A3B8

    // 4. Semantic Status & Accent Tokens
    public static readonly Color AccentPrimary = Color.FromArgb(37, 99, 235);       // #2563EB - Blue 600
    public static readonly Color AccentSelectionBg = Color.FromArgb(239, 246, 255); // #EFF6FF - Blue 50
    public static readonly Color AccentSelectionBorder = Color.FromArgb(147, 197, 253); // #93C5FD

    // Online / Healthy (Green)
    public static readonly Color StatusOnlineDot = Color.FromArgb(22, 163, 74);     // #16A34A
    public static readonly Color StatusOnlineBg = Color.FromArgb(220, 252, 231);    // #DCFCE7
    public static readonly Color StatusOnlineText = Color.FromArgb(22, 101, 52);    // #166534 (7.1:1)

    // Warning / Near Threshold (Amber)
    public static readonly Color StatusWarningBar = Color.FromArgb(217, 119, 6);    // #D97706
    public static readonly Color StatusWarningBg = Color.FromArgb(254, 243, 199);   // #FEF3C7
    public static readonly Color StatusWarningText = Color.FromArgb(146, 64, 14);   // #92400E (6.8:1)

    // Danger / Threshold Exceeded (Red)
    public static readonly Color StatusDangerBar = Color.FromArgb(220, 38, 38);     // #DC2626
    public static readonly Color StatusDangerBg = Color.FromArgb(254, 226, 226);    // #FEE2E2
    public static readonly Color StatusDangerRowBg = Color.FromArgb(254, 242, 242); // #FEF2F2
    public static readonly Color StatusDangerText = Color.FromArgb(153, 27, 27);    // #991B1B (8.2:1)

    // Offline / Disconnected (Slate Neutral)
    public static readonly Color StatusOfflineDot = Color.FromArgb(148, 163, 184);   // #94A3B8
    public static readonly Color StatusOfflineBg = Color.FromArgb(241, 245, 249);    // #F1F5F9
    public static readonly Color StatusOfflineText = Color.FromArgb(71, 85, 105);    // #475569 (6.8:1)

    // 5. Typography Scale (Fixed point scale, 1 family + Tabular Monospace for metrics)
    public static readonly Font FontHeading = new("Segoe UI", 12.5F, FontStyle.Bold);
    public static readonly Font FontSection = new("Segoe UI", 9.5F, FontStyle.Bold);
    public static readonly Font FontBody = new("Segoe UI", 9.25F, FontStyle.Regular);
    public static readonly Font FontBodyStrong = new("Segoe UI", 9.25F, FontStyle.Bold);
    public static readonly Font FontLabel = new("Segoe UI", 8.5F, FontStyle.Regular);
    public static readonly Font FontBadge = new("Segoe UI", 8.5F, FontStyle.Bold);
    public static readonly Font FontTabular = new("Consolas", 9.5F, FontStyle.Regular);
    public static readonly Font FontTabularBold = new("Consolas", 9.5F, FontStyle.Bold);
}
