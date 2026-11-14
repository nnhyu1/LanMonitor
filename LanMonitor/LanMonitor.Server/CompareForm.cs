using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LanMonitor.Server;

/// <summary>
/// Cửa sổ So sánh hiệu năng trực tiếp giữa 2 máy trạm (CPU, RAM, Ổ đĩa, Đỉnh tải & Biểu đồ kép).
/// Tự động đồng bộ Light/Dark Theme theo DesignTokens.
/// </summary>
public class CompareForm : Form
{
    private readonly MonitorServer _server;
    private readonly AppSettings _settings;

    private readonly Panel _topBar;
    private readonly ModernSelector _selA = null!;
    private readonly ModernSelector _selB = null!;
    private readonly SegmentedControl _segMetric = null!;

    private readonly Panel _canvas;
    private readonly System.Windows.Forms.Timer _refreshTimer;

    private List<AgentInfo> _agents = new();
    private string? _initialKeyA;
    private string? _initialKeyB;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public CompareForm(MonitorServer server, AppSettings settings, string? keyA = null, string? keyB = null)
    {
        _server = server;
        _settings = settings;
        _initialKeyA = keyA;
        _initialKeyB = keyB;

        Text = "So sánh hiệu năng giữa 2 máy trạm — LanMonitor";
        Size = new Size(940, 650);
        MinimumSize = new Size(820, 550);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = DesignTokens.SurfaceCanvas;

        _topBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = DesignTokens.SurfaceSubtle
        };
        _topBar.Paint += (_, e) =>
        {
            using var pen = new Pen(DesignTokens.BorderStrong, 1);
            e.Graphics.DrawLine(pen, 0, _topBar.Height - 1, _topBar.Width, _topBar.Height - 1);
        };

        _selA = new ModernSelector
        {
            BadgeText = "MÁY A",
            BadgeColor = DesignTokens.AccentPrimary,
            Location = new Point(18, 13),
            Size = new Size(295, 38)
        };
        _selA.SelectedIndexChanged += (_, _) => _canvas?.Invalidate();

        _selB = new ModernSelector
        {
            BadgeText = "MÁY B",
            BadgeColor = DesignTokens.ChartRam,
            Location = new Point(325, 13),
            Size = new Size(295, 38)
        };
        _selB.SelectedIndexChanged += (_, _) => _canvas?.Invalidate();

        _segMetric = new SegmentedControl
        {
            Items = new[] { "Biểu đồ CPU %", "Biểu đồ RAM %" },
            Size = new Size(236, 36),
            Location = new Point(682, 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _segMetric.SelectedIndexChanged += (_, _) => _canvas?.Invalidate();

        _topBar.Controls.Add(_selA);
        _topBar.Controls.Add(_selB);
        _topBar.Controls.Add(_segMetric);

        _canvas = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = DesignTokens.SurfaceCanvas
        };
        typeof(Control).InvokeMember(
            "DoubleBuffered",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
            null, _canvas, new object[] { true });

        _canvas.Paint += Canvas_Paint;
        _canvas.Resize += (_, _) => _canvas.Invalidate();

        Controls.Add(_canvas);
        Controls.Add(_topBar);

        _refreshTimer = new System.Windows.Forms.Timer { Interval = 1500 };
        _refreshTimer.Tick += (_, _) => _canvas.Invalidate();
        _refreshTimer.Start();

        Load += (_, _) =>
        {
            try
            {
                int useDark = DesignTokens.IsDark ? 1 : 0;
                DwmSetWindowAttribute(Handle, 20, ref useDark, sizeof(int));
            }
            catch { }
            PopulateMachines();
        };

        FormClosed += (_, _) => _refreshTimer.Stop();
    }

    public void SelectMachines(string? keyA, string? keyB)
    {
        _initialKeyA = keyA;
        _initialKeyB = keyB;
        PopulateMachines();
    }

    private void PopulateMachines()
    {
        _agents = _server.Agents.Values
            .OrderByDescending(a => a.Online)
            .ThenBy(a => a.Name)
            .ToList();

        if (_agents.Count == 0)
        {
            _selA.SetItems(Array.Empty<ModernSelector.SelectorItem>(), -1);
            _selB.SetItems(Array.Empty<ModernSelector.SelectorItem>(), -1);
            _canvas.Invalidate();
            return;
        }

        var items = _agents.Select(a => new ModernSelector.SelectorItem(
            a.Key,
            a.Name,
            $"{a.Ip} · {(a.Online ? "Online" : "Offline")}",
            a.Online)).ToList();

        int idxA = _initialKeyA != null ? _agents.FindIndex(a => a.Key == _initialKeyA) : 0;
        if (idxA < 0) idxA = 0;

        int idxB = _initialKeyB != null ? _agents.FindIndex(a => a.Key == _initialKeyB) : (_agents.Count > 1 ? 1 : 0);
        if (idxB < 0) idxB = _agents.Count > 1 && idxA == 0 ? 1 : 0;

        _selA.SetItems(items, idxA);
        _selB.SetItems(items, idxB);
        _canvas.Invalidate();
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int w = _canvas.Width;
        int h = _canvas.Height;

        if (_agents.Count == 0 || _selA.SelectedIndex < 0 || _selB.SelectedIndex < 0)
        {
            TextRenderer.DrawText(
                g,
                "Cần ít nhất 1–2 máy trạm trong danh sách để thực hiện so sánh hiệu năng.\nBạn có thể bật 'Mô phỏng máy trạm ảo' trong Cài đặt để thử ngay.",
                DesignTokens.FontBody,
                new Rectangle(24, 24, w - 48, h - 48),
                DesignTokens.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            return;
        }

        var a = _agents[_selA.SelectedIndex];
        var b = _agents[_selB.SelectedIndex];

        int pad = 18;
        int gap = 14;
        int cardW = (w - pad * 2 - gap) / 2;
        int summaryH = 92;

        DrawMachineHeaderCard(g, new Rectangle(pad, pad, cardW, summaryH), a, "MÁY A", DesignTokens.AccentPrimary);
        DrawMachineHeaderCard(g, new Rectangle(pad + cardW + gap, pad, cardW, summaryH), b, "MÁY B", DesignTokens.ChartRam);

        int barsTop = pad + summaryH + 14;
        int barsH = 185;
        Rectangle barsCard = new(pad, barsTop, w - pad * 2, barsH);
        using (var path = GraphicsExtensions.CreateRoundedRect(barsCard, 10))
        using (var bg = new SolidBrush(DesignTokens.SurfaceElevated))
        using (var pen = new Pen(DesignTokens.BorderStrong, 1))
        {
            g.FillPath(bg, path);
            g.DrawPath(pen, path);
        }

        DrawComparisonRow(g, barsCard, 14, "CPU hiện tại", a.Cpu, b.Cpu, "%", _settings.CpuLimit);
        DrawComparisonRow(g, barsCard, 56, "RAM hiện tại", a.Ram, b.Ram, "%", _settings.RamLimit);
        DrawComparisonRow(g, barsCard, 98, "Ổ đĩa đã dùng", a.Disk, b.Disk, "%", _settings.DiskLimit);
        DrawComparisonRow(g, barsCard, 140, "CPU Trung bình / Đỉnh", a.AvgCpu, b.AvgCpu, $"% (Đỉnh: {a.PeakCpu}% vs {b.PeakCpu}%)", 101);

        int chartTop = barsTop + barsH + 14;
        int chartH = Math.Max(140, h - chartTop - pad);
        Rectangle chartCard = new(pad, chartTop, w - pad * 2, chartH);
        DrawDualChart(g, chartCard, a, b);
    }

    private void DrawMachineHeaderCard(Graphics g, Rectangle rect, AgentInfo agent, string badge, Color accent)
    {
        using (var path = GraphicsExtensions.CreateRoundedRect(rect, 10))
        using (var bg = new SolidBrush(DesignTokens.SurfaceElevated))
        using (var pen = new Pen(DesignTokens.BorderStrong, 1))
        {
            g.FillPath(bg, path);
            g.DrawPath(pen, path);
        }

        Rectangle badgeRect = new(rect.X + 14, rect.Y + 14, 58, 22);
        using (var bPath = GraphicsExtensions.CreateRoundedRect(badgeRect, 6))
        using (var bBrush = new SolidBrush(accent))
            g.FillPath(bBrush, bPath);

        TextRenderer.DrawText(g, badge, DesignTokens.FontBadge, badgeRect,
            DesignTokens.IsDark ? Color.FromArgb(15, 23, 42) : Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        TextRenderer.DrawText(g, agent.Name, DesignTokens.FontHeading,
            new Point(rect.X + 80, rect.Y + 12), DesignTokens.TextPrimary, TextFormatFlags.NoPrefix);

        string status = agent.Online ? "Online" : "Offline";
        Color statusFg = agent.Online ? DesignTokens.StatusOnlineText : DesignTokens.StatusOfflineText;
        Color statusBg = agent.Online ? DesignTokens.StatusOnlineBg : DesignTokens.StatusOfflineBg;
        Rectangle stRect = new(rect.Right - 84, rect.Y + 14, 70, 22);
        using (var sPath = GraphicsExtensions.CreateRoundedRect(stRect, 11))
        using (var sBrush = new SolidBrush(statusBg))
            g.FillPath(sBrush, sPath);
        TextRenderer.DrawText(g, status, DesignTokens.FontBadge, stRect, statusFg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        string meta = $"IP: {agent.Ip}  ·  HĐH: {agent.Os}";
        TextRenderer.DrawText(g, meta, DesignTokens.FontBody,
            new Rectangle(rect.X + 14, rect.Y + 44, rect.Width - 28, 20),
            DesignTokens.TextSecondary, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        string stats = $"TB CPU: {agent.AvgCpu}% (Đỉnh {agent.PeakCpu}%)   ·   TB RAM: {agent.AvgRam}% (Đỉnh {agent.PeakRam}%)";
        TextRenderer.DrawText(g, stats, DesignTokens.FontTabular,
            new Rectangle(rect.X + 14, rect.Y + 66, rect.Width - 28, 18),
            DesignTokens.TextMuted, TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private void DrawComparisonRow(Graphics g, Rectangle card, int offsetY, string label, int valA, int valB, string suffix, int limit)
    {
        int y = card.Y + offsetY;
        int center = card.X + card.Width / 2;
        int labelW = 190;

        TextRenderer.DrawText(g, label, DesignTokens.FontBodyStrong,
            new Rectangle(center - labelW / 2, y, labelW, 20),
            DesignTokens.TextPrimary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        TextRenderer.DrawText(g, $"{valA}%", DesignTokens.FontTabularBold,
            new Rectangle(card.X + 16, y, 70, 20),
            valA >= limit ? DesignTokens.StatusDangerText : DesignTokens.AccentPrimary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        TextRenderer.DrawText(g, $"{valB}%", DesignTokens.FontTabularBold,
            new Rectangle(card.Right - 86, y, 70, 20),
            valB >= limit ? DesignTokens.StatusDangerText : DesignTokens.ChartRam,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        if (suffix.Length > 2)
        {
            TextRenderer.DrawText(g, suffix.TrimStart('%', ' '), DesignTokens.FontLabel,
                new Rectangle(center - 130, y + 20, 260, 16),
                DesignTokens.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);
            return;
        }

        int barW = (card.Width - 60) / 2 - 12;
        int barY = y + 22;
        int barH = 10;

        Rectangle leftTrack = new(center - 6 - barW, barY, barW, barH);
        Rectangle rightTrack = new(center + 6, barY, barW, barH);

        using var trackBrush = new SolidBrush(DesignTokens.MeterTrack);
        using (var lp = GraphicsExtensions.CreateRoundedRect(leftTrack, 5)) g.FillPath(trackBrush, lp);
        using (var rp = GraphicsExtensions.CreateRoundedRect(rightTrack, 5)) g.FillPath(trackBrush, rp);

        int wA = (int)Math.Round(barW * (Math.Clamp(valA, 0, 100) / 100.0));
        if (wA > 3)
        {
            Rectangle fillA = new(leftTrack.Right - wA, barY, wA, barH);
            Color cA = valA >= limit ? DesignTokens.StatusDangerBar : DesignTokens.AccentPrimary;
            using var fp = GraphicsExtensions.CreateRoundedRect(fillA, 5);
            using var fb = new SolidBrush(cA);
            g.FillPath(fb, fp);
        }

        int wB = (int)Math.Round(barW * (Math.Clamp(valB, 0, 100) / 100.0));
        if (wB > 3)
        {
            Rectangle fillB = new(rightTrack.X, barY, wB, barH);
            Color cB = valB >= limit ? DesignTokens.StatusDangerBar : DesignTokens.ChartRam;
            using var fp = GraphicsExtensions.CreateRoundedRect(fillB, 5);
            using var fb = new SolidBrush(cB);
            g.FillPath(fb, fp);
        }
    }

    private void DrawDualChart(Graphics g, Rectangle card, AgentInfo a, AgentInfo b)
    {
        using (var path = GraphicsExtensions.CreateRoundedRect(card, 10))
        using (var bg = new SolidBrush(DesignTokens.SurfaceElevated))
        using (var pen = new Pen(DesignTokens.BorderStrong, 1))
        {
            g.FillPath(bg, path);
            g.DrawPath(pen, path);
        }

        bool showCpu = _segMetric.SelectedIndex == 0;
        string title = showCpu
            ? "SO SÁNH BIỂU ĐỒ CPU (%) THEO THỜI GIAN THỰC"
            : "SO SÁNH BIỂU ĐỒ RAM (%) THEO THỜI GIAN THỰC";

        TextRenderer.DrawText(g, title, DesignTokens.FontSection,
            new Point(card.X + 16, card.Y + 12), DesignTokens.TextPrimary, TextFormatFlags.NoPrefix);

        int legX = card.Right - 360;
        using (var bA = new SolidBrush(DesignTokens.AccentPrimary))
        using (var bB = new SolidBrush(DesignTokens.ChartRam))
        {
            g.FillRectangle(bA, legX, card.Y + 14, 12, 12);
            TextRenderer.DrawText(g, $"Máy A: {a.Name}", DesignTokens.FontLabel,
                new Point(legX + 16, card.Y + 12), DesignTokens.TextPrimary, TextFormatFlags.NoPrefix);

            g.FillRectangle(bB, legX + 175, card.Y + 14, 12, 12);
            TextRenderer.DrawText(g, $"Máy B: {b.Name}", DesignTokens.FontLabel,
                new Point(legX + 191, card.Y + 12), DesignTokens.TextPrimary, TextFormatFlags.NoPrefix);
        }

        Rectangle plot = new(card.X + 48, card.Y + 42, card.Width - 66, card.Height - 58);
        if (plot.Width < 40 || plot.Height < 40) return;

        using var gridPen = new Pen(DesignTokens.BorderSubtle, 1);
        foreach (int t in new[] { 0, 25, 50, 75, 100 })
        {
            int y = plot.Bottom - (int)Math.Round(plot.Height * (t / 100.0));
            g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
            TextRenderer.DrawText(g, $"{t}%", DesignTokens.FontTabular,
                new Rectangle(card.X + 4, y - 8, 40, 16), DesignTokens.TextMuted,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        List<int> ptsA, ptsB;
        lock (a.CpuHistory) ptsA = (showCpu ? a.CpuHistory : a.RamHistory).ToList();
        lock (b.CpuHistory) ptsB = (showCpu ? b.CpuHistory : b.RamHistory).ToList();

        DrawLineSeries(g, plot, ptsA, DesignTokens.AccentPrimary);
        DrawLineSeries(g, plot, ptsB, DesignTokens.ChartRam);

        using var borderPen = new Pen(DesignTokens.BorderStrong, 1);
        g.DrawRectangle(borderPen, plot);
    }

    private static void DrawLineSeries(Graphics g, Rectangle plot, List<int> values, Color color)
    {
        if (values.Count == 0) return;
        int max = AgentInfo.MaxHistoryPoints;
        var pts = new List<PointF>(values.Count);
        for (int i = 0; i < values.Count; i++)
        {
            float x = values.Count == 1
                ? plot.Left + plot.Width / 2f
                : plot.Left + (float)i / (max - 1) * plot.Width;
            float y = plot.Bottom - (Math.Clamp(values[i], 0, 100) / 100f) * plot.Height;
            pts.Add(new PointF(x, y));
        }

        if (pts.Count >= 2)
        {
            using var pen = new Pen(color, 2.4f);
            g.DrawLines(pen, pts.ToArray());
        }

        var last = pts[^1];
        using var dot = new SolidBrush(color);
        g.FillEllipse(dot, last.X - 4, last.Y - 4, 8, 8);
    }
}
