using System.Runtime.InteropServices;

namespace LanMonitor.Server;

public enum DemoLoadProfile { Mixed = 0, Normal = 1, Overload = 2, Fluctuating = 3 }
public enum DemoOsChoice { Mixed = 0, Windows11 = 1, Ubuntu = 2 }

public record DemoSimulationConfig(
    int MachineCount,
    string NamePrefix,
    DemoLoadProfile Profile,
    DemoOsChoice OsChoice,
    bool SimulateRandomDrop);

/// <summary>
/// Cửa sổ/Tab cấu hình chi tiết Mô phỏng máy trạm ảo (Demo / Stress Test)
/// với đầy đủ các lựa chọn số lượng máy, kịch bản tải, hệ điều hành và nút bấm chuyên nghiệp.
/// </summary>
public class DemoSimulatorForm : Form
{
    private readonly ModernStepper _stepCount;
    private readonly SegmentedControl _segPrefix;
    private readonly SegmentedControl _segProfile;
    private readonly SegmentedControl _segOs;
    private readonly ModernToggleSwitch _togRandomDrop;

    private readonly ModernButton _btnStart;
    private readonly ModernButton _btnStopAll;
    private readonly ModernButton _btnClose;

    private readonly Label _lblStatusBanner;

    public DemoSimulationConfig ResultConfig { get; private set; }
    public bool StopRequested { get; private set; }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public DemoSimulatorForm(DemoSimulationConfig currentConfig, bool isDemoRunning)
    {
        ResultConfig = currentConfig;

        Text = "Cấu hình Mô phỏng máy trạm ảo — LanMonitor";
        Size = new Size(490, 490);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = DesignTokens.SurfaceCanvas;

        // Header Banner
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 62,
            BackColor = DesignTokens.SurfaceSubtle
        };
        pnlTop.Paint += (_, e) =>
        {
            using var pen = new Pen(DesignTokens.BorderStrong, 1);
            e.Graphics.DrawLine(pen, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = "THIẾT LẬP MÔ PHỎNG MÁY TRẠM ẢO",
            Location = new Point(22, 12),
            AutoSize = true,
            Font = DesignTokens.FontSection,
            ForeColor = DesignTokens.TextPrimary,
            UseMnemonic = false
        };
        _lblStatusBanner = new Label
        {
            Text = isDemoRunning
                ? "Trạng thái: Đang có máy trạm ảo kết nối tới Server"
                : "Trạng thái: Chưa chạy mô phỏng",
            Location = new Point(22, 34),
            AutoSize = true,
            Font = DesignTokens.FontLabel,
            ForeColor = isDemoRunning ? DesignTokens.StatusOnlineText : DesignTokens.TextMuted,
            UseMnemonic = false
        };
        pnlTop.Controls.Add(lblTitle);
        pnlTop.Controls.Add(_lblStatusBanner);

        // Thân tùy chọn
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22)
        };

        int y = 18;

        // 1. Số lượng máy muốn tạo
        var lblCount = new Label
        {
            Text = "Số lượng máy trạm muốn tạo",
            Location = new Point(22, y + 6),
            AutoSize = true,
            Font = DesignTokens.FontBodyStrong,
            ForeColor = DesignTokens.TextPrimary,
            UseMnemonic = false
        };
        _stepCount = new ModernStepper
        {
            Location = new Point(316, y),
            Size = new Size(134, 32),
            Minimum = 1,
            Maximum = 20,
            Value = Math.Clamp(currentConfig.MachineCount, 1, 20),
            Suffix = " máy"
        };
        pnlBody.Controls.Add(lblCount);
        pnlBody.Controls.Add(_stepCount);
        y += 50;

        // 2. Nhóm tên máy
        pnlBody.Controls.Add(MakeFieldLabel("Tiền tố tên máy (Hostname Prefix)", 22, y));
        y += 22;
        _segPrefix = new SegmentedControl
        {
            Items = new[] { "PC-LAB", "DESIGN", "SERVER" },
            Location = new Point(22, y),
            Size = new Size(428, 34)
        };
        int prefixIdx = currentConfig.NamePrefix switch
        {
            "DESIGN" => 1,
            "SERVER" => 2,
            _ => 0
        };
        _segPrefix.SetSelectedIndexImmediate(prefixIdx);
        pnlBody.Controls.Add(_segPrefix);
        y += 48;

        // 3. Kịch bản tải (Workload)
        pnlBody.Controls.Add(MakeFieldLabel("Kịch bản tải CPU / RAM / Ổ đĩa", 22, y));
        y += 22;
        _segProfile = new SegmentedControl
        {
            Items = new[] { "Hỗn hợp", "Ổn định", "Quá tải", "Biến động" },
            Location = new Point(22, y),
            Size = new Size(428, 34)
        };
        _segProfile.SetSelectedIndexImmediate((int)currentConfig.Profile);
        pnlBody.Controls.Add(_segProfile);
        y += 48;

        // 4. Hệ điều hành
        pnlBody.Controls.Add(MakeFieldLabel("Hệ điều hành mô phỏng", 22, y));
        y += 22;
        _segOs = new SegmentedControl
        {
            Items = new[] { "Hỗn hợp Win/Linux", "Windows 11", "Ubuntu Linux" },
            Location = new Point(22, y),
            Size = new Size(428, 34)
        };
        _segOs.SetSelectedIndexImmediate((int)currentConfig.OsChoice);
        pnlBody.Controls.Add(_segOs);
        y += 52;

        // 5. Tùy chọn rớt mạng ngẫu nhiên
        var lblDrop = new Label
        {
            Text = "Giả lập rớt mạng ngẫu nhiên (Test Offline)",
            Location = new Point(22, y),
            Size = new Size(360, 18),
            Font = DesignTokens.FontBodyStrong,
            ForeColor = DesignTokens.TextPrimary,
            UseMnemonic = false
        };
        var lblDropSub = new Label
        {
            Text = "Ngắt kết nối 1 máy sau mỗi 15 giây để kiểm tra cảnh báo Offline",
            Location = new Point(22, y + 19),
            Size = new Size(375, 16),
            Font = DesignTokens.FontLabel,
            ForeColor = DesignTokens.TextMuted,
            UseMnemonic = false
        };
        _togRandomDrop = new ModernToggleSwitch
        {
            Location = new Point(406, y + 6)
        };
        _togRandomDrop.SetCheckedImmediate(currentConfig.SimulateRandomDrop);
        pnlBody.Controls.Add(lblDrop);
        pnlBody.Controls.Add(lblDropSub);
        pnlBody.Controls.Add(_togRandomDrop);

        // Footer Actions (Các nút bấm chuyên nghiệp)
        var pnlActions = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            BackColor = DesignTokens.SurfaceSubtle
        };
        pnlActions.Paint += (_, e) =>
        {
            using var pen = new Pen(DesignTokens.BorderStrong, 1);
            e.Graphics.DrawLine(pen, 0, 0, pnlActions.Width, 0);
        };

        _btnStopAll = new ModernButton
        {
            Text = "Dừng máy ảo",
            Location = new Point(22, 13),
            Size = new Size(124, 36),
            Tone = ButtonTone.Danger,
            Visible = isDemoRunning
        };
        _btnStopAll.Click += (_, _) =>
        {
            StopRequested = true;
            DialogResult = DialogResult.OK;
            Close();
        };

        _btnClose = new ModernButton
        {
            Text = "Đóng",
            Location = new Point(198, 13),
            Size = new Size(90, 36),
            Tone = ButtonTone.Secondary
        };
        _btnClose.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        _btnStart = new ModernButton
        {
            Text = isDemoRunning ? "Tạo lại mô phỏng" : "Khởi chạy mô phỏng",
            Location = new Point(298, 13),
            Size = new Size(154, 36),
            Tone = ButtonTone.Primary
        };
        _btnStart.Click += (_, _) =>
        {
            string prefix = _segPrefix.SelectedIndex switch
            {
                1 => "DESIGN",
                2 => "SERVER",
                _ => "PC-LAB"
            };
            ResultConfig = new DemoSimulationConfig(
                _stepCount.Value,
                prefix,
                (DemoLoadProfile)_segProfile.SelectedIndex,
                (DemoOsChoice)_segOs.SelectedIndex,
                _togRandomDrop.Checked);
            StopRequested = false;
            DialogResult = DialogResult.OK;
            Close();
        };

        pnlActions.Controls.Add(_btnStopAll);
        pnlActions.Controls.Add(_btnClose);
        pnlActions.Controls.Add(_btnStart);

        Controls.Add(pnlBody);
        Controls.Add(pnlActions);
        Controls.Add(pnlTop);

        Load += (_, _) =>
        {
            try
            {
                int useDark = DesignTokens.IsDark ? 1 : 0;
                DwmSetWindowAttribute(Handle, 20, ref useDark, sizeof(int));
            }
            catch { }
        };
    }

    private static Label MakeFieldLabel(string text, int x, int y) => new()
    {
        Text = text,
        Location = new Point(x, y),
        AutoSize = true,
        Font = DesignTokens.FontBodyStrong,
        ForeColor = DesignTokens.TextSecondary,
        UseMnemonic = false
    };
}
