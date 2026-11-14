using System.Collections.Concurrent;
using System.Drawing.Drawing2D;
using System.Media;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using LanMonitor.Shared;

namespace LanMonitor.Server;

public partial class Form1 : Form
{
    private readonly MonitorServer _server = new();
    private readonly Dictionary<string, DataGridViewRow> _rowsByKey = new();
    private AppSettings _settings = AppSettings.Load();
    private bool _syncingControls;
    private bool _allowExit;

    // Trạng thái mô phỏng máy trạm ảo (Demo / Test)
    private CancellationTokenSource? _demoCts;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _demoAgentCts = new();
    private DemoSimulationConfig _demoConfig = new(4, "PC-LAB", DemoLoadProfile.Mixed, DemoOsChoice.Mixed, false);

    // Chuột phải & So sánh 2 máy & Hover
    private string? _compareKeyA;
    private int _hoveredRowIndex = -1;
    private CompareForm? _compareForm;

    private ToolStripMenuItem _miPing = null!;
    private ToolStripMenuItem _miSetCompareA = null!;
    private ToolStripMenuItem _miCompareWithA = null!;
    private ToolStripMenuItem _miOpenCompare = null!;
    private ToolStripMenuItem _miCopyIp = null!;
    private ToolStripMenuItem _miKick = null!;
    private ToolStripMenuItem _miRemove = null!;

    private int CpuLimit => _settings.CpuLimit;
    private int RamLimit => _settings.RamLimit;
    private int DiskLimit => _settings.DiskLimit;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public Form1()
    {
        InitializeComponent();

        EnableDoubleBuffering(dgvAgents);
        EnableDoubleBuffering(pnlHeader);
        EnableDoubleBuffering(pnlFooter);
        EnableDoubleBuffering(pnlChartCanvas);
        EnableDoubleBuffering(lstLogs);

        _server.AgentUpdated += OnAgentUpdated;
        _server.LogMessage += OnLogMessage;

        SetupTrayIcon();
        SetupAgentContextMenu();
        WireSettingsDrawer();
        ApplySettings(_settings, saveToDisk: false);
    }

    private static void EnableDoubleBuffering(Control ctrl)
    {
        typeof(Control).InvokeMember(
            "DoubleBuffered",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
            null, ctrl, new object[] { true });
    }

    private void SetupTrayIcon()
    {
        // Tạo icon vector cho thanh taskbar & khay hệ thống
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var bg = new SolidBrush(Color.FromArgb(37, 99, 235));
            using var path = GraphicsExtensions.CreateRoundedRect(new Rectangle(1, 1, 30, 30), 7);
            g.FillPath(bg, path);

            using var pen = new Pen(Color.White, 2f);
            Point p1 = new(16, 8), p2 = new(8, 22), p3 = new(24, 22);
            g.DrawLine(pen, p1, p2);
            g.DrawLine(pen, p1, p3);
            g.DrawLine(pen, p2, p3);
            g.FillEllipse(Brushes.White, p1.X - 2.5f, p1.Y - 2.5f, 5, 5);
            g.FillEllipse(Brushes.White, p2.X - 2.5f, p2.Y - 2.5f, 5, 5);
            g.FillEllipse(Brushes.White, p3.X - 2.5f, p3.Y - 2.5f, 5, 5);
        }

        IntPtr hIcon = bmp.GetHicon();
        var icon = Icon.FromHandle(hIcon);
        Icon = (Icon)icon.Clone();
        trayIcon.Icon = (Icon)icon.Clone();

        trayMenu.Items.Add("Mở Dashboard", null, (_, _) => RestoreFromTray());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("Thoát hoàn toàn", null, (_, _) =>
        {
            _allowExit = true;
            Close();
        });
        trayIcon.ContextMenuStrip = trayMenu;
    }

    private void SetupAgentContextMenu()
    {
        _miPing = new ToolStripMenuItem("Ping kiểm tra độ trễ (ICMP)");
        _miPing.Click += async (_, _) =>
        {
            if (GetSelectedAgent() is { } agent)
                await PingAgentAsync(agent);
        };

        _miSetCompareA = new ToolStripMenuItem("Đặt làm Máy A (So sánh)");
        _miSetCompareA.Click += (_, _) =>
        {
            if (GetSelectedAgent() is { } agent)
            {
                _compareKeyA = agent.Key;
                OnLogMessage($"[{DateTime.Now:HH:mm:ss}] [So sánh] Đã chọn '{agent.Name}' làm Máy A. Chuột phải máy thứ 2 để so sánh.");
            }
        };

        _miCompareWithA = new ToolStripMenuItem("So sánh với Máy A");
        _miCompareWithA.Click += (_, _) =>
        {
            if (GetSelectedAgent() is { } agentB)
                OpenCompareDialog(_compareKeyA, agentB.Key);
        };

        _miOpenCompare = new ToolStripMenuItem("Mở bảng So sánh 2 máy...");
        _miOpenCompare.Click += (_, _) =>
        {
            var sel = GetSelectedAgent();
            OpenCompareDialog(_compareKeyA ?? sel?.Key, _compareKeyA != null ? sel?.Key : null);
        };

        _miCopyIp = new ToolStripMenuItem("Sao chép địa chỉ IP");
        _miCopyIp.Click += (_, _) =>
        {
            if (GetSelectedAgent() is { } agent)
            {
                Clipboard.SetText(agent.Ip);
                OnLogMessage($"[{DateTime.Now:HH:mm:ss}] Đã sao chép IP {agent.Ip} vào Clipboard.");
            }
        };

        _miKick = new ToolStripMenuItem("Kick (Ngắt kết nối)")
        {
            Font = new Font(DesignTokens.FontBody, FontStyle.Bold)
        };
        _miKick.Click += (_, _) =>
        {
            if (GetSelectedAgent() is { } agent)
                KickSelectedAgent(agent);
        };

        _miRemove = new ToolStripMenuItem("Xóa khỏi danh sách");
        _miRemove.Click += (_, _) =>
        {
            if (GetSelectedAgent() is { } agent)
                RemoveSingleAgent(agent);
        };

        agentMenu.Items.AddRange(new ToolStripItem[]
        {
            _miPing,
            new ToolStripSeparator(),
            _miSetCompareA,
            _miCompareWithA,
            _miOpenCompare,
            new ToolStripSeparator(),
            _miCopyIp,
            new ToolStripSeparator(),
            _miKick,
            _miRemove
        });

        agentMenu.Opening += (_, e) =>
        {
            var agent = GetSelectedAgent();
            if (agent == null)
            {
                e.Cancel = true;
                return;
            }

            _miKick.Enabled = agent.Online;
            _miKick.Text = agent.Online
                ? $"Kick '{agent.Name}' (Ngắt kết nối)"
                : $"'{agent.Name}' đang Offline";
            _miKick.ForeColor = agent.Online ? DesignTokens.StatusDangerText : DesignTokens.TextMuted;

            if (_compareKeyA != null && _server.Agents.TryGetValue(_compareKeyA, out var agentA) && _compareKeyA != agent.Key)
            {
                _miCompareWithA.Visible = true;
                _miCompareWithA.Text = $"So sánh '{agent.Name}' với '{agentA.Name}'";
            }
            else
            {
                _miCompareWithA.Visible = false;
            }
        };

        dgvAgents.ContextMenuStrip = agentMenu;
    }

    private void WireSettingsDrawer()
    {
        settingsDrawer.CloseRequested += () => ToggleSettingsDrawer(false);
        settingsDrawer.SettingsChanged += s => ApplySettings(s, saveToDisk: true);
        settingsDrawer.ExportCsvRequested += ExportAgentsToCsv;
        settingsDrawer.ExportLogRequested += ExportLogsToTxt;
        settingsDrawer.ToggleDemoAgentsRequested += ToggleDemoAgents;
        settingsDrawer.ResetDefaultsRequested += () =>
        {
            _settings.ResetToDefaults();
            ApplySettings(_settings, saveToDisk: true);
            OnLogMessage($"[{DateTime.Now:HH:mm:ss}] [Cài đặt] Đã khôi phục cấu hình mặc định.");
        };

        configurePopover.ValuesChanged += (cpu, ram, disk, interval) =>
        {
            if (_syncingControls) return;
            _settings.CpuLimit = cpu;
            _settings.RamLimit = ram;
            _settings.DiskLimit = disk;
            _settings.ReportIntervalSeconds = interval;
            ApplySettings(_settings, saveToDisk: true);
        };
        configurePopover.Closed += () => btnConfigure.Active = false;
    }

    private void Form1_Load(object? sender, EventArgs e)
    {
        LayoutHeaderAndToolbar();
        if (splitBottom.Width > 200)
            splitBottom.SplitterDistance = (int)(splitBottom.Width * 0.48);

        _server.ReportIntervalSeconds = _settings.ReportIntervalSeconds;
        _server.Start();
        UpdateServerStatusUi();
        ApplyTitleBarTheme();
    }

    private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_allowExit && _settings.MinimizeToTray && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            trayIcon.ShowBalloonTip(2000, "LanMonitor Server",
                "Server vẫn đang chạy ngầm và giám sát mạng LAN.", ToolTipIcon.Info);
            return;
        }

        _demoCts?.Cancel();
        timerWatchdog.Stop();
        _server.Stop();
        trayIcon.Visible = false;
    }

    private void RestoreFromTray()
    {
        Show();
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;
        Activate();
    }

    // =========================================================================
    // ÁP DỤNG THEME SÁNG / TỐI & CẤU HÌNH TOÀN CỤC
    // =========================================================================
    private void ApplySettings(AppSettings s, bool saveToDisk)
    {
        _settings = s;
        bool isDark = DesignTokens.Resolve(s.Theme);
        DesignTokens.Apply(isDark, s.AccentIndex);

        _syncingControls = true;
        configurePopover.LoadValues(s.CpuLimit, s.RamLimit, s.DiskLimit, s.ReportIntervalSeconds);
        settingsDrawer.LoadFrom(s);
        _syncingControls = false;

        _server.ReportIntervalSeconds = s.ReportIntervalSeconds;
        dgvAgents.RowTemplate.Height = s.CompactRows ? 34 : 42;
        foreach (DataGridViewRow r in dgvAgents.Rows)
            r.Height = dgvAgents.RowTemplate.Height;

        ApplyThemeToControls();
        if (IsHandleCreated) ApplyTitleBarTheme();

        foreach (var agent in _server.Agents.Values)
            UpdateOrCreateRow(agent);

        ApplyFilter();
        pnlHeader.Invalidate();
        pnlFooter.Invalidate();
        pnlToolbar.Invalidate();
        pnlChartCanvas.Invalidate();

        if (saveToDisk)
            _settings.Save();
    }

    private void ApplyThemeToControls()
    {
        BackColor = DesignTokens.SurfaceCanvas;
        pnlHeader.BackColor = DesignTokens.SurfaceHeader;
        pnlToolbar.BackColor = DesignTokens.SurfaceSubtle;

        searchBox.ApplyTheme();
        segFilter.Invalidate();

        splitMain.BackColor = DesignTokens.BorderStrong;
        splitBottom.BackColor = DesignTokens.BorderStrong;

        dgvAgents.BackgroundColor = DesignTokens.SurfaceCanvas;
        dgvAgents.GridColor = DesignTokens.BorderSubtle;
        dgvAgents.ColumnHeadersDefaultCellStyle.BackColor = DesignTokens.SurfaceElevated;
        dgvAgents.ColumnHeadersDefaultCellStyle.ForeColor = DesignTokens.TextSecondary;

        pnlChartContainer.BackColor = DesignTokens.SurfaceElevated;
        pnlChartHeader.BackColor = DesignTokens.SurfaceSubtle;
        lblChartTitle.ForeColor = DesignTokens.TextPrimary;
        pnlChartCanvas.BackColor = DesignTokens.SurfaceElevated;

        pnlLogHeader.BackColor = DesignTokens.SurfaceSubtle;
        lblLogTitle.ForeColor = DesignTokens.TextPrimary;
        lstLogs.BackColor = DesignTokens.SurfaceTerminal;
        lstLogs.ForeColor = DesignTokens.TextOnDark;

        pnlFooter.BackColor = DesignTokens.SurfaceSubtle;
        agentMenu.BackColor = DesignTokens.SurfaceElevated;
        agentMenu.ForeColor = DesignTokens.TextPrimary;

        configurePopover.ApplyTheme();
        settingsDrawer.ApplyTheme();
        Invalidate(true);
    }

    private void ApplyTitleBarTheme()
    {
        try
        {
            int useDark = DesignTokens.IsDark ? 1 : 0;
            // DWMWA_USE_IMMERSIVE_DARK_MODE = 20 (Windows 10 build 19041+ & Windows 11)
            DwmSetWindowAttribute(Handle, 20, ref useDark, sizeof(int));
        }
        catch { }
    }

    private void LayoutHeaderAndToolbar()
    {
        int hw = pnlHeader.ClientSize.Width;
        if (hw > 360)
        {
            btnSettings.Location = new Point(hw - btnSettings.Width - 16, 14);
            btnToggleServer.Location = new Point(btnSettings.Left - btnToggleServer.Width - 10, 14);
            btnRemoveOffline.Location = new Point(btnToggleServer.Left - btnRemoveOffline.Width - 10, 14);
        }

        int tw = pnlToolbar.ClientSize.Width;
        if (tw > 400)
        {
            btnConfigure.Location = new Point(tw - btnConfigure.Width - 16, 9);
            configurePopover.Location = new Point(
                Math.Max(16, pnlToolbar.Left + btnConfigure.Right - configurePopover.Width),
                pnlToolbar.Bottom);
        }
    }

    // =========================================================================
    // NHẬN DỮ LIỆU TỪ SERVER & CẬP NHẬT DASHBOARD
    // =========================================================================
    private void OnAgentUpdated(AgentInfo agent)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(() => OnAgentUpdated(agent)); }
            catch (ObjectDisposedException) { }
            return;
        }

        bool wasOnlineBefore = _rowsByKey.TryGetValue(agent.Key, out var existingRow)
            && existingRow.Cells["colStatus"].Value?.ToString() == "Online";

        UpdateOrCreateRow(agent);
        CheckAndLogThresholdAlert(agent);

        if (wasOnlineBefore && !agent.Online && _settings.NotifyOnOffline)
        {
            trayIcon.ShowBalloonTip(3000, "Máy trạm mất kết nối",
                $"Máy '{agent.Name}' ({agent.Ip}) đã chuyển sang Offline.", ToolTipIcon.Warning);
        }

        ApplyFilter();
        pnlHeader.Invalidate();
        pnlFooter.Invalidate();

        if (GetSelectedAgent()?.Key == agent.Key || dgvAgents.Rows.Count == 1)
        {
            UpdateChartHeader();
            pnlChartCanvas.Invalidate();
        }
    }

    private void OnLogMessage(string msg)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(() => OnLogMessage(msg)); }
            catch (ObjectDisposedException) { }
            return;
        }

        lstLogs.Items.Add(msg);
        while (lstLogs.Items.Count > 500)
            lstLogs.Items.RemoveAt(0);

        lstLogs.TopIndex = lstLogs.Items.Count - 1;
    }

    private void UpdateOrCreateRow(AgentInfo agent)
    {
        if (!_rowsByKey.TryGetValue(agent.Key, out var row))
        {
            int idx = dgvAgents.Rows.Add();
            row = dgvAgents.Rows[idx];
            row.Height = dgvAgents.RowTemplate.Height;
            row.Tag = agent;
            _rowsByKey[agent.Key] = row;
        }

        bool isAlert = agent.IsAlerting(CpuLimit, RamLimit, DiskLimit);

        row.Cells["colStatus"].Value = agent.Online ? "Online" : "Offline";
        row.Cells["colName"].Value = agent.Name;
        row.Cells["colIp"].Value = agent.Ip;
        row.Cells["colOs"].Value = agent.Os;
        row.Cells["colCpu"].Value = agent.Online ? $"{agent.Cpu}%" : "—";
        row.Cells["colRam"].Value = agent.Online ? $"{agent.Ram}%" : "—";
        row.Cells["colDisk"].Value = agent.Online ? $"{agent.Disk}%" : "—";
        row.Cells["colAlert"].Value = agent.GetAlertText(CpuLimit, RamLimit, DiskLimit);
        row.Cells["colLastSeen"].Value = agent.LastSeen.ToString("HH:mm:ss");

        row.Cells["colIp"].Style.Font = DesignTokens.FontTabular;
        row.Cells["colLastSeen"].Style.Font = DesignTokens.FontTabular;
        row.Cells["colName"].Style.Font = DesignTokens.FontBodyStrong;

        if (!agent.Online)
        {
            row.DefaultCellStyle.BackColor = DesignTokens.OfflineRowBg;
            row.DefaultCellStyle.ForeColor = DesignTokens.TextMuted;
            row.DefaultCellStyle.SelectionBackColor = DesignTokens.SurfaceSubtle;
            row.DefaultCellStyle.SelectionForeColor = DesignTokens.TextSecondary;
            row.Cells["colAlert"].Style.ForeColor = DesignTokens.TextMuted;
            row.Cells["colAlert"].Style.Font = DesignTokens.FontBody;
        }
        else if (isAlert)
        {
            row.DefaultCellStyle.BackColor = DesignTokens.StatusDangerRowBg;
            row.DefaultCellStyle.ForeColor = DesignTokens.TextPrimary;
            row.DefaultCellStyle.SelectionBackColor = DesignTokens.StatusDangerBg;
            row.DefaultCellStyle.SelectionForeColor = DesignTokens.StatusDangerText;
            row.Cells["colAlert"].Style.ForeColor = DesignTokens.StatusDangerText;
            row.Cells["colAlert"].Style.Font = DesignTokens.FontBodyStrong;
        }
        else
        {
            row.DefaultCellStyle.BackColor = DesignTokens.SurfaceElevated;
            row.DefaultCellStyle.ForeColor = DesignTokens.TextPrimary;
            row.DefaultCellStyle.SelectionBackColor = DesignTokens.AccentSelectionBg;
            row.DefaultCellStyle.SelectionForeColor = DesignTokens.TextPrimary;
            row.Cells["colAlert"].Style.ForeColor = DesignTokens.StatusOnlineText;
            row.Cells["colAlert"].Style.Font = DesignTokens.FontBody;
        }

        dgvAgents.InvalidateRow(row.Index);
    }

    private void ApplyFilter()
    {
        string query = searchBox.SearchText.Trim();
        int mode = segFilter.SelectedIndex;

        foreach (DataGridViewRow row in dgvAgents.Rows)
        {
            if (row.Tag is not AgentInfo agent) continue;

            bool matchSearch = string.IsNullOrEmpty(query)
                || agent.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || agent.Ip.Contains(query, StringComparison.OrdinalIgnoreCase)
                || agent.Os.Contains(query, StringComparison.OrdinalIgnoreCase);

            bool matchMode = mode switch
            {
                1 => agent.Online,
                2 => agent.IsAlerting(CpuLimit, RamLimit, DiskLimit),
                3 => !agent.Online,
                _ => true
            };

            row.Visible = matchSearch && matchMode;
        }

        dgvAgents.Invalidate();
    }

    private void CheckAndLogThresholdAlert(AgentInfo agent)
    {
        bool isAlert = agent.IsAlerting(CpuLimit, RamLimit, DiskLimit);
        if (isAlert && !agent.WasAlerting)
        {
            string alertText = agent.GetAlertText(CpuLimit, RamLimit, DiskLimit);
            OnLogMessage($"[{DateTime.Now:HH:mm:ss}] [CẢNH BÁO] Máy '{agent.Name}' ({agent.Ip}) -> {alertText}");

            if (_settings.NotifyOnThreshold)
            {
                trayIcon.ShowBalloonTip(3500, $"Cảnh báo vượt ngưỡng — {agent.Name}",
                    $"{agent.Ip}: {alertText}", ToolTipIcon.Warning);
            }
            if (_settings.SoundAlert)
                SystemSounds.Exclamation.Play();
        }
        agent.WasAlerting = isAlert;
    }

    // =========================================================================
    // VẼ HEADER, BẢNG DỮ LIỆU, BIỂU ĐỒ & CONSOLE
    // =========================================================================
    private void pnlHeader_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle iconRect = new(16, 16, 30, 30);
        using (var accentBrush = new SolidBrush(DesignTokens.AccentPrimary))
        using (var pen = new Pen(DesignTokens.AccentForeground, 1.8f))
        using (var dotBrush = new SolidBrush(DesignTokens.AccentForeground))
        {
            using var path = GraphicsExtensions.CreateRoundedRect(iconRect, 8);
            g.FillPath(accentBrush, path);

            Point pTop = new(iconRect.X + 15, iconRect.Y + 8);
            Point pLeft = new(iconRect.X + 8, iconRect.Y + 21);
            Point pRight = new(iconRect.X + 22, iconRect.Y + 21);
            g.DrawLine(pen, pTop, pLeft);
            g.DrawLine(pen, pTop, pRight);
            g.DrawLine(pen, pLeft, pRight);
            g.FillEllipse(dotBrush, pTop.X - 2, pTop.Y - 2, 5, 5);
            g.FillEllipse(dotBrush, pLeft.X - 2, pLeft.Y - 2, 5, 5);
            g.FillEllipse(dotBrush, pRight.X - 2, pRight.Y - 2, 5, 5);
        }

        TextRenderer.DrawText(
            g, "LAN MONITOR",
            DesignTokens.FontHeading,
            new Point(54, 11),
            DesignTokens.TextOnDark,
            TextFormatFlags.NoPrefix);

        bool running = _server.IsRunning;
        string statusText = running ? "ONLINE" : "OFFLINE";

        Color dotColor = running ? Color.FromArgb(34, 197, 94) : DesignTokens.StatusOfflineDot;
        using (var statusBrush = new SolidBrush(dotColor))
            g.FillEllipse(statusBrush, 58, 38, 8, 8);

        TextRenderer.DrawText(
            g, statusText,
            DesignTokens.FontBadge,
            new Point(70, 34),
            running ? Color.FromArgb(134, 239, 172) : DesignTokens.TextOnDarkMuted,
            TextFormatFlags.NoPrefix);

        var all = _server.Agents.Values.ToList();
        int total = all.Count;
        int online = all.Count(a => a.Online);
        int offline = total - online;
        int alerting = all.Count(a => a.IsAlerting(CpuLimit, RamLimit, DiskLimit));

        int startX = 355;
        DrawTelemetryMetric(g, ref startX, "TỔNG MÁY", total.ToString(), DesignTokens.TextOnDark);
        DrawTelemetryMetric(g, ref startX, "ONLINE", online.ToString(), Color.FromArgb(74, 222, 128));
        DrawTelemetryMetric(g, ref startX, "OFFLINE", offline.ToString(), DesignTokens.TextOnDarkMuted);
        DrawTelemetryMetric(g, ref startX, "VƯỢT NGƯỠNG", alerting.ToString(),
            alerting > 0 ? Color.FromArgb(248, 113, 113) : DesignTokens.TextOnDarkMuted);
    }

    private static void DrawTelemetryMetric(Graphics g, ref int x, string label, string value, Color valueColor)
    {
        using var divPen = new Pen(DesignTokens.BorderDark, 1);
        g.DrawLine(divPen, x, 15, x, 47);
        x += 16;

        TextRenderer.DrawText(g, label, DesignTokens.FontLabel, new Point(x, 12), DesignTokens.TextOnDarkMuted, TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(g, value, DesignTokens.FontHeading, new Point(x, 28), valueColor, TextFormatFlags.NoPrefix);
        x += 104;
    }

    private void pnlToolbar_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(DesignTokens.BorderStrong, 1);
        e.Graphics.DrawLine(pen, 0, pnlToolbar.Height - 1, pnlToolbar.Width, pnlToolbar.Height - 1);
    }

    private void pnlSubHeader_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Control c) return;
        using var pen = new Pen(DesignTokens.BorderStrong, 1);
        e.Graphics.DrawLine(pen, 0, c.Height - 1, c.Width, c.Height - 1);
    }

    private void dgvAgents_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.Graphics == null) return;

        if (e.RowIndex == -1 && e.ColumnIndex >= 0)
        {
            e.PaintBackground(e.ClipBounds, false);
            using var bottomPen = new Pen(DesignTokens.BorderStrong, 1);
            e.Graphics.DrawLine(bottomPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);

            Rectangle textRect = new(e.CellBounds.X + 8, e.CellBounds.Y, e.CellBounds.Width - 12, e.CellBounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                e.FormattedValue?.ToString() ?? "",
                DesignTokens.FontSection,
                textRect,
                DesignTokens.TextSecondary,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            e.Handled = true;
            return;
        }

        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (dgvAgents.Rows[e.RowIndex].Tag is not AgentInfo agent) return;

        string colName = dgvAgents.Columns[e.ColumnIndex].Name;

        if (colName == "colStatus")
        {
            e.PaintBackground(e.ClipBounds, true);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int pillY = e.CellBounds.Y + Math.Max(4, (e.CellBounds.Height - 24) / 2);
            Rectangle pillRect = new(e.CellBounds.X + 8, pillY, 78, 24);
            Color bg = agent.Online ? DesignTokens.StatusOnlineBg : DesignTokens.StatusOfflineBg;
            Color fg = agent.Online ? DesignTokens.StatusOnlineText : DesignTokens.StatusOfflineText;
            Color dot = agent.Online ? DesignTokens.StatusOnlineDot : DesignTokens.StatusOfflineDot;

            using (var path = GraphicsExtensions.CreateRoundedRect(pillRect, 12))
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);

            if (agent.Online)
            {
                using var dotBrush = new SolidBrush(dot);
                g.FillEllipse(dotBrush, pillRect.X + 10, pillRect.Y + 8, 8, 8);
            }
            else
            {
                using var dotPen = new Pen(dot, 1.6f);
                g.DrawEllipse(dotPen, pillRect.X + 10, pillRect.Y + 8, 8, 8);
            }

            TextRenderer.DrawText(
                g,
                agent.Online ? "Online" : "Offline",
                DesignTokens.FontBadge,
                new Rectangle(pillRect.X + 22, pillRect.Y, pillRect.Width - 24, pillRect.Height),
                fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);

            e.Handled = true;
            return;
        }

        if (colName is "colCpu" or "colRam" or "colDisk")
        {
            if (!agent.Online) return;

            int value = colName switch
            {
                "colCpu" => agent.Cpu,
                "colRam" => agent.Ram,
                _ => agent.Disk
            };
            int limit = colName switch
            {
                "colCpu" => CpuLimit,
                "colRam" => RamLimit,
                _ => DiskLimit
            };

            e.PaintBackground(e.ClipBounds, true);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int barH = Math.Min(22, Math.Max(14, e.CellBounds.Height - 16));
            int barY = e.CellBounds.Y + (e.CellBounds.Height - barH) / 2;
            Rectangle barBounds = new(
                e.CellBounds.X + 8,
                barY,
                Math.Max(0, e.CellBounds.Width - 16),
                barH);

            if (barBounds.Width > 20)
            {
                bool isDanger = value >= limit;
                bool isWarn = !isDanger && value >= limit - 15;

                Color trackColor = isDanger
                    ? DesignTokens.StatusDangerBg
                    : isWarn ? DesignTokens.StatusWarningBg : DesignTokens.MeterTrack;

                Color fillColor = isDanger
                    ? DesignTokens.StatusDangerBar
                    : isWarn ? DesignTokens.StatusWarningBar : DesignTokens.AccentPrimary;

                using (var trackPath = GraphicsExtensions.CreateRoundedRect(barBounds, 5))
                using (var trackBrush = new SolidBrush(trackColor))
                    g.FillPath(trackBrush, trackPath);

                int fillWidth = (int)Math.Round(barBounds.Width * (Math.Clamp(value, 0, 100) / 100.0));
                if (fillWidth > 4)
                {
                    Rectangle fillRect = new(barBounds.X, barBounds.Y, fillWidth, barBounds.Height);
                    using var fillPath = GraphicsExtensions.CreateRoundedRect(fillRect, 5);
                    using var fillBrush = new SolidBrush(fillColor);
                    g.FillPath(fillBrush, fillPath);
                }

                using (var borderPath = GraphicsExtensions.CreateRoundedRect(barBounds, 5))
                using (var borderPen = new Pen(isDanger ? DesignTokens.StatusDangerBar : DesignTokens.BorderStrong))
                    g.DrawPath(borderPen, borderPath);

                string label = isDanger ? $"{value}% [!]" : $"{value}%";
                Color textColor = fillWidth > barBounds.Width / 2
                    ? (isDanger || !DesignTokens.IsDark ? Color.White : DesignTokens.AccentForeground)
                    : DesignTokens.TextPrimary;

                TextRenderer.DrawText(
                    g, label,
                    isDanger ? DesignTokens.FontTabularBold : DesignTokens.FontTabular,
                    barBounds, textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }

            e.Handled = true;
        }
    }

    private void dgvAgents_Paint(object? sender, PaintEventArgs e)
    {
        bool anyVisible = dgvAgents.Rows.Cast<DataGridViewRow>().Any(r => r.Visible);
        if (anyVisible) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int cardW = Math.Min(560, dgvAgents.Width - 40);
        int cardH = 148;
        int cardX = (dgvAgents.Width - cardW) / 2;
        int cardY = Math.Max(55, (dgvAgents.Height - cardH) / 2);

        Rectangle cardRect = new(cardX, cardY, cardW, cardH);
        using (var path = GraphicsExtensions.CreateRoundedRect(cardRect, 10))
        using (var bgBrush = new SolidBrush(DesignTokens.SurfaceElevated))
        using (var borderPen = new Pen(DesignTokens.BorderStrong, 1))
        {
            g.FillPath(bgBrush, path);
            g.DrawPath(borderPen, path);
        }

        string title = _server.Agents.IsEmpty
            ? "Đang chờ máy trạm (Agent) trong mạng LAN kết nối..."
            : "Không có máy trạm nào khớp với bộ lọc hiện tại";

        string desc = _server.Agents.IsEmpty
            ? $"Khởi chạy LanMonitor.Agent trên các máy cần giám sát, hoặc bấm nút Bánh răng (⚙)\ngóc trên phải để mở Cài đặt và dùng thử chế độ Mô phỏng máy trạm ảo."
            : "Hãy xóa từ khóa tìm kiếm hoặc chuyển bộ lọc hiển thị về 'Tất cả máy'.";

        string cmdHint = _server.Agents.IsEmpty
            ? "Lệnh chạy Agent:  dotnet run --project LanMonitor.Agent"
            : "";

        TextRenderer.DrawText(
            g, title, DesignTokens.FontSection,
            new Rectangle(cardX + 20, cardY + 18, cardW - 40, 24),
            DesignTokens.TextPrimary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        TextRenderer.DrawText(
            g, desc, DesignTokens.FontBody,
            new Rectangle(cardX + 20, cardY + 46, cardW - 40, 44),
            DesignTokens.TextSecondary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        if (!string.IsNullOrEmpty(cmdHint))
        {
            Rectangle codeRect = new(cardX + 36, cardY + 98, cardW - 72, 32);
            using var codeBrush = new SolidBrush(DesignTokens.SurfaceSubtle);
            using var codePath = GraphicsExtensions.CreateRoundedRect(codeRect, 6);
            g.FillPath(codeBrush, codePath);

            TextRenderer.DrawText(
                g, cmdHint, DesignTokens.FontTabularBold,
                codeRect, DesignTokens.AccentPrimary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    private AgentInfo? GetSelectedAgent()
    {
        if (dgvAgents.SelectedRows.Count > 0 && dgvAgents.SelectedRows[0].Tag is AgentInfo selected)
            return selected;
        if (dgvAgents.Rows.Count > 0 && dgvAgents.Rows[0].Tag is AgentInfo first)
            return first;
        return null;
    }

    private void dgvAgents_SelectionChanged(object? sender, EventArgs e)
    {
        UpdateChartHeader();
        pnlChartCanvas.Invalidate();
    }

    private void dgvAgents_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.RowIndex < dgvAgents.Rows.Count)
        {
            dgvAgents.ClearSelection();
            dgvAgents.Rows[e.RowIndex].Selected = true;
        }
    }

    private void dgvAgents_CellMouseEnter(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex != _hoveredRowIndex)
        {
            _hoveredRowIndex = e.RowIndex;
            if (e.RowIndex >= 0) dgvAgents.InvalidateRow(e.RowIndex);
        }
    }

    private void dgvAgents_CellMouseLeave(object? sender, DataGridViewCellEventArgs e)
    {
        if (_hoveredRowIndex >= 0)
        {
            int prev = _hoveredRowIndex;
            _hoveredRowIndex = -1;
            if (prev < dgvAgents.Rows.Count) dgvAgents.InvalidateRow(prev);
        }
    }

    private void pnlFooter_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        using var topPen = new Pen(DesignTokens.BorderStrong, 1);
        g.DrawLine(topPen, 0, 0, pnlFooter.Width, 0);

        var online = _server.Agents.Values.Where(a => a.Online).ToList();
        string leftText;
        if (online.Count > 0)
        {
            double avgCpu = online.Average(a => a.Cpu);
            double avgRam = online.Average(a => a.Ram);
            var hottest = online.OrderByDescending(a => a.Cpu + a.Ram).First();
            leftText = $"Trung bình LAN ({online.Count} máy Online):  CPU {avgCpu:F0}%  ·  RAM {avgRam:F0}%   |   Tải cao nhất: {hottest.Name} (CPU {hottest.Cpu}%, RAM {hottest.Ram}%)";
        }
        else
        {
            leftText = "Chưa có máy trạm Online trong mạng LAN";
        }

        TextRenderer.DrawText(g, leftText, DesignTokens.FontLabel,
            new Rectangle(14, 0, pnlFooter.Width - 28, pnlFooter.Height),
            DesignTokens.TextSecondary,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    private void UpdateChartHeader()
    {
        var agent = GetSelectedAgent();
        lblChartTitle.Text = agent != null
            ? $"XU HƯỚNG CPU & RAM — {agent.Name.ToUpperInvariant()} ({agent.Ip})"
            : "BIỂU ĐỒ XU HƯỚNG CPU & RAM (THEO THỜI GIAN THỰC)";
    }

    private void pnlChartCanvas_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var w = pnlChartCanvas.Width;
        var h = pnlChartCanvas.Height;
        if (w < 100 || h < 80) return;

        var agent = GetSelectedAgent();
        if (agent == null)
        {
            TextRenderer.DrawText(
                g,
                "Chọn một máy trạm trên bảng để xem biểu đồ lịch sử CPU & RAM theo thời gian thực.",
                DesignTokens.FontBody,
                new Rectangle(16, 16, w - 32, h - 32),
                DesignTokens.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            return;
        }

        int padLeft = 44, padRight = 18, padTop = 34, padBottom = 24;
        Rectangle plot = new(padLeft, padTop, Math.Max(10, w - padLeft - padRight), Math.Max(10, h - padTop - padBottom));

        Color cpuColor = DesignTokens.AccentPrimary;
        Color ramColor = DesignTokens.ChartRam;

        using (var cpuBrush = new SolidBrush(cpuColor))
        using (var ramBrush = new SolidBrush(ramColor))
        {
            g.FillRectangle(cpuBrush, padLeft, 10, 12, 12);
            TextRenderer.DrawText(g, $"CPU: {agent.Cpu}% (Ngưỡng {CpuLimit}%)", DesignTokens.FontLabel,
                new Point(padLeft + 16, 8), DesignTokens.TextPrimary, TextFormatFlags.NoPrefix);

            int ramLegendX = padLeft + 195;
            g.FillRectangle(ramBrush, ramLegendX, 10, 12, 12);
            TextRenderer.DrawText(g, $"RAM: {agent.Ram}% (Ngưỡng {RamLimit}%)", DesignTokens.FontLabel,
                new Point(ramLegendX + 16, 8), DesignTokens.TextPrimary, TextFormatFlags.NoPrefix);
        }

        using var gridPen = new Pen(DesignTokens.BorderSubtle, 1);
        int[] ticks = { 0, 25, 50, 75, 100 };
        foreach (int t in ticks)
        {
            int y = plot.Bottom - (int)Math.Round(plot.Height * (t / 100.0));
            g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
            TextRenderer.DrawText(
                g, $"{t}%", DesignTokens.FontTabular,
                new Rectangle(2, y - 8, padLeft - 6, 16),
                DesignTokens.TextMuted,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        if (_settings.ShowThresholdLineOnChart)
        {
            int limitY = plot.Bottom - (int)Math.Round(plot.Height * (CpuLimit / 100.0));
            using var limitPen = new Pen(Color.FromArgb(190, DesignTokens.StatusDangerBar), 1.2f)
            {
                DashStyle = DashStyle.Dash
            };
            g.DrawLine(limitPen, plot.Left, limitY, plot.Right, limitY);
        }

        List<int> cpuPoints;
        List<int> ramPoints;
        lock (agent.CpuHistory)
        {
            cpuPoints = agent.CpuHistory.ToList();
            ramPoints = agent.RamHistory.ToList();
        }

        DrawSeries(g, plot, cpuPoints, cpuColor, fillAlpha: DesignTokens.IsDark ? 42 : 28);
        DrawSeries(g, plot, ramPoints, ramColor, fillAlpha: DesignTokens.IsDark ? 32 : 20);

        using var borderPen = new Pen(DesignTokens.BorderStrong, 1);
        g.DrawRectangle(borderPen, plot);
    }

    private static void DrawSeries(Graphics g, Rectangle plot, List<int> values, Color color, int fillAlpha)
    {
        if (values.Count == 0) return;

        int maxPoints = AgentInfo.MaxHistoryPoints;
        var pts = new List<PointF>(values.Count);

        for (int i = 0; i < values.Count; i++)
        {
            float x = values.Count == 1
                ? plot.Left + plot.Width / 2f
                : plot.Left + (float)i / (maxPoints - 1) * plot.Width;
            float y = plot.Bottom - (Math.Clamp(values[i], 0, 100) / 100f) * plot.Height;
            pts.Add(new PointF(x, y));
        }

        if (pts.Count >= 2)
        {
            using var areaPath = new GraphicsPath();
            areaPath.AddLines(pts.ToArray());
            areaPath.AddLine(pts[^1], new PointF(pts[^1].X, plot.Bottom));
            areaPath.AddLine(new PointF(pts[^1].X, plot.Bottom), new PointF(pts[0].X, plot.Bottom));
            areaPath.CloseFigure();

            using var areaBrush = new SolidBrush(Color.FromArgb(fillAlpha, color));
            g.FillPath(areaBrush, areaPath);

            using var linePen = new Pen(color, 2.2f);
            g.DrawLines(linePen, pts.ToArray());
        }

        var last = pts[^1];
        using var dotBrush = new SolidBrush(color);
        g.FillEllipse(dotBrush, last.X - 3.5f, last.Y - 3.5f, 7f, 7f);
    }

    private void lstLogs_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstLogs.Items.Count) return;

        e.DrawBackground();
        string text = lstLogs.Items[e.Index]?.ToString() ?? "";

        Color fg = DesignTokens.TextOnDark;
        if (text.Contains("[CẢNH BÁO]") || text.Contains("[Lỗi"))
            fg = Color.FromArgb(252, 165, 165);
        else if (text.Contains("[Timeout]") || text.Contains("[-]"))
            fg = Color.FromArgb(253, 224, 71);
        else if (text.Contains("[+]"))
            fg = Color.FromArgb(134, 239, 172);
        else if (text.Contains("[UDP]"))
            fg = Color.FromArgb(147, 197, 253);

        TextRenderer.DrawText(
            e.Graphics, text, DesignTokens.FontTabular,
            new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height),
            fg,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);

        e.DrawFocusRectangle();
    }

    // =========================================================================
    // SỰ KIỆN TƯƠNG TÁC, SETTINGS DRAWER, EXPORT & MÔ PHỎNG MÁY TRẠM
    // =========================================================================
    private void btnSettings_Click(object? sender, EventArgs e) =>
        ToggleSettingsDrawer(!settingsDrawer.Visible);

    private void ToggleSettingsDrawer(bool open)
    {
        settingsDrawer.AnimateOpen(open);
        btnSettings.Active = open;
    }

    private void btnConfigure_Click(object? sender, EventArgs e)
    {
        bool open = !btnConfigure.Active;
        btnConfigure.Active = open;
        LayoutHeaderAndToolbar();
        configurePopover.AnimateToggle(open);
    }

    private void btnToggleServer_Click(object? sender, EventArgs e)
    {
        if (_server.IsRunning)
            _server.Stop();
        else
            _server.Start();

        UpdateServerStatusUi();
    }

    private void UpdateServerStatusUi()
    {
        if (_server.IsRunning)
        {
            btnToggleServer.Text = "Dừng Server";
            btnToggleServer.Tone = ButtonTone.Danger;
        }
        else
        {
            btnToggleServer.Text = "Khởi động Server";
            btnToggleServer.Tone = ButtonTone.Primary;
        }
        btnToggleServer.Invalidate();
        pnlHeader.Invalidate();
    }

    private void btnRemoveOffline_Click(object? sender, EventArgs e)
    {
        var offlineKeys = _server.Agents.Values
            .Where(a => !a.Online)
            .Select(a => a.Key)
            .ToList();

        foreach (var key in offlineKeys)
        {
            _server.Agents.TryRemove(key, out _);
            if (_rowsByKey.TryGetValue(key, out var row))
            {
                dgvAgents.Rows.Remove(row);
                _rowsByKey.Remove(key);
            }
        }

        pnlHeader.Invalidate();
        pnlFooter.Invalidate();
        pnlChartCanvas.Invalidate();
        dgvAgents.Invalidate();
    }

    private void btnCompare_Click(object? sender, EventArgs e)
    {
        var sel = GetSelectedAgent();
        OpenCompareDialog(_compareKeyA ?? sel?.Key, _compareKeyA != null ? sel?.Key : null);
    }

    private void OpenCompareDialog(string? keyA, string? keyB)
    {
        if (_compareForm == null || _compareForm.IsDisposed)
        {
            _compareForm = new CompareForm(_server, _settings, keyA, keyB);
            _compareForm.Show(this);
        }
        else
        {
            _compareForm.SelectMachines(keyA, keyB);
            _compareForm.Activate();
        }
    }

    private void KickSelectedAgent(AgentInfo agent)
    {
        var result = MessageBox.Show(
            this,
            $"Bạn có chắc muốn Kick (ngắt kết nối) máy '{agent.Name}' ({agent.Ip}) không?",
            "Xác nhận Kick máy trạm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes) return;

        // Nếu là máy ảo Demo thì hủy luôn task gửi của máy đó để nó chuyển Offline hẳn
        if (_demoAgentCts.TryRemove(agent.Key, out var demoCts))
        {
            demoCts.Cancel();
            demoCts.Dispose();
        }

        _server.KickAgent(agent.Key);
        pnlHeader.Invalidate();
        pnlFooter.Invalidate();
    }

    private void RemoveSingleAgent(AgentInfo agent)
    {
        if (_demoAgentCts.TryRemove(agent.Key, out var demoCts))
        {
            demoCts.Cancel();
            demoCts.Dispose();
        }

        if (agent.Online)
            _server.KickAgent(agent.Key);

        _server.Agents.TryRemove(agent.Key, out _);
        if (_rowsByKey.TryGetValue(agent.Key, out var row))
        {
            dgvAgents.Rows.Remove(row);
            _rowsByKey.Remove(agent.Key);
        }

        pnlHeader.Invalidate();
        pnlFooter.Invalidate();
        pnlChartCanvas.Invalidate();
        dgvAgents.Invalidate();
    }

    private async Task PingAgentAsync(AgentInfo agent)
    {
        try
        {
            using var ping = new Ping();
            string target = agent.Ip == "unknown" ? "127.0.0.1" : agent.Ip;
            var reply = await ping.SendPingAsync(target, 1500);

            if (reply.Status == IPStatus.Success)
            {
                agent.LastPingMs = reply.RoundtripTime;
                OnLogMessage($"[{DateTime.Now:HH:mm:ss}] [Ping] '{agent.Name}' ({target}) phản hồi: {reply.RoundtripTime} ms (TTL={reply.Options?.Ttl ?? 0}).");
            }
            else
            {
                agent.LastPingMs = null;
                OnLogMessage($"[{DateTime.Now:HH:mm:ss}] [Ping] '{agent.Name}' ({target}) thất bại: {reply.Status}.");
            }
        }
        catch (Exception ex)
        {
            OnLogMessage($"[{DateTime.Now:HH:mm:ss}] [Ping] Lỗi kiểm tra '{agent.Name}': {ex.Message}");
        }
    }

    private void btnClearLog_Click(object? sender, EventArgs e) =>
        lstLogs.Items.Clear();

    private void Filter_Changed(object? sender, EventArgs e) =>
        ApplyFilter();

    private void timerWatchdog_Tick(object? sender, EventArgs e)
    {
        if (!_server.IsRunning) return;
        int timeoutSeconds = Math.Max(8, _settings.ReportIntervalSeconds * _settings.OfflineTimeoutMultiplier + 2);
        _server.CheckTimeouts(timeoutSeconds);
    }

    private void ExportAgentsToCsv()
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "CSV File (*.csv)|*.csv",
            FileName = $"LanMonitor_Agents_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };
        if (sfd.ShowDialog(this) != DialogResult.OK) return;

        var sb = new StringBuilder();
        sb.AppendLine("TrangThai,TenMay,DiaChiIP,HeDieuHanh,CPU_%,RAM_%,Disk_%,CanhBao,CapNhatLuc");
        foreach (var a in _server.Agents.Values.OrderByDescending(x => x.Online).ThenBy(x => x.Name))
        {
            string alert = a.GetAlertText(CpuLimit, RamLimit, DiskLimit).Replace(",", ";");
            string os = a.Os.Replace(",", " ");
            sb.AppendLine($"{(a.Online ? "Online" : "Offline")},{a.Name},{a.Ip},{os},{a.Cpu},{a.Ram},{a.Disk},{alert},{a.LastSeen:yyyy-MM-dd HH:mm:ss}");
        }

        File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        OnLogMessage($"[{DateTime.Now:HH:mm:ss}] [Xuất dữ liệu] Đã lưu báo cáo CSV tại: {sfd.FileName}");
    }

    private void ExportLogsToTxt()
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "Text File (*.txt)|*.txt",
            FileName = $"LanMonitor_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };
        if (sfd.ShowDialog(this) != DialogResult.OK) return;

        var lines = lstLogs.Items.Cast<object>().Select(x => x.ToString() ?? "");
        File.WriteAllLines(sfd.FileName, lines, Encoding.UTF8);
        OnLogMessage($"[{DateTime.Now:HH:mm:ss}] [Xuất dữ liệu] Đã lưu nhật ký sự kiện tại: {sfd.FileName}");
    }

    // Mở cửa sổ cấu hình mô phỏng máy trạm ảo và khởi chạy theo tùy chọn người dùng
    private void ToggleDemoAgents()
    {
        using var dlg = new DemoSimulatorForm(_demoConfig, _demoCts != null);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        StopAllDemoAgents(silent: dlg.StopRequested == false);

        if (dlg.StopRequested)
            return;

        _demoConfig = dlg.ResultConfig;
        StartDemoSimulation(_demoConfig);
    }

    private void StopAllDemoAgents(bool silent)
    {
        if (_demoCts == null) return;

        _demoCts.Cancel();
        _demoCts.Dispose();
        _demoCts = null;
        foreach (var kv in _demoAgentCts)
            kv.Value.Dispose();
        _demoAgentCts.Clear();

        settingsDrawer.SetDemoActive(false);
        if (!silent)
            OnLogMessage($"[{DateTime.Now:HH:mm:ss}] [Demo] Đã dừng toàn bộ máy trạm ảo.");
    }

    private void StartDemoSimulation(DemoSimulationConfig cfg)
    {
        if (!_server.IsRunning)
        {
            _server.Start();
            UpdateServerStatusUi();
        }

        _demoCts = new CancellationTokenSource();
        settingsDrawer.SetDemoActive(true);
        OnLogMessage($"[{DateTime.Now:HH:mm:ss}] [Demo] Đang khởi tạo {cfg.MachineCount} máy trạm ảo ({cfg.NamePrefix}-01..{cfg.MachineCount:D2})...");

        var rng = new Random();
        string[] winVersions = { "Windows 11 Pro 24H2", "Windows 11 Enterprise", "Windows 10 Pro" };
        string[] linuxVersions = { "Ubuntu 24.04 LTS", "Debian 12 Bookworm", "Rocky Linux 9" };

        for (int i = 1; i <= cfg.MachineCount; i++)
        {
            string name = $"{cfg.NamePrefix}-{i:D2}";
            string ip = $"192.168.1.{100 + i}";
            string os = cfg.OsChoice switch
            {
                DemoOsChoice.Windows11 => winVersions[i % 2],
                DemoOsChoice.Ubuntu => linuxVersions[i % linuxVersions.Length],
                _ => (i % 3 == 0) ? linuxVersions[i % linuxVersions.Length] : winVersions[i % winVersions.Length]
            };

            (int baseCpu, int baseRam, int baseDisk) = cfg.Profile switch
            {
                DemoLoadProfile.Normal => (rng.Next(15, 42), rng.Next(30, 58), rng.Next(35, 65)),
                DemoLoadProfile.Overload => (rng.Next(84, 96), rng.Next(86, 97), rng.Next(88, 96)),
                DemoLoadProfile.Fluctuating => (rng.Next(25, 78), rng.Next(40, 82), rng.Next(40, 75)),
                _ => (i % 3 == 0)
                    ? (rng.Next(82, 95), rng.Next(84, 96), rng.Next(85, 95))
                    : (rng.Next(18, 52), rng.Next(35, 68), rng.Next(35, 72))
            };

            int jitter = cfg.Profile == DemoLoadProfile.Fluctuating ? 28 : 10;
            string key = $"{name}@{ip}";
            var linked = CancellationTokenSource.CreateLinkedTokenSource(_demoCts.Token);
            _demoAgentCts[key] = linked;
            _ = RunDemoAgentAsync(name, ip, os, baseCpu, baseRam, baseDisk, jitter, linked.Token);
        }

        if (cfg.SimulateRandomDrop)
            _ = RunRandomDropLoopAsync(_demoCts.Token);
    }

    private async Task RunRandomDropLoopAsync(CancellationToken ct)
    {
        var rng = new Random();
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(15000, ct);
                var keys = _demoAgentCts.Keys.ToList();
                if (keys.Count == 0) break;

                string pick = keys[rng.Next(keys.Count)];
                if (_demoAgentCts.TryRemove(pick, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                    OnLogMessage($"[{DateTime.Now:HH:mm:ss}] [Demo] Giả lập rớt mạng ngẫu nhiên cho '{pick}'.");
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private static async Task RunDemoAgentAsync(
        string name, string ip, string os,
        int baseCpu, int baseRam, int baseDisk, int jitter,
        CancellationToken ct)
    {
        var rng = new Random(HashCode.Combine(name, Environment.TickCount));
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", Protocol.TcpPort, ct);
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

            await writer.WriteAsync(Protocol.Build("HELLO", ("name", name), ("ip", ip), ("os", os)));

            int interval = 3;
            string? line = await reader.ReadLineAsync(ct);
            if (line != null)
            {
                var (cmd, data) = Protocol.Parse(line);
                if (cmd == "WELCOME" && data.TryGetValue("interval", out var s) && int.TryParse(s, out int n) && n > 0)
                    interval = n;
            }

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    int cpu = Math.Clamp(baseCpu + rng.Next(-jitter, jitter + 2), 3, 99);
                    int ram = Math.Clamp(baseRam + rng.Next(-6, 8), 10, 99);
                    int disk = Math.Clamp(baseDisk + rng.Next(-1, 2), 10, 99);

                    await writer.WriteAsync(Protocol.Build("STATS",
                        ("cpu", cpu.ToString()),
                        ("ram", ram.ToString()),
                        ("disk", disk.ToString())));

                    await Task.Delay(interval * 1000, ct);
                }
            }
            finally
            {
                try { await writer.WriteAsync(Protocol.Build("BYE")); } catch { }
            }
        }
        catch { }
    }
}
