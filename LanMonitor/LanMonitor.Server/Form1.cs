using System.Drawing.Drawing2D;
using System.Reflection;
using LanMonitor.Shared;

namespace LanMonitor.Server;

public partial class Form1 : Form
{
    private readonly MonitorServer _server = new();
    private readonly Dictionary<string, DataGridViewRow> _rowsByKey = new();

    private int CpuLimit => (int)numCpuLimit.Value;
    private int RamLimit => (int)numRamLimit.Value;
    private int DiskLimit => (int)numDiskLimit.Value;

    public Form1()
    {
        InitializeComponent();

        // Bật DoubleBuffered cho các control vẽ đồ họa thời gian thực để chống nhấp nháy (60fps)
        EnableDoubleBuffering(dgvAgents);
        EnableDoubleBuffering(pnlHeader);
        EnableDoubleBuffering(pnlChartCanvas);
        EnableDoubleBuffering(lstLogs);

        _server.AgentUpdated += OnAgentUpdated;
        _server.LogMessage += OnLogMessage;
    }

    private static void EnableDoubleBuffering(Control ctrl)
    {
        typeof(Control).InvokeMember(
            "DoubleBuffered",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
            null, ctrl, new object[] { true });
    }

    private void Form1_Load(object? sender, EventArgs e)
    {
        LayoutHeaderAndToolbar();
        if (splitBottom.Width > 200)
            splitBottom.SplitterDistance = (int)(splitBottom.Width * 0.48);

        _server.ReportIntervalSeconds = (int)numInterval.Value;
        _server.Start();
        UpdateServerStatusUi();
    }

    // Căn chỉnh chính xác vị trí các nút và ô cấu hình ngưỡng theo chiều rộng thực tế của cửa sổ
    private void LayoutHeaderAndToolbar()
    {
        int hw = pnlHeader.ClientSize.Width;
        if (hw > 300)
        {
            btnToggleServer.Location = new Point(hw - btnToggleServer.Width - 16, 13);
            btnRemoveOffline.Location = new Point(btnToggleServer.Left - btnRemoveOffline.Width - 10, 13);
        }

        int tw = pnlToolbar.ClientSize.Width;
        if (tw > 600)
        {
            int right = tw - 16;

            numInterval.Location = new Point(right - numInterval.Width, 12);
            right = numInterval.Left - 6;
            lblInterval.Location = new Point(right - lblInterval.PreferredWidth, 15);
            right = lblInterval.Left - 14;

            numDiskLimit.Location = new Point(right - numDiskLimit.Width, 12);
            right = numDiskLimit.Left - 6;
            lblDiskLimit.Location = new Point(right - lblDiskLimit.PreferredWidth, 15);
            right = lblDiskLimit.Left - 14;

            numRamLimit.Location = new Point(right - numRamLimit.Width, 12);
            right = numRamLimit.Left - 6;
            lblRamLimit.Location = new Point(right - lblRamLimit.PreferredWidth, 15);
            right = lblRamLimit.Left - 14;

            numCpuLimit.Location = new Point(right - numCpuLimit.Width, 12);
            right = numCpuLimit.Left - 6;
            lblCpuLimit.Location = new Point(right - lblCpuLimit.PreferredWidth, 15);
        }
    }

    private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        timerWatchdog.Stop();
        _server.Stop();
    }

    // Nhận sự kiện từ luồng mạng và đồng bộ về UI thread
    private void OnAgentUpdated(AgentInfo agent)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(() => OnAgentUpdated(agent)); }
            catch (ObjectDisposedException) { }
            return;
        }

        UpdateOrCreateRow(agent);
        CheckAndLogThresholdAlert(agent);
        ApplyFilter();
        pnlHeader.Invalidate();

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

    // Cập nhật hoặc thêm mới 1 dòng trên bảng Dashboard
    private void UpdateOrCreateRow(AgentInfo agent)
    {
        if (!_rowsByKey.TryGetValue(agent.Key, out var row))
        {
            int idx = dgvAgents.Rows.Add();
            row = dgvAgents.Rows[idx];
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

        // Sử dụng шрифт Tabular cho cột IP và thời gian (quy tắc number-tabular)
        row.Cells["colIp"].Style.Font = DesignTokens.FontTabular;
        row.Cells["colLastSeen"].Style.Font = DesignTokens.FontTabular;
        row.Cells["colName"].Style.Font = DesignTokens.FontBodyStrong;

        if (!agent.Online)
        {
            row.DefaultCellStyle.BackColor = DesignTokens.SurfaceSubtle;
            row.DefaultCellStyle.ForeColor = DesignTokens.TextMuted;
            row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(226, 232, 240);
            row.DefaultCellStyle.SelectionForeColor = DesignTokens.TextSecondary;
            row.Cells["colAlert"].Style.ForeColor = DesignTokens.TextMuted;
            row.Cells["colAlert"].Style.Font = DesignTokens.FontBody;
        }
        else if (isAlert)
        {
            row.DefaultCellStyle.BackColor = DesignTokens.StatusDangerRowBg;
            row.DefaultCellStyle.ForeColor = DesignTokens.TextPrimary;
            row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 226, 226);
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

    // Lọc danh sách máy theo ô Tìm kiếm và ComboBox trạng thái
    private void ApplyFilter()
    {
        string query = txtSearch.Text.Trim();
        int mode = cboFilter.SelectedIndex; // 0: Tất cả, 1: Online, 2: Vượt ngưỡng, 3: Offline

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
        }
        agent.WasAlerting = isAlert;
    }

    // =========================================================================
    // 1. VẼ THANH COMMAND HEADER (NOC TELEMETRY RIBBON)
    // =========================================================================
    private void pnlHeader_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Vẽ biểu tượng mạng Vector (3 nút kết nối) thay vì dùng emoji
        Rectangle iconRect = new(16, 15, 30, 30);
        using (var accentBrush = new SolidBrush(DesignTokens.AccentPrimary))
        using (var pen = new Pen(Color.White, 1.8f))
        {
            using var path = CreateRoundedRect(iconRect, 7);
            g.FillPath(accentBrush, path);

            // Vẽ 3 đỉnh mạng bên trong icon
            Point pTop = new(iconRect.X + 15, iconRect.Y + 8);
            Point pLeft = new(iconRect.X + 8, iconRect.Y + 21);
            Point pRight = new(iconRect.X + 22, iconRect.Y + 21);
            g.DrawLine(pen, pTop, pLeft);
            g.DrawLine(pen, pTop, pRight);
            g.DrawLine(pen, pLeft, pRight);
            g.FillEllipse(Brushes.White, pTop.X - 2, pTop.Y - 2, 5, 5);
            g.FillEllipse(Brushes.White, pLeft.X - 2, pLeft.Y - 2, 5, 5);
            g.FillEllipse(Brushes.White, pRight.X - 2, pRight.Y - 2, 5, 5);
        }

        // Tiêu đề hệ thống
        TextRenderer.DrawText(
            g, "LAN MONITOR",
            DesignTokens.FontHeading,
            new Point(54, 10),
            DesignTokens.TextOnDark);

        // Huy hiệu trạng thái Server (Pill Badge)
        bool running = _server.IsRunning;
        string statusText = running
            ? $"ĐANG LẮNG NGHE · TCP {Protocol.TcpPort} · UDP {Protocol.UdpPort}"
            : "SERVER ĐÃ TẠM DỪNG";

        Color dotColor = running ? Color.FromArgb(34, 197, 94) : DesignTokens.StatusOfflineDot;
        g.FillEllipse(new SolidBrush(dotColor), 58, 37, 8, 8);
        TextRenderer.DrawText(
            g, statusText,
            DesignTokens.FontLabel,
            new Point(70, 33),
            running ? Color.FromArgb(134, 239, 172) : DesignTokens.TextOnDarkMuted);

        // Dải thống kê Inline Telemetry (Tabular Numerals)
        var all = _server.Agents.Values.ToList();
        int total = all.Count;
        int online = all.Count(a => a.Online);
        int offline = total - online;
        int alerting = all.Count(a => a.IsAlerting(CpuLimit, RamLimit, DiskLimit));

        int startX = 360;
        DrawTelemetryMetric(g, ref startX, "TỔNG MÁY", total.ToString(), DesignTokens.TextOnDark);
        DrawTelemetryMetric(g, ref startX, "ONLINE", online.ToString(), Color.FromArgb(74, 222, 128));
        DrawTelemetryMetric(g, ref startX, "OFFLINE", offline.ToString(), DesignTokens.TextOnDarkMuted);
        DrawTelemetryMetric(g, ref startX, "VƯỢT NGƯỠNG", alerting.ToString(),
            alerting > 0 ? Color.FromArgb(248, 113, 113) : DesignTokens.TextOnDarkMuted);
    }

    private static void DrawTelemetryMetric(Graphics g, ref int x, string label, string value, Color valueColor)
    {
        using var divPen = new Pen(DesignTokens.BorderDark, 1);
        g.DrawLine(divPen, x, 14, x, 46);
        x += 16;

        TextRenderer.DrawText(g, label, DesignTokens.FontLabel, new Point(x, 12), DesignTokens.TextOnDarkMuted);
        TextRenderer.DrawText(g, value, DesignTokens.FontHeading, new Point(x, 27), valueColor);
        x += 105;
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

    // =========================================================================
    // 2. VẼ BẢNG DỮ LIỆU & EMPTY STATE (OPERATE MODE + WCAG COLOR-NOT-ONLY)
    // =========================================================================
    private void dgvAgents_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.Graphics == null) return;

        // Vẽ Header bảng với đường kẻ phân cách tinh tế
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
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

            e.Handled = true;
            return;
        }

        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (dgvAgents.Rows[e.RowIndex].Tag is not AgentInfo agent) return;

        string colName = dgvAgents.Columns[e.ColumnIndex].Name;

        // A. Vẽ Huy hiệu Trạng thái (Status Pill Badge có chấm Vector)
        if (colName == "colStatus")
        {
            e.PaintBackground(e.ClipBounds, true);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle pillRect = new(e.CellBounds.X + 8, e.CellBounds.Y + 9, 78, 24);
            Color bg = agent.Online ? DesignTokens.StatusOnlineBg : DesignTokens.StatusOfflineBg;
            Color fg = agent.Online ? DesignTokens.StatusOnlineText : DesignTokens.StatusOfflineText;
            Color dot = agent.Online ? DesignTokens.StatusOnlineDot : DesignTokens.StatusOfflineDot;

            using (var path = CreateRoundedRect(pillRect, 12))
            using (var brush = new SolidBrush(bg))
            {
                g.FillPath(brush, path);
            }

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
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            e.Handled = true;
            return;
        }

        // B. Vẽ thanh đo mức sử dụng CPU / RAM / Disk
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

            Rectangle barBounds = new(
                e.CellBounds.X + 8,
                e.CellBounds.Y + 9,
                Math.Max(0, e.CellBounds.Width - 16),
                Math.Max(0, e.CellBounds.Height - 18));

            if (barBounds.Width > 20 && barBounds.Height > 8)
            {
                bool isDanger = value >= limit;
                bool isWarn = !isDanger && value >= limit - 15;

                Color trackColor = isDanger
                    ? DesignTokens.StatusDangerBg
                    : isWarn ? DesignTokens.StatusWarningBg : DesignTokens.SurfaceSubtle;

                Color fillColor = isDanger
                    ? DesignTokens.StatusDangerBar
                    : isWarn ? DesignTokens.StatusWarningBar : DesignTokens.AccentPrimary;

                using (var trackPath = CreateRoundedRect(barBounds, 5))
                using (var trackBrush = new SolidBrush(trackColor))
                {
                    g.FillPath(trackBrush, trackPath);
                }

                int fillWidth = (int)Math.Round(barBounds.Width * (Math.Clamp(value, 0, 100) / 100.0));
                if (fillWidth > 4)
                {
                    Rectangle fillRect = new(barBounds.X, barBounds.Y, fillWidth, barBounds.Height);
                    using var fillPath = CreateRoundedRect(fillRect, 5);
                    using var fillBrush = new SolidBrush(fillColor);
                    g.FillPath(fillBrush, fillPath);
                }

                using (var borderPath = CreateRoundedRect(barBounds, 5))
                using (var borderPen = new Pen(isDanger ? Color.FromArgb(252, 165, 165) : DesignTokens.BorderStrong))
                {
                    g.DrawPath(borderPen, borderPath);
                }

                // Thêm ký hiệu [!] khi vượt ngưỡng để không chỉ phụ thuộc vào màu sắc (quy tắc color-not-only)
                string label = isDanger ? $"{value}% [!]" : $"{value}%";
                Color textColor = fillWidth > barBounds.Width / 2 ? Color.White : DesignTokens.TextPrimary;

                TextRenderer.DrawText(
                    g,
                    label,
                    isDanger ? DesignTokens.FontTabularBold : DesignTokens.FontTabular,
                    barBounds,
                    textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            e.Handled = true;
        }
    }

    // Vẽ Empty State hướng dẫn rõ ràng khi chưa có máy nào kết nối
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
        using (var path = CreateRoundedRect(cardRect, 10))
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
            ? $"Khởi chạy LanMonitor.Agent trên các máy cần giám sát. Agent sẽ tự động tìm Server\nqua UDP Broadcast (cổng {Protocol.UdpPort}) và gửi thông số về cổng TCP {Protocol.TcpPort}."
            : "Hãy xóa từ khóa tìm kiếm hoặc chuyển bộ lọc hiển thị về 'Tất cả máy'.";

        string cmdHint = _server.Agents.IsEmpty
            ? "Lệnh chạy thử:  dotnet run --project LanMonitor.Agent"
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
            using var codePath = CreateRoundedRect(codeRect, 6);
            g.FillPath(codeBrush, codePath);

            TextRenderer.DrawText(
                g, cmdHint, DesignTokens.FontTabularBold,
                codeRect, DesignTokens.AccentPrimary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    // =========================================================================
    // 3. BIỂU ĐỒ XU HƯỚNG THỜI GIAN THỰC (CHART & DATA RULES)
    // =========================================================================
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

        int padLeft = 44, padRight = 18, padTop = 34, padBottom = 26;
        Rectangle plot = new(padLeft, padTop, Math.Max(10, w - padLeft - padRight), Math.Max(10, h - padTop - padBottom));

        // Vẽ chú giải (Legend) phía trên biểu đồ (quy tắc legend-visible)
        Color cpuColor = DesignTokens.AccentPrimary;
        Color ramColor = Color.FromArgb(16, 185, 129); // Emerald 500

        using (var cpuBrush = new SolidBrush(cpuColor))
        using (var ramBrush = new SolidBrush(ramColor))
        {
            g.FillRectangle(cpuBrush, padLeft, 10, 12, 12);
            TextRenderer.DrawText(g, $"CPU hiện tại: {agent.Cpu}% (Ngưỡng {CpuLimit}%)", DesignTokens.FontLabel, new Point(padLeft + 16, 8), DesignTokens.TextPrimary);

            int ramLegendX = padLeft + 210;
            g.FillRectangle(ramBrush, ramLegendX, 10, 12, 12);
            TextRenderer.DrawText(g, $"RAM hiện tại: {agent.Ram}% (Ngưỡng {RamLimit}%)", DesignTokens.FontLabel, new Point(ramLegendX + 16, 8), DesignTokens.TextPrimary);
        }

        // Vẽ lưới ngang (gridline-subtle) và nhãn trục Y (axis-labels)
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
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }

        // Đường ngưỡng cảnh báo CPU (nét đứt)
        int limitY = plot.Bottom - (int)Math.Round(plot.Height * (CpuLimit / 100.0));
        using (var limitPen = new Pen(Color.FromArgb(180, 220, 38, 38), 1) { DashStyle = DashStyle.Dash })
        {
            g.DrawLine(limitPen, plot.Left, limitY, plot.Right, limitY);
        }

        List<int> cpuPoints;
        List<int> ramPoints;
        lock (agent.CpuHistory)
        {
            cpuPoints = agent.CpuHistory.ToList();
            ramPoints = agent.RamHistory.ToList();
        }

        DrawSeries(g, plot, cpuPoints, cpuColor, fillAlpha: 28);
        DrawSeries(g, plot, ramPoints, ramColor, fillAlpha: 20);

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

        // Vẽ điểm mẫu mới nhất
        var last = pts[^1];
        using var dotBrush = new SolidBrush(color);
        g.FillEllipse(dotBrush, last.X - 3.5f, last.Y - 3.5f, 7f, 7f);
    }

    // =========================================================================
    // 4. VẼ NHẬT KÝ SỰ KIỆN MẠNG VỚI PHÂN LOẠI MÀU NGỮ NGHĨA
    // =========================================================================
    private void lstLogs_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= lstLogs.Items.Count) return;

        e.DrawBackground();
        string text = lstLogs.Items[e.Index]?.ToString() ?? "";

        Color fg = DesignTokens.TextOnDark;
        if (text.Contains("[CẢNH BÁO]") || text.Contains("[Lỗi"))
            fg = Color.FromArgb(252, 165, 165); // Red 300
        else if (text.Contains("[Timeout]") || text.Contains("[-]"))
            fg = Color.FromArgb(253, 224, 71);  // Yellow 300
        else if (text.Contains("[+]"))
            fg = Color.FromArgb(134, 239, 172); // Green 300
        else if (text.Contains("[UDP]"))
            fg = Color.FromArgb(147, 197, 253); // Blue 300

        TextRenderer.DrawText(
            e.Graphics,
            text,
            DesignTokens.FontTabular,
            new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height),
            fg,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);

        e.DrawFocusRectangle();
    }

    // =========================================================================
    // 5. SỰ KIỆN ĐIỀU KHIỂN
    // =========================================================================
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
            btnToggleServer.BackColor = DesignTokens.StatusDangerBar;
        }
        else
        {
            btnToggleServer.Text = "Khởi động Server";
            btnToggleServer.BackColor = DesignTokens.StatusOnlineDot;
        }
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
        pnlChartCanvas.Invalidate();
        dgvAgents.Invalidate();
    }

    private void btnClearLog_Click(object? sender, EventArgs e) =>
        lstLogs.Items.Clear();

    private void Filter_Changed(object? sender, EventArgs e) =>
        ApplyFilter();

    private void Threshold_ValueChanged(object? sender, EventArgs e)
    {
        foreach (var agent in _server.Agents.Values)
            UpdateOrCreateRow(agent);

        ApplyFilter();
        pnlHeader.Invalidate();
        pnlChartCanvas.Invalidate();
    }

    private void numInterval_ValueChanged(object? sender, EventArgs e)
    {
        _server.ReportIntervalSeconds = (int)numInterval.Value;
    }

    private void timerWatchdog_Tick(object? sender, EventArgs e)
    {
        if (!_server.IsRunning) return;
        int timeoutSeconds = Math.Max(10, _server.ReportIntervalSeconds * 3 + 2);
        _server.CheckTimeouts(timeoutSeconds);
    }

    private static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        if (radius <= 0 || bounds.Width < d || bounds.Height < d)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
