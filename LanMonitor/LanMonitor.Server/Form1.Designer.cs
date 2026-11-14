namespace LanMonitor.Server;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    // 1. Top Command Header
    private Panel pnlHeader;
    private ModernButton btnToggleServer;
    private ModernButton btnRemoveOffline;
    private GearButton btnSettings;

    // 2. Secondary Filter & Configure Bar
    private Panel pnlToolbar;
    private ModernSearchBox searchBox;
    private SegmentedControl segFilter;
    private ModernButton btnCompare;
    private ConfigureButton btnConfigure;
    private ConfigurePopover configurePopover;

    // 3. Settings Drawer (Slide-out Right Panel)
    private SettingsDrawer settingsDrawer;

    // 4. Main Split Workspace
    private SplitContainer splitMain;
    private DataGridView dgvAgents;
    private DataGridViewTextBoxColumn colStatus;
    private DataGridViewTextBoxColumn colName;
    private DataGridViewTextBoxColumn colIp;
    private DataGridViewTextBoxColumn colOs;
    private DataGridViewTextBoxColumn colCpu;
    private DataGridViewTextBoxColumn colRam;
    private DataGridViewTextBoxColumn colDisk;
    private DataGridViewTextBoxColumn colAlert;
    private DataGridViewTextBoxColumn colLastSeen;

    // 5. Bottom Split: Live Trend Chart (Left) + Event Stream Console (Right)
    private SplitContainer splitBottom;
    private Panel pnlChartContainer;
    private Panel pnlChartHeader;
    private Label lblChartTitle;
    private Panel pnlChartCanvas;

    private Panel pnlLogHeader;
    private Label lblLogTitle;
    private ModernButton btnClearLog;
    private ListBox lstLogs;

    private System.Windows.Forms.Timer timerWatchdog;
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;
    private ContextMenuStrip agentMenu;
    private Panel pnlFooter;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        pnlHeader = new Panel();
        btnToggleServer = new ModernButton();
        btnRemoveOffline = new ModernButton();
        btnSettings = new GearButton();

        pnlToolbar = new Panel();
        searchBox = new ModernSearchBox();
        segFilter = new SegmentedControl();
        btnCompare = new ModernButton();
        btnConfigure = new ConfigureButton();
        configurePopover = new ConfigurePopover();

        settingsDrawer = new SettingsDrawer();

        splitMain = new SplitContainer();
        dgvAgents = new DataGridView();
        colStatus = new DataGridViewTextBoxColumn();
        colName = new DataGridViewTextBoxColumn();
        colIp = new DataGridViewTextBoxColumn();
        colOs = new DataGridViewTextBoxColumn();
        colCpu = new DataGridViewTextBoxColumn();
        colRam = new DataGridViewTextBoxColumn();
        colDisk = new DataGridViewTextBoxColumn();
        colAlert = new DataGridViewTextBoxColumn();
        colLastSeen = new DataGridViewTextBoxColumn();

        splitBottom = new SplitContainer();
        pnlChartContainer = new Panel();
        pnlChartHeader = new Panel();
        lblChartTitle = new Label();
        pnlChartCanvas = new Panel();

        pnlLogHeader = new Panel();
        lblLogTitle = new Label();
        btnClearLog = new ModernButton();
        lstLogs = new ListBox();

        timerWatchdog = new System.Windows.Forms.Timer(components);
        trayMenu = new ContextMenuStrip(components);
        trayIcon = new NotifyIcon(components);
        agentMenu = new ContextMenuStrip(components);
        pnlFooter = new Panel();

        pnlHeader.SuspendLayout();
        pnlToolbar.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
        splitMain.Panel1.SuspendLayout();
        splitMain.Panel2.SuspendLayout();
        splitMain.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvAgents).BeginInit();
        ((System.ComponentModel.ISupportInitialize)splitBottom).BeginInit();
        splitBottom.Panel1.SuspendLayout();
        splitBottom.Panel2.SuspendLayout();
        splitBottom.SuspendLayout();
        pnlChartContainer.SuspendLayout();
        pnlChartHeader.SuspendLayout();
        pnlLogHeader.SuspendLayout();
        SuspendLayout();

        // 
        // pnlHeader (Top Command Ribbon)
        // 
        pnlHeader.BackColor = DesignTokens.SurfaceHeader;
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Size = new Size(1220, 62);
        pnlHeader.Paint += pnlHeader_Paint;
        pnlHeader.Resize += (_, _) => LayoutHeaderAndToolbar();

        btnRemoveOffline.Tone = ButtonTone.HeaderGhost;
        btnRemoveOffline.Size = new Size(134, 34);
        btnRemoveOffline.Text = "Dọn máy Offline";
        btnRemoveOffline.Click += btnRemoveOffline_Click;

        btnToggleServer.Tone = ButtonTone.Danger;
        btnToggleServer.Size = new Size(136, 34);
        btnToggleServer.Text = "Dừng Server";
        btnToggleServer.Click += btnToggleServer_Click;

        btnSettings.Size = new Size(36, 34);
        btnSettings.Click += btnSettings_Click;

        pnlHeader.Controls.Add(btnRemoveOffline);
        pnlHeader.Controls.Add(btnToggleServer);
        pnlHeader.Controls.Add(btnSettings);

        // 
        // pnlToolbar (Filter & Configure Bar)
        // 
        pnlToolbar.BackColor = DesignTokens.SurfaceSubtle;
        pnlToolbar.Dock = DockStyle.Top;
        pnlToolbar.Size = new Size(1220, 48);
        pnlToolbar.Paint += pnlToolbar_Paint;
        pnlToolbar.Resize += (_, _) => LayoutHeaderAndToolbar();

        searchBox.Location = new Point(16, 8);
        searchBox.Size = new Size(215, 32);
        searchBox.SearchTextChanged += Filter_Changed;

        segFilter.Items = new[] { "Tất cả", "Online", "Cảnh báo", "Offline" };
        segFilter.Location = new Point(244, 8);
        segFilter.Size = new Size(310, 32);
        segFilter.SetSelectedIndexImmediate(0);
        segFilter.SelectedIndexChanged += Filter_Changed;

        btnCompare.Tone = ButtonTone.Secondary;
        btnCompare.Radius = 7;
        btnCompare.Font = DesignTokens.FontBodyStrong;
        btnCompare.Location = new Point(568, 8);
        btnCompare.Size = new Size(134, 32);
        btnCompare.Text = "⇄ So sánh 2 máy";
        btnCompare.Click += btnCompare_Click;

        btnConfigure.Location = new Point(1080, 9);
        btnConfigure.Size = new Size(124, 30);
        btnConfigure.Text = "Configure";
        btnConfigure.Click += btnConfigure_Click;

        pnlToolbar.Controls.Add(searchBox);
        pnlToolbar.Controls.Add(segFilter);
        pnlToolbar.Controls.Add(btnCompare);
        pnlToolbar.Controls.Add(btnConfigure);

        // 
        // splitMain
        // 
        splitMain.Dock = DockStyle.Fill;
        splitMain.Orientation = Orientation.Horizontal;
        splitMain.Size = new Size(1220, 610);
        splitMain.SplitterDistance = 370;
        splitMain.SplitterWidth = 4;

        // 
        // dgvAgents
        // 
        dgvAgents.AllowUserToAddRows = false;
        dgvAgents.AllowUserToDeleteRows = false;
        dgvAgents.AllowUserToResizeRows = false;
        dgvAgents.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvAgents.BorderStyle = BorderStyle.None;
        dgvAgents.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgvAgents.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dgvAgents.ColumnHeadersHeight = 40;
        dgvAgents.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvAgents.Dock = DockStyle.Fill;
        dgvAgents.EnableHeadersVisualStyles = false;
        dgvAgents.MultiSelect = false;
        dgvAgents.ReadOnly = true;
        dgvAgents.RowHeadersVisible = false;
        dgvAgents.RowTemplate.Height = 42;
        dgvAgents.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvAgents.Font = DesignTokens.FontBody;
        dgvAgents.CellPainting += dgvAgents_CellPainting;
        dgvAgents.Paint += dgvAgents_Paint;
        dgvAgents.SelectionChanged += dgvAgents_SelectionChanged;
        dgvAgents.CellMouseDown += dgvAgents_CellMouseDown;
        dgvAgents.CellMouseEnter += dgvAgents_CellMouseEnter;
        dgvAgents.CellMouseLeave += dgvAgents_CellMouseLeave;

        dgvAgents.ColumnHeadersDefaultCellStyle.Font = DesignTokens.FontSection;
        dgvAgents.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dgvAgents.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);

        colStatus.HeaderText = "TRẠNG THÁI";
        colStatus.Name = "colStatus";
        colStatus.FillWeight = 82;

        colName.HeaderText = "TÊN MÁY (HOST)";
        colName.Name = "colName";
        colName.FillWeight = 115;

        colIp.HeaderText = "ĐỊA CHỈ IP";
        colIp.Name = "colIp";
        colIp.FillWeight = 95;

        colOs.HeaderText = "HỆ ĐIỀU HÀNH";
        colOs.Name = "colOs";
        colOs.FillWeight = 135;

        colCpu.HeaderText = "CPU (%)";
        colCpu.Name = "colCpu";
        colCpu.FillWeight = 92;

        colRam.HeaderText = "RAM (%)";
        colRam.Name = "colRam";
        colRam.FillWeight = 92;

        colDisk.HeaderText = "Ổ ĐĨA (%)";
        colDisk.Name = "colDisk";
        colDisk.FillWeight = 92;

        colAlert.HeaderText = "CHẨN ĐOÁN / CẢNH BÁO";
        colAlert.Name = "colAlert";
        colAlert.FillWeight = 150;

        colLastSeen.HeaderText = "CẬP NHẬT";
        colLastSeen.Name = "colLastSeen";
        colLastSeen.FillWeight = 78;

        dgvAgents.Columns.AddRange(
            colStatus, colName, colIp, colOs,
            colCpu, colRam, colDisk, colAlert, colLastSeen);

        splitMain.Panel1.Controls.Add(dgvAgents);

        // 
        // splitBottom
        // 
        splitBottom.Dock = DockStyle.Fill;
        splitBottom.Orientation = Orientation.Vertical;
        splitBottom.Size = new Size(1220, 236);
        splitBottom.SplitterDistance = 580;
        splitBottom.SplitterWidth = 4;

        // 
        // pnlChartContainer (Left Pane)
        // 
        pnlChartContainer.Dock = DockStyle.Fill;

        pnlChartHeader.Dock = DockStyle.Top;
        pnlChartHeader.Height = 34;
        pnlChartHeader.Paint += pnlSubHeader_Paint;

        lblChartTitle.AutoSize = true;
        lblChartTitle.Font = DesignTokens.FontSection;
        lblChartTitle.Location = new Point(12, 8);
        lblChartTitle.Text = "BIỂU ĐỒ XU HƯỚNG CPU & RAM (THEO THỜI GIAN THỰC)";
        lblChartTitle.UseMnemonic = false;

        pnlChartHeader.Controls.Add(lblChartTitle);

        pnlChartCanvas.Dock = DockStyle.Fill;
        pnlChartCanvas.Paint += pnlChartCanvas_Paint;
        pnlChartCanvas.Resize += (_, _) => pnlChartCanvas.Invalidate();

        pnlChartContainer.Controls.Add(pnlChartCanvas);
        pnlChartContainer.Controls.Add(pnlChartHeader);
        splitBottom.Panel1.Controls.Add(pnlChartContainer);

        // 
        // Right Pane: Log Header & Owner-Drawn Terminal ListBox
        // 
        pnlLogHeader.Dock = DockStyle.Top;
        pnlLogHeader.Size = new Size(636, 34);
        pnlLogHeader.Paint += pnlSubHeader_Paint;
        pnlLogHeader.Resize += (_, _) =>
        {
            btnClearLog.Location = new Point(Math.Max(12, pnlLogHeader.Width - btnClearLog.Width - 10), 4);
        };

        lblLogTitle.AutoSize = true;
        lblLogTitle.Font = DesignTokens.FontSection;
        lblLogTitle.Location = new Point(12, 8);
        lblLogTitle.Text = "NHẬT KÝ SỰ KIỆN MẠNG & CẢNH BÁO (TCP 5000 / UDP 9000)";
        lblLogTitle.UseMnemonic = false;

        btnClearLog.Tone = ButtonTone.Secondary;
        btnClearLog.Font = DesignTokens.FontLabel;
        btnClearLog.Radius = 6;
        btnClearLog.Size = new Size(102, 26);
        btnClearLog.Location = new Point(524, 4);
        btnClearLog.Text = "Xóa nhật ký";
        btnClearLog.Click += btnClearLog_Click;

        pnlLogHeader.Controls.Add(lblLogTitle);
        pnlLogHeader.Controls.Add(btnClearLog);

        lstLogs.BorderStyle = BorderStyle.None;
        lstLogs.Dock = DockStyle.Fill;
        lstLogs.DrawMode = DrawMode.OwnerDrawFixed;
        lstLogs.ItemHeight = 24;
        lstLogs.Font = DesignTokens.FontTabular;
        lstLogs.HorizontalScrollbar = true;
        lstLogs.DrawItem += lstLogs_DrawItem;

        splitBottom.Panel2.Controls.Add(lstLogs);
        splitBottom.Panel2.Controls.Add(pnlLogHeader);

        splitMain.Panel2.Controls.Add(splitBottom);

        // 
        // timerWatchdog
        // 
        timerWatchdog.Interval = 2000;
        timerWatchdog.Enabled = true;
        timerWatchdog.Tick += timerWatchdog_Tick;

        // 
        // trayIcon & trayMenu
        // 
        trayIcon.Text = "LanMonitor Server";
        trayIcon.Visible = true;
        trayIcon.DoubleClick += (_, _) => RestoreFromTray();

        // 
        // pnlFooter (Bottom Live LAN Telemetry Bar)
        // 
        pnlFooter.Dock = DockStyle.Bottom;
        pnlFooter.Height = 28;
        pnlFooter.Paint += pnlFooter_Paint;

        // 
        // Form1 (Thứ tự Controls.Add quyết định Z-Order Docking:
        // Header trên cùng, Footer dưới cùng, SettingsDrawer bên phải, Toolbar bên trên phần còn lại, và splitMain lấp đầy)
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1220, 720);
        MinimumSize = new Size(1040, 600);
        Controls.Add(configurePopover);
        Controls.Add(splitMain);
        Controls.Add(pnlToolbar);
        Controls.Add(settingsDrawer);
        Controls.Add(pnlFooter);
        Controls.Add(pnlHeader);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "LanMonitor Server — Hệ thống giám sát máy trạm trong mạng LAN";
        Load += Form1_Load;
        FormClosing += Form1_FormClosing;

        pnlHeader.ResumeLayout(false);
        pnlToolbar.ResumeLayout(false);
        pnlToolbar.PerformLayout();
        splitMain.Panel1.ResumeLayout(false);
        splitMain.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
        splitMain.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvAgents).EndInit();
        splitBottom.Panel1.ResumeLayout(false);
        splitBottom.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitBottom).EndInit();
        splitBottom.ResumeLayout(false);
        pnlChartContainer.ResumeLayout(false);
        pnlChartHeader.ResumeLayout(false);
        pnlChartHeader.PerformLayout();
        pnlLogHeader.ResumeLayout(false);
        pnlLogHeader.PerformLayout();
        ResumeLayout(false);
    }

    #endregion
}
