namespace LanMonitor.Server;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    // 1. Top Command Header (Slate 900)
    private Panel pnlHeader;
    private Button btnToggleServer;
    private Button btnRemoveOffline;

    // 2. Secondary Filter & Threshold Bar (Slate 100)
    private Panel pnlToolbar;
    private Label lblSearch;
    private TextBox txtSearch;
    private Label lblFilter;
    private ComboBox cboFilter;
    private Label lblCpuLimit;
    private NumericUpDown numCpuLimit;
    private Label lblRamLimit;
    private NumericUpDown numRamLimit;
    private Label lblDiskLimit;
    private NumericUpDown numDiskLimit;
    private Label lblInterval;
    private NumericUpDown numInterval;

    // 3. Main Split Workspace
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

    // 4. Bottom Split: Live Trend Chart (Left) + Event Stream Console (Right)
    private SplitContainer splitBottom;
    private Panel pnlChartContainer;
    private Panel pnlChartHeader;
    private Label lblChartTitle;
    private Panel pnlChartCanvas;

    private Panel pnlLogHeader;
    private Label lblLogTitle;
    private Button btnClearLog;
    private ListBox lstLogs;

    private System.Windows.Forms.Timer timerWatchdog;

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
        btnToggleServer = new Button();
        btnRemoveOffline = new Button();

        pnlToolbar = new Panel();
        lblSearch = new Label();
        txtSearch = new TextBox();
        lblFilter = new Label();
        cboFilter = new ComboBox();
        lblCpuLimit = new Label();
        numCpuLimit = new NumericUpDown();
        lblRamLimit = new Label();
        numRamLimit = new NumericUpDown();
        lblDiskLimit = new Label();
        numDiskLimit = new NumericUpDown();
        lblInterval = new Label();
        numInterval = new NumericUpDown();

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
        btnClearLog = new Button();
        lstLogs = new ListBox();

        timerWatchdog = new System.Windows.Forms.Timer(components);

        pnlHeader.SuspendLayout();
        pnlToolbar.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numCpuLimit).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numRamLimit).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numDiskLimit).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numInterval).BeginInit();
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
        // pnlHeader (Top Command Ribbon) — Đặt Size trước khi thêm control con
        // 
        pnlHeader.BackColor = DesignTokens.SurfaceHeader;
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Size = new Size(1180, 60);
        pnlHeader.Paint += pnlHeader_Paint;
        pnlHeader.Resize += (_, _) => LayoutHeaderAndToolbar();

        // 
        // btnRemoveOffline
        // 
        btnRemoveOffline.BackColor = DesignTokens.SurfaceHeaderSecondary;
        btnRemoveOffline.FlatStyle = FlatStyle.Flat;
        btnRemoveOffline.FlatAppearance.BorderColor = DesignTokens.BorderDark;
        btnRemoveOffline.FlatAppearance.BorderSize = 1;
        btnRemoveOffline.FlatAppearance.MouseOverBackColor = Color.FromArgb(51, 65, 85);
        btnRemoveOffline.Font = DesignTokens.FontBody;
        btnRemoveOffline.ForeColor = DesignTokens.TextOnDark;
        btnRemoveOffline.Size = new Size(140, 34);
        btnRemoveOffline.Text = "Dọn máy Offline";
        btnRemoveOffline.Cursor = Cursors.Hand;
        btnRemoveOffline.Click += btnRemoveOffline_Click;

        // 
        // btnToggleServer
        // 
        btnToggleServer.BackColor = DesignTokens.StatusDangerBar;
        btnToggleServer.FlatStyle = FlatStyle.Flat;
        btnToggleServer.FlatAppearance.BorderSize = 0;
        btnToggleServer.Font = DesignTokens.FontBodyStrong;
        btnToggleServer.ForeColor = Color.White;
        btnToggleServer.Size = new Size(134, 34);
        btnToggleServer.Text = "Dừng Server";
        btnToggleServer.Cursor = Cursors.Hand;
        btnToggleServer.Click += btnToggleServer_Click;

        pnlHeader.Controls.Add(btnRemoveOffline);
        pnlHeader.Controls.Add(btnToggleServer);

        // 
        // pnlToolbar (Filter & Threshold Bar)
        // 
        pnlToolbar.BackColor = DesignTokens.SurfaceSubtle;
        pnlToolbar.Dock = DockStyle.Top;
        pnlToolbar.Size = new Size(1180, 48);
        pnlToolbar.Paint += pnlToolbar_Paint;
        pnlToolbar.Resize += (_, _) => LayoutHeaderAndToolbar();

        lblSearch.AutoSize = true;
        lblSearch.Font = DesignTokens.FontBodyStrong;
        lblSearch.ForeColor = DesignTokens.TextSecondary;
        lblSearch.Location = new Point(16, 15);
        lblSearch.Text = "Tìm máy:";
        lblSearch.UseMnemonic = false;

        txtSearch.BorderStyle = BorderStyle.FixedSingle;
        txtSearch.Font = DesignTokens.FontBody;
        txtSearch.Location = new Point(82, 12);
        txtSearch.Size = new Size(165, 25);
        txtSearch.PlaceholderText = "Tên máy hoặc IP...";
        txtSearch.TextChanged += Filter_Changed;

        lblFilter.AutoSize = true;
        lblFilter.Font = DesignTokens.FontBodyStrong;
        lblFilter.ForeColor = DesignTokens.TextSecondary;
        lblFilter.Location = new Point(262, 15);
        lblFilter.Text = "Lọc:";
        lblFilter.UseMnemonic = false;

        cboFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        cboFilter.FlatStyle = FlatStyle.Standard;
        cboFilter.Font = DesignTokens.FontBody;
        cboFilter.Location = new Point(298, 12);
        cboFilter.Size = new Size(145, 25);
        cboFilter.Items.AddRange(new object[]
        {
            "Tất cả máy",
            "Đang Online",
            "Đang Vượt ngưỡng",
            "Đã Offline"
        });
        cboFilter.SelectedIndex = 0;
        cboFilter.SelectedIndexChanged += Filter_Changed;

        lblCpuLimit.AutoSize = true;
        lblCpuLimit.Font = DesignTokens.FontBodyStrong;
        lblCpuLimit.ForeColor = DesignTokens.TextSecondary;
        lblCpuLimit.Text = "Ngưỡng CPU %:";
        lblCpuLimit.UseMnemonic = false;

        numCpuLimit.Font = DesignTokens.FontTabularBold;
        numCpuLimit.Minimum = 10;
        numCpuLimit.Maximum = 100;
        numCpuLimit.Value = 80;
        numCpuLimit.Width = 56;
        numCpuLimit.ValueChanged += Threshold_ValueChanged;

        lblRamLimit.AutoSize = true;
        lblRamLimit.Font = DesignTokens.FontBodyStrong;
        lblRamLimit.ForeColor = DesignTokens.TextSecondary;
        lblRamLimit.Text = "RAM %:";
        lblRamLimit.UseMnemonic = false;

        numRamLimit.Font = DesignTokens.FontTabularBold;
        numRamLimit.Minimum = 10;
        numRamLimit.Maximum = 100;
        numRamLimit.Value = 85;
        numRamLimit.Width = 56;
        numRamLimit.ValueChanged += Threshold_ValueChanged;

        lblDiskLimit.AutoSize = true;
        lblDiskLimit.Font = DesignTokens.FontBodyStrong;
        lblDiskLimit.ForeColor = DesignTokens.TextSecondary;
        lblDiskLimit.Text = "Disk %:";
        lblDiskLimit.UseMnemonic = false;

        numDiskLimit.Font = DesignTokens.FontTabularBold;
        numDiskLimit.Minimum = 10;
        numDiskLimit.Maximum = 100;
        numDiskLimit.Value = 90;
        numDiskLimit.Width = 56;
        numDiskLimit.ValueChanged += Threshold_ValueChanged;

        lblInterval.AutoSize = true;
        lblInterval.Font = DesignTokens.FontBodyStrong;
        lblInterval.ForeColor = DesignTokens.TextSecondary;
        lblInterval.Text = "Chu kỳ (s):";
        lblInterval.UseMnemonic = false;

        numInterval.Font = DesignTokens.FontTabularBold;
        numInterval.Minimum = 1;
        numInterval.Maximum = 60;
        numInterval.Value = 3;
        numInterval.Width = 52;
        numInterval.ValueChanged += numInterval_ValueChanged;

        pnlToolbar.Controls.Add(lblSearch);
        pnlToolbar.Controls.Add(txtSearch);
        pnlToolbar.Controls.Add(lblFilter);
        pnlToolbar.Controls.Add(cboFilter);
        pnlToolbar.Controls.Add(lblCpuLimit);
        pnlToolbar.Controls.Add(numCpuLimit);
        pnlToolbar.Controls.Add(lblRamLimit);
        pnlToolbar.Controls.Add(numRamLimit);
        pnlToolbar.Controls.Add(lblDiskLimit);
        pnlToolbar.Controls.Add(numDiskLimit);
        pnlToolbar.Controls.Add(lblInterval);
        pnlToolbar.Controls.Add(numInterval);

        // 
        // splitMain
        // 
        splitMain.BackColor = DesignTokens.BorderStrong;
        splitMain.Dock = DockStyle.Fill;
        splitMain.Orientation = Orientation.Horizontal;
        splitMain.Size = new Size(1180, 592);
        splitMain.SplitterDistance = 365;
        splitMain.SplitterWidth = 4;

        // 
        // dgvAgents
        // 
        dgvAgents.AllowUserToAddRows = false;
        dgvAgents.AllowUserToDeleteRows = false;
        dgvAgents.AllowUserToResizeRows = false;
        dgvAgents.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvAgents.BackgroundColor = DesignTokens.SurfaceCanvas;
        dgvAgents.BorderStyle = BorderStyle.None;
        dgvAgents.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgvAgents.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dgvAgents.ColumnHeadersHeight = 40;
        dgvAgents.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvAgents.Dock = DockStyle.Fill;
        dgvAgents.EnableHeadersVisualStyles = false;
        dgvAgents.GridColor = DesignTokens.BorderSubtle;
        dgvAgents.MultiSelect = false;
        dgvAgents.ReadOnly = true;
        dgvAgents.RowHeadersVisible = false;
        dgvAgents.RowTemplate.Height = 42;
        dgvAgents.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvAgents.Font = DesignTokens.FontBody;
        dgvAgents.CellPainting += dgvAgents_CellPainting;
        dgvAgents.Paint += dgvAgents_Paint;
        dgvAgents.SelectionChanged += dgvAgents_SelectionChanged;

        dgvAgents.ColumnHeadersDefaultCellStyle.BackColor = DesignTokens.SurfaceElevated;
        dgvAgents.ColumnHeadersDefaultCellStyle.ForeColor = DesignTokens.TextSecondary;
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
        // splitBottom (Left: Selected Agent Chart | Right: Network Event Console)
        // 
        splitBottom.BackColor = DesignTokens.BorderStrong;
        splitBottom.Dock = DockStyle.Fill;
        splitBottom.Orientation = Orientation.Vertical;
        splitBottom.Size = new Size(1180, 223);
        splitBottom.SplitterDistance = 560;
        splitBottom.SplitterWidth = 4;

        // 
        // pnlChartContainer (Left Pane)
        // 
        pnlChartContainer.BackColor = DesignTokens.SurfaceElevated;
        pnlChartContainer.Dock = DockStyle.Fill;

        pnlChartHeader.BackColor = DesignTokens.SurfaceSubtle;
        pnlChartHeader.Dock = DockStyle.Top;
        pnlChartHeader.Height = 34;
        pnlChartHeader.Paint += pnlSubHeader_Paint;

        lblChartTitle.AutoSize = true;
        lblChartTitle.Font = DesignTokens.FontSection;
        lblChartTitle.ForeColor = DesignTokens.TextPrimary;
        lblChartTitle.Location = new Point(12, 8);
        lblChartTitle.Text = "BIỂU ĐỒ XU HƯỚNG CPU & RAM (THEO THỜI GIAN THỰC)";
        lblChartTitle.UseMnemonic = false;

        pnlChartHeader.Controls.Add(lblChartTitle);

        pnlChartCanvas.BackColor = DesignTokens.SurfaceElevated;
        pnlChartCanvas.Dock = DockStyle.Fill;
        pnlChartCanvas.Paint += pnlChartCanvas_Paint;
        pnlChartCanvas.Resize += (_, _) => pnlChartCanvas.Invalidate();

        pnlChartContainer.Controls.Add(pnlChartCanvas);
        pnlChartContainer.Controls.Add(pnlChartHeader);
        splitBottom.Panel1.Controls.Add(pnlChartContainer);

        // 
        // Right Pane: Log Header & Owner-Drawn Terminal ListBox
        // 
        pnlLogHeader.BackColor = DesignTokens.SurfaceSubtle;
        pnlLogHeader.Dock = DockStyle.Top;
        pnlLogHeader.Size = new Size(616, 34);
        pnlLogHeader.Paint += pnlSubHeader_Paint;
        pnlLogHeader.Resize += (_, _) =>
        {
            btnClearLog.Location = new Point(Math.Max(12, pnlLogHeader.Width - btnClearLog.Width - 10), 4);
        };

        lblLogTitle.AutoSize = true;
        lblLogTitle.Font = DesignTokens.FontSection;
        lblLogTitle.ForeColor = DesignTokens.TextPrimary;
        lblLogTitle.Location = new Point(12, 8);
        lblLogTitle.Text = "NHẬT KÝ SỰ KIỆN MẠNG & CẢNH BÁO (TCP 5000 / UDP 9000)";
        lblLogTitle.UseMnemonic = false;

        btnClearLog.BackColor = DesignTokens.SurfaceElevated;
        btnClearLog.FlatStyle = FlatStyle.Flat;
        btnClearLog.FlatAppearance.BorderColor = DesignTokens.BorderStrong;
        btnClearLog.Font = DesignTokens.FontLabel;
        btnClearLog.ForeColor = DesignTokens.TextSecondary;
        btnClearLog.Size = new Size(102, 25);
        btnClearLog.Location = new Point(504, 4);
        btnClearLog.Text = "Xóa nhật ký";
        btnClearLog.Cursor = Cursors.Hand;
        btnClearLog.Click += btnClearLog_Click;

        pnlLogHeader.Controls.Add(lblLogTitle);
        pnlLogHeader.Controls.Add(btnClearLog);

        lstLogs.BackColor = DesignTokens.SurfaceTerminal;
        lstLogs.BorderStyle = BorderStyle.None;
        lstLogs.Dock = DockStyle.Fill;
        lstLogs.DrawMode = DrawMode.OwnerDrawFixed;
        lstLogs.ItemHeight = 24;
        lstLogs.Font = DesignTokens.FontTabular;
        lstLogs.ForeColor = DesignTokens.TextOnDark;
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
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = DesignTokens.SurfaceCanvas;
        ClientSize = new Size(1180, 700);
        MinimumSize = new Size(1024, 580);
        Controls.Add(splitMain);
        Controls.Add(pnlToolbar);
        Controls.Add(pnlHeader);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "LanMonitor Server — Hệ thống giám sát máy trạm trong mạng LAN";
        Load += Form1_Load;
        FormClosing += Form1_FormClosing;

        pnlHeader.ResumeLayout(false);
        pnlToolbar.ResumeLayout(false);
        pnlToolbar.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numCpuLimit).EndInit();
        ((System.ComponentModel.ISupportInitialize)numRamLimit).EndInit();
        ((System.ComponentModel.ISupportInitialize)numDiskLimit).EndInit();
        ((System.ComponentModel.ISupportInitialize)numInterval).EndInit();
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
