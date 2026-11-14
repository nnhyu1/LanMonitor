using System.ComponentModel;
using System.Drawing.Drawing2D;
using LanMonitor.Shared;

namespace LanMonitor.Server;

public static class GraphicsExtensions
{
    public static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
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

    public static float EaseOutCubic(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }
}

public enum ButtonTone { Secondary, Primary, Danger, HeaderGhost }

/// <summary>
/// Nút bấm bo tròn vẽ bằng GDI+ có hiệu ứng Hover / Press mượt mà.
/// </summary>
public class ModernButton : Control
{
    private bool _hovered;
    private bool _pressed;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ButtonTone Tone { get; set; } = ButtonTone.Secondary;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 7;

    public ModernButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);
        Cursor = Cursors.Hand;
        Font = DesignTokens.FontBodyStrong;
        Size = new Size(120, 34);
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color bg, fg, border;
        switch (Tone)
        {
            case ButtonTone.Primary:
                bg = DesignTokens.AccentPrimary;
                fg = DesignTokens.AccentForeground;
                border = bg;
                break;
            case ButtonTone.Danger:
                bg = DesignTokens.StatusDangerBar;
                fg = Color.White;
                border = bg;
                break;
            case ButtonTone.HeaderGhost:
                bg = DesignTokens.SurfaceHeaderSecondary;
                fg = DesignTokens.TextOnDark;
                border = DesignTokens.BorderDark;
                break;
            default:
                bg = DesignTokens.SurfaceElevated;
                fg = DesignTokens.TextPrimary;
                border = DesignTokens.BorderStrong;
                break;
        }

        if (_pressed)
            bg = DesignTokens.Blend(bg, Color.Black, 0.16);
        else if (_hovered)
            bg = DesignTokens.Blend(bg, DesignTokens.IsDark ? Color.White : Color.Black, 0.10);

        Rectangle rect = new(0, 0, Width - 1, Height - 1);
        using (var path = GraphicsExtensions.CreateRoundedRect(rect, Radius))
        using (var brush = new SolidBrush(bg))
        using (var pen = new Pen(border, 1f))
        {
            g.FillPath(brush, path);
            if (Tone == ButtonTone.Secondary || Tone == ButtonTone.HeaderGhost)
                g.DrawPath(pen, path);
        }

        TextRenderer.DrawText(
            g, Text, Font, ClientRectangle, fg,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}

/// <summary>
/// Nút Configure (biểu tượng Sliders Vector) dùng để mở/đóng bảng cấu hình ngưỡng hiện đại.
/// </summary>
public class ConfigureButton : Control
{
    private bool _hovered;
    private bool _pressed;
    private bool _active;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Active
    {
        get => _active;
        set { _active = value; Invalidate(); }
    }

    public ConfigureButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);
        Cursor = Cursors.Hand;
        Font = DesignTokens.FontBodyStrong;
        Text = "Configure";
        Size = new Size(122, 30);
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color bg = _active ? DesignTokens.AccentPrimary : DesignTokens.SurfaceElevated;
        Color fg = _active ? DesignTokens.AccentForeground : DesignTokens.TextPrimary;
        Color border = _active ? DesignTokens.AccentPrimary : DesignTokens.BorderStrong;

        if (_pressed)
            bg = DesignTokens.Blend(bg, Color.Black, 0.14);
        else if (_hovered && !_active)
            bg = DesignTokens.Blend(bg, DesignTokens.AccentPrimary, 0.08);

        Rectangle rect = new(0, 0, Width - 1, Height - 1);
        using (var path = GraphicsExtensions.CreateRoundedRect(rect, 7))
        using (var brush = new SolidBrush(bg))
        using (var pen = new Pen(border, 1f))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        // Vẽ icon Sliders 3 thanh ngang Vector
        using (var linePen = new Pen(fg, 1.7f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        using (var knobBrush = new SolidBrush(fg))
        {
            int sx = 12;
            int ex = 26;
            int[] ys = { Height / 2 - 5, Height / 2, Height / 2 + 5 };
            int[] knobs = { sx + 4, ex - 4, sx + 7 };

            for (int i = 0; i < 3; i++)
            {
                g.DrawLine(linePen, sx, ys[i], ex, ys[i]);
                g.FillEllipse(knobBrush, knobs[i] - 2.3f, ys[i] - 2.3f, 4.6f, 4.6f);
            }
        }

        Rectangle textRect = new(30, 0, Width - 36, Height);
        TextRenderer.DrawText(
            g, Text, Font, textRect, fg,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }
}

/// <summary>
/// Bộ tăng/giảm số hiện đại (Modern Stepper) thay thế hoàn toàn NumericUpDown mặc định:
/// hỗ trợ bấm +/-, giữ chuột tăng nhanh và cuộn con lăn chuột.
/// </summary>
public class ModernStepper : Control
{
    private int _value = 80;
    private int _minimum = 0;
    private int _maximum = 100;
    private string _suffix = "%";

    private int _hoverPart; // 0 = none, -1 = minus, 1 = plus
    private int _pressPart;
    private readonly System.Windows.Forms.Timer _repeatTimer;
    private int _repeatTicks;

    public event EventHandler? ValueChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Minimum
    {
        get => _minimum;
        set { _minimum = value; if (_value < _minimum) Value = _minimum; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Maximum
    {
        get => _maximum;
        set { _maximum = value; if (_value > _maximum) Value = _maximum; }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Suffix
    {
        get => _suffix;
        set { _suffix = value ?? ""; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set
        {
            int clamped = Math.Clamp(value, _minimum, _maximum);
            if (_value == clamped) return;
            _value = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ModernStepper()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        Size = new Size(118, 30);
        Cursor = Cursors.Hand;

        _repeatTimer = new System.Windows.Forms.Timer { Interval = 75 };
        _repeatTimer.Tick += (_, _) =>
        {
            _repeatTicks++;
            if (_repeatTicks > 4 && _pressPart != 0)
                Value += _pressPart;
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _repeatTimer.Dispose();
        base.Dispose(disposing);
    }

    private int HitTestPart(int x)
    {
        int btnW = Height;
        if (x < btnW) return -1;
        if (x > Width - btnW) return 1;
        return 0;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int part = HitTestPart(e.X);
        if (part != _hoverPart)
        {
            _hoverPart = part;
            Cursor = part != 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hoverPart = 0;
        _pressPart = 0;
        _repeatTimer.Stop();
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            int part = HitTestPart(e.X);
            if (part != 0)
            {
                _pressPart = part;
                Value += part;
                _repeatTicks = 0;
                _repeatTimer.Start();
                Invalidate();
            }
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressPart = 0;
        _repeatTimer.Stop();
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        Value += e.Delta > 0 ? 1 : -1;
        base.OnMouseWheel(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle outer = new(0, 0, Width - 1, Height - 1);
        using (var bgPath = GraphicsExtensions.CreateRoundedRect(outer, 7))
        using (var bgBrush = new SolidBrush(DesignTokens.SurfaceSubtle))
        using (var borderPen = new Pen(DesignTokens.BorderStrong, 1f))
        {
            g.FillPath(bgBrush, bgPath);
            g.DrawPath(borderPen, bgPath);
        }

        int btnW = Height - 4;
        Rectangle minusRect = new(2, 2, btnW, Height - 4);
        Rectangle plusRect = new(Width - btnW - 2, 2, btnW, Height - 4);

        DrawStepButton(g, minusRect, isPlus: false, _hoverPart == -1, _pressPart == -1, _value > _minimum);
        DrawStepButton(g, plusRect, isPlus: true, _hoverPart == 1, _pressPart == 1, _value < _maximum);

        Rectangle valRect = new(minusRect.Right + 2, 0, plusRect.Left - minusRect.Right - 4, Height);
        TextRenderer.DrawText(
            g,
            $"{_value}{_suffix}",
            DesignTokens.FontTabularBold,
            valRect,
            DesignTokens.TextPrimary,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
    }

    private static void DrawStepButton(Graphics g, Rectangle rect, bool isPlus, bool hovered, bool pressed, bool enabled)
    {
        Color btnBg = DesignTokens.SurfaceElevated;
        if (!enabled)
            btnBg = DesignTokens.SurfaceSubtle;
        else if (pressed)
            btnBg = DesignTokens.AccentPrimary;
        else if (hovered)
            btnBg = DesignTokens.Blend(DesignTokens.SurfaceElevated, DesignTokens.AccentPrimary, 0.15);

        using (var path = GraphicsExtensions.CreateRoundedRect(rect, 5))
        using (var brush = new SolidBrush(btnBg))
        {
            g.FillPath(brush, path);
            if (enabled && !pressed)
            {
                using var pen = new Pen(hovered ? DesignTokens.AccentPrimary : DesignTokens.BorderSubtle, 1f);
                g.DrawPath(pen, path);
            }
        }

        Color iconColor = !enabled
            ? DesignTokens.TextMuted
            : pressed ? DesignTokens.AccentForeground : (hovered ? DesignTokens.AccentPrimary : DesignTokens.TextPrimary);

        float cx = rect.X + rect.Width / 2f;
        float cy = rect.Y + rect.Height / 2f;
        float arm = 4.2f;

        using var iconPen = new Pen(iconColor, 1.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        g.DrawLine(iconPen, cx - arm, cy, cx + arm, cy);
        if (isPlus)
            g.DrawLine(iconPen, cx, cy - arm, cx, cy + arm);
    }
}

/// <summary>
/// Nút bánh răng (răng cưa) có Animation xoay mượt mà khi bật/tắt bảng Cài đặt.
/// </summary>
public class GearButton : Control
{
    private bool _hovered;
    private bool _pressed;
    private bool _active;

    private float _angle;
    private float _targetAngle;
    private readonly System.Windows.Forms.Timer _animTimer;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Active
    {
        get => _active;
        set
        {
            if (_active == value) return;
            _active = value;
            _targetAngle = value ? 60f : 0f;
            _animTimer.Start();
            Invalidate();
        }
    }

    public GearButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint |
            ControlStyles.SupportsTransparentBackColor,
            true);
        Cursor = Cursors.Hand;
        Size = new Size(36, 36);

        _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animTimer.Tick += (_, _) =>
        {
            float diff = _targetAngle - _angle;
            if (Math.Abs(diff) < 0.8f)
            {
                _angle = _targetAngle;
                _animTimer.Stop();
            }
            else
            {
                _angle += diff * 0.28f;
            }
            Invalidate();
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _animTimer.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color bg = _active ? DesignTokens.AccentPrimary : DesignTokens.SurfaceHeaderSecondary;

        if (_pressed)
            bg = DesignTokens.Blend(bg, Color.Black, 0.18);
        else if (_hovered && !_active)
            bg = DesignTokens.Blend(bg, Color.White, 0.12);

        Rectangle rect = new(1, 1, Width - 3, Height - 3);
        using (var path = GraphicsExtensions.CreateRoundedRect(rect, 8))
        using (var brush = new SolidBrush(bg))
        using (var pen = new Pen(_active ? DesignTokens.AccentPrimary : DesignTokens.BorderDark, 1f))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        Color iconColor = _active ? DesignTokens.AccentForeground : DesignTokens.TextOnDark;
        float cx = Width / 2f;
        float cy = Height / 2f;
        float rOuter = 9.5f;
        float rInner = 7.0f;
        float rHole = 3.2f;
        int teeth = 8;
        double offsetRad = _angle * Math.PI / 180.0;

        using var gearPath = new GraphicsPath();
        var pts = new List<PointF>();
        for (int i = 0; i < teeth; i++)
        {
            double a0 = i * 2 * Math.PI / teeth + offsetRad;
            double step = 2 * Math.PI / teeth;
            pts.Add(Polar(cx, cy, rInner, a0));
            pts.Add(Polar(cx, cy, rOuter, a0 + step * 0.18));
            pts.Add(Polar(cx, cy, rOuter, a0 + step * 0.42));
            pts.Add(Polar(cx, cy, rInner, a0 + step * 0.60));
        }
        gearPath.AddPolygon(pts.ToArray());
        gearPath.AddEllipse(cx - rHole, cy - rHole, rHole * 2, rHole * 2);

        using var iconBrush = new SolidBrush(iconColor);
        g.FillPath(iconBrush, gearPath);
    }

    private static PointF Polar(float cx, float cy, float r, double angle) =>
        new(cx + (float)(r * Math.Cos(angle)), cy + (float)(r * Math.Sin(angle)));
}

/// <summary>
/// Công tắc gạt bật/tắt (Toggle Switch) có Animation trượt tròn 60fps.
/// </summary>
public class ModernToggleSwitch : Control
{
    private bool _checked;
    private float _knobProgress; // 0f (Off) -> 1f (On)
    private readonly System.Windows.Forms.Timer _animTimer;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            _animTimer.Start();
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? CheckedChanged;

    public ModernToggleSwitch()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        Cursor = Cursors.Hand;
        Size = new Size(44, 24);

        _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animTimer.Tick += (_, _) =>
        {
            float target = _checked ? 1f : 0f;
            float diff = target - _knobProgress;
            if (Math.Abs(diff) < 0.03f)
            {
                _knobProgress = target;
                _animTimer.Stop();
            }
            else
            {
                _knobProgress += diff * 0.35f;
            }
            Invalidate();
        };
    }

    public void SetCheckedImmediate(bool value)
    {
        _checked = value;
        _knobProgress = value ? 1f : 0f;
        _animTimer.Stop();
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _animTimer.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnClick(EventArgs e)
    {
        Checked = !Checked;
        base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle track = new(1, 1, Width - 3, Height - 3);
        Color trackColor = DesignTokens.Blend(DesignTokens.ToggleOff, DesignTokens.AccentPrimary, _knobProgress);

        using (var path = GraphicsExtensions.CreateRoundedRect(track, track.Height / 2))
        using (var brush = new SolidBrush(trackColor))
            g.FillPath(brush, path);

        int knobSize = Height - 8;
        float minX = 4f;
        float maxX = Width - knobSize - 4f;
        float knobX = minX + (maxX - minX) * _knobProgress;

        using var knobBrush = new SolidBrush(Color.White);
        g.FillEllipse(knobBrush, knobX, 4f, knobSize, knobSize);
    }
}

/// <summary>
/// Thanh chọn phân đoạn (Segmented Control) có Animation trượt thanh chọn 60fps.
/// </summary>
public class SegmentedControl : Control
{
    private string[] _items = { "Sáng", "Tối", "Hệ thống" };
    private int _selectedIndex;
    private float _animIndex;
    private readonly System.Windows.Forms.Timer _animTimer;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string[] Items
    {
        get => _items;
        set { _items = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            int clamped = Math.Clamp(value, 0, Math.Max(0, _items.Length - 1));
            if (_selectedIndex == clamped) return;
            _selectedIndex = clamped;
            _animTimer.Start();
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? SelectedIndexChanged;

    public SegmentedControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        Cursor = Cursors.Hand;
        Size = new Size(276, 34);
        Font = DesignTokens.FontBody;

        _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animTimer.Tick += (_, _) =>
        {
            float diff = _selectedIndex - _animIndex;
            if (Math.Abs(diff) < 0.02f)
            {
                _animIndex = _selectedIndex;
                _animTimer.Stop();
            }
            else
            {
                _animIndex += diff * 0.34f;
            }
            Invalidate();
        };
    }

    public void SetSelectedIndexImmediate(int index)
    {
        _selectedIndex = Math.Clamp(index, 0, Math.Max(0, _items.Length - 1));
        _animIndex = _selectedIndex;
        _animTimer.Stop();
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _animTimer.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _items.Length > 0)
        {
            int idx = Math.Clamp(e.X * _items.Length / Math.Max(1, Width), 0, _items.Length - 1);
            SelectedIndex = idx;
        }
        base.OnMouseDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle outer = new(0, 0, Width - 1, Height - 1);
        using (var bgPath = GraphicsExtensions.CreateRoundedRect(outer, 8))
        using (var bgBrush = new SolidBrush(DesignTokens.SegmentTrack))
        using (var borderPen = new Pen(DesignTokens.BorderSubtle, 1f))
        {
            g.FillPath(bgBrush, bgPath);
            g.DrawPath(borderPen, bgPath);
        }

        if (_items.Length == 0) return;

        float itemW = (Width - 6f) / _items.Length;

        // Vẽ viên thuốc đang trượt theo _animIndex
        Rectangle pillRect = new(
            3 + (int)Math.Round(_animIndex * itemW),
            3,
            (int)Math.Round(itemW),
            Height - 6);

        using (var selPath = GraphicsExtensions.CreateRoundedRect(pillRect, 6))
        using (var selBrush = new SolidBrush(DesignTokens.SegmentSelected))
        using (var selPen = new Pen(DesignTokens.BorderStrong, 1f))
        {
            g.FillPath(selBrush, selPath);
            g.DrawPath(selPen, selPath);
        }

        for (int i = 0; i < _items.Length; i++)
        {
            Rectangle cell = new(3 + (int)Math.Round(i * itemW), 3, (int)Math.Round(itemW), Height - 6);
            bool selected = i == _selectedIndex;
            TextRenderer.DrawText(
                g,
                _items[i],
                selected ? DesignTokens.FontBodyStrong : DesignTokens.FontBody,
                cell,
                selected ? DesignTokens.TextPrimary : DesignTokens.TextSecondary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }
}

/// <summary>
/// Bảng chọn màu nhấn (Accent Color Swatches) hiển thị các chấm tròn màu.
/// </summary>
public class AccentSwatchPicker : Control
{
    private int _selectedIndex;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            int clamped = Math.Clamp(value, 0, DesignTokens.AccentPresets.Length - 1);
            if (_selectedIndex == clamped) return;
            _selectedIndex = clamped;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? SelectedIndexChanged;

    public AccentSwatchPicker()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        Cursor = Cursors.Hand;
        Size = new Size(220, 34);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            int idx = e.X / 40;
            if (idx >= 0 && idx < DesignTokens.AccentPresets.Length)
                SelectedIndex = idx;
        }
        base.OnMouseDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        for (int i = 0; i < DesignTokens.AccentPresets.Length; i++)
        {
            var preset = DesignTokens.AccentPresets[i];
            Color swatch = DesignTokens.IsDark ? preset.Dark : preset.Light;
            int x = i * 40 + 4;
            int y = 4;
            int d = 26;

            if (i == _selectedIndex)
            {
                using var ringPen = new Pen(DesignTokens.TextPrimary, 2f);
                g.DrawEllipse(ringPen, x - 3, y - 3, d + 6, d + 6);
            }

            using (var brush = new SolidBrush(swatch))
                g.FillEllipse(brush, x, y, d, d);

            if (i == _selectedIndex)
            {
                using var checkPen = new Pen(DesignTokens.IsDark ? Color.FromArgb(15, 23, 42) : Color.White, 2.2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                };
                g.DrawLines(checkPen, new[]
                {
                    new Point(x + 7, y + 13),
                    new Point(x + 11, y + 17),
                    new Point(x + 19, y + 9)
                });
            }
        }
    }
}

/// <summary>
/// Bảng Popover "Configure" hiện đại với Animation trượt xuống khi bấm nút Configure.
/// </summary>
public class ConfigurePopover : Panel
{
    private const int TargetHeight = 228;
    private const int AnimDurationMs = 170;

    private readonly Label _lblTitle;
    private readonly ModernButton _btnDone;
    private readonly List<Label> _labels = new();
    private readonly ModernStepper _stepCpu;
    private readonly ModernStepper _stepRam;
    private readonly ModernStepper _stepDisk;
    private readonly ModernStepper _stepInterval;

    private readonly System.Windows.Forms.Timer _animTimer;
    private DateTime _animStart;
    private int _startHeight;
    private int _endHeight;
    private bool _closing;
    private bool _suppress;

    public event Action<int, int, int, int>? ValuesChanged;
    public event Action? Closed;

    public ConfigurePopover()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        Size = new Size(320, 0);
        Visible = false;

        _lblTitle = new Label
        {
            Text = "CẤU HÌNH NGƯỠNG & CHU KỲ",
            Location = new Point(16, 14),
            AutoSize = true,
            Font = DesignTokens.FontBadge,
            UseMnemonic = false
        };
        Controls.Add(_lblTitle);

        (_stepCpu, _) = AddRow("Ngưỡng cảnh báo CPU", 10, 100, 80, "%", 44);
        (_stepRam, _) = AddRow("Ngưỡng cảnh báo RAM", 10, 100, 85, "%", 82);
        (_stepDisk, _) = AddRow("Ngưỡng cảnh báo Ổ đĩa", 10, 100, 90, "%", 120);
        (_stepInterval, _) = AddRow("Chu kỳ Agent gửi tin", 1, 60, 3, "s", 158);

        _btnDone = new ModernButton
        {
            Text = "Đóng",
            Size = new Size(76, 26),
            Location = new Point(228, 192),
            Tone = ButtonTone.Secondary,
            Font = DesignTokens.FontLabel,
            Radius = 6
        };
        _btnDone.Click += (_, _) => AnimateToggle(false);
        Controls.Add(_btnDone);

        _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animTimer.Tick += (_, _) =>
        {
            float t = (float)(DateTime.UtcNow - _animStart).TotalMilliseconds / AnimDurationMs;
            if (t >= 1f)
            {
                Height = _endHeight;
                _animTimer.Stop();
                if (_closing)
                {
                    Visible = false;
                    Closed?.Invoke();
                }
            }
            else
            {
                float eased = GraphicsExtensions.EaseOutCubic(t);
                Height = (int)Math.Round(_startHeight + (_endHeight - _startHeight) * eased);
            }
        };

        ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _animTimer.Dispose();
        base.Dispose(disposing);
    }

    private (ModernStepper stepper, Label lbl) AddRow(string title, int min, int max, int val, string suffix, int y)
    {
        var lbl = new Label
        {
            Text = title,
            Location = new Point(16, y + 6),
            Size = new Size(165, 20),
            Font = DesignTokens.FontBody,
            UseMnemonic = false
        };
        var stepper = new ModernStepper
        {
            Location = new Point(186, y),
            Size = new Size(118, 30),
            Minimum = min,
            Maximum = max,
            Value = val,
            Suffix = suffix
        };
        stepper.ValueChanged += (_, _) =>
        {
            if (!_suppress)
                ValuesChanged?.Invoke(_stepCpu.Value, _stepRam.Value, _stepDisk.Value, _stepInterval.Value);
        };

        _labels.Add(lbl);
        Controls.Add(lbl);
        Controls.Add(stepper);
        return (stepper, lbl);
    }

    public void LoadValues(int cpu, int ram, int disk, int interval)
    {
        _suppress = true;
        _stepCpu.Value = cpu;
        _stepRam.Value = ram;
        _stepDisk.Value = disk;
        _stepInterval.Value = interval;
        _suppress = false;
    }

    public void AnimateToggle(bool open)
    {
        _closing = !open;
        if (open)
        {
            if (!Visible)
            {
                Height = 0;
                Visible = true;
            }
            BringToFront();
            _startHeight = Height;
            _endHeight = TargetHeight;
        }
        else
        {
            if (!Visible) return;
            _startHeight = Height;
            _endHeight = 0;
        }

        _animStart = DateTime.UtcNow;
        _animTimer.Start();
    }

    public void ApplyTheme()
    {
        BackColor = DesignTokens.SurfaceElevated;
        _lblTitle.ForeColor = DesignTokens.AccentPrimary;
        foreach (var l in _labels) l.ForeColor = DesignTokens.TextPrimary;
        Invalidate(true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(DesignTokens.BorderStrong, 1.5f);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}

/// <summary>
/// Khay chọn máy (Custom Dropdown Selector) vẽ bằng GDI+ đồng bộ Light/Dark Theme,
/// thay thế hoàn toàn ComboBox mặc định của Windows.
/// </summary>
public class ModernSelector : Control
{
    public record SelectorItem(string Key, string Title, string Subtitle, bool Online);

    private readonly List<SelectorItem> _items = new();
    private int _selectedIndex = -1;
    private bool _hovered;
    private bool _dropdownOpen;
    private Color _badgeColor = DesignTokens.AccentPrimary;
    private string _badgeText = "MÁY A";

    public event EventHandler? SelectedIndexChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BadgeColor
    {
        get => _badgeColor;
        set { _badgeColor = value; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string BadgeText
    {
        get => _badgeText;
        set { _badgeText = value ?? ""; Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            int next = _items.Count == 0 ? -1 : Math.Clamp(value, 0, _items.Count - 1);
            if (_selectedIndex == next) return;
            _selectedIndex = next;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public SelectorItem? SelectedItem =>
        _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;

    public ModernSelector()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        Cursor = Cursors.Hand;
        Size = new Size(280, 38);
    }

    public void SetItems(IEnumerable<SelectorItem> items, int selectedIndex)
    {
        _items.Clear();
        _items.AddRange(items);
        _selectedIndex = _items.Count == 0 ? -1 : Math.Clamp(selectedIndex, 0, _items.Count - 1);
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _items.Count > 0)
            ShowPopupList();
        base.OnMouseDown(e);
    }

    private void ShowPopupList()
    {
        _dropdownOpen = true;
        Invalidate();

        var listBox = new ListBox
        {
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 42,
            Width = Math.Max(Width, 270),
            Height = Math.Min(6, _items.Count) * 42 + 4,
            BackColor = DesignTokens.SurfaceElevated,
            ForeColor = DesignTokens.TextPrimary,
            Font = DesignTokens.FontBody
        };

        foreach (var item in _items)
            listBox.Items.Add(item);

        if (_selectedIndex >= 0 && _selectedIndex < listBox.Items.Count)
            listBox.SelectedIndex = _selectedIndex;

        int hoverIdx = -1;
        listBox.MouseMove += (_, me) =>
        {
            int idx = listBox.IndexFromPoint(me.Location);
            if (idx != hoverIdx)
            {
                hoverIdx = idx;
                listBox.Invalidate();
            }
        };

        listBox.DrawItem += (_, de) =>
        {
            if (de.Index < 0 || de.Index >= _items.Count) return;
            var item = _items[de.Index];
            bool selected = de.Index == _selectedIndex;
            bool hovered = de.Index == hoverIdx;

            Color bg = selected
                ? DesignTokens.AccentSelectionBg
                : hovered ? DesignTokens.SurfaceSubtle : DesignTokens.SurfaceElevated;

            using (var b = new SolidBrush(bg))
                de.Graphics.FillRectangle(b, de.Bounds);

            de.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color dot = item.Online ? DesignTokens.StatusOnlineDot : DesignTokens.StatusOfflineDot;
            using (var db = new SolidBrush(dot))
                de.Graphics.FillEllipse(db, de.Bounds.X + 12, de.Bounds.Y + 17, 8, 8);

            TextRenderer.DrawText(
                de.Graphics, item.Title, DesignTokens.FontBodyStrong,
                new Rectangle(de.Bounds.X + 28, de.Bounds.Y + 5, de.Bounds.Width - 36, 18),
                selected ? DesignTokens.AccentPrimary : DesignTokens.TextPrimary,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            TextRenderer.DrawText(
                de.Graphics, item.Subtitle, DesignTokens.FontTabular,
                new Rectangle(de.Bounds.X + 28, de.Bounds.Y + 23, de.Bounds.Width - 36, 15),
                DesignTokens.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        };

        var host = new ToolStripControlHost(listBox)
        {
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            AutoSize = false,
            Size = listBox.Size
        };

        var popup = new ToolStripDropDown
        {
            Margin = Padding.Empty,
            Padding = new Padding(1),
            BackColor = DesignTokens.BorderStrong
        };
        popup.Items.Add(host);
        popup.Closed += (_, _) =>
        {
            _dropdownOpen = false;
            Invalidate();
        };

        listBox.MouseClick += (_, me) =>
        {
            int idx = listBox.IndexFromPoint(me.Location);
            if (idx >= 0 && idx < _items.Count)
            {
                SelectedIndex = idx;
                popup.Close();
            }
        };

        popup.Show(this, new Point(0, Height + 4));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle rect = new(0, 0, Width - 1, Height - 1);
        Color border = _dropdownOpen
            ? _badgeColor
            : _hovered ? DesignTokens.BorderStrong : DesignTokens.BorderSubtle;

        using (var path = GraphicsExtensions.CreateRoundedRect(rect, 8))
        using (var bg = new SolidBrush(DesignTokens.SurfaceElevated))
        using (var pen = new Pen(border, _dropdownOpen ? 1.5f : 1f))
        {
            g.FillPath(bg, path);
            g.DrawPath(pen, path);
        }

        // Pill badge nhận diện MÁY A / MÁY B bên trái
        Rectangle badgeRect = new(8, 8, 54, Height - 16);
        using (var bPath = GraphicsExtensions.CreateRoundedRect(badgeRect, 5))
        using (var bBrush = new SolidBrush(_badgeColor))
            g.FillPath(bBrush, bPath);

        TextRenderer.DrawText(
            g, _badgeText, DesignTokens.FontBadge, badgeRect,
            DesignTokens.IsDark ? Color.FromArgb(15, 23, 42) : Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

        var sel = SelectedItem;
        string title = sel != null ? $"{sel.Title}  ·  {sel.Subtitle}" : "Chưa có máy trạm";
        TextRenderer.DrawText(
            g, title, DesignTokens.FontBodyStrong,
            new Rectangle(70, 0, Width - 96, Height),
            sel != null ? DesignTokens.TextPrimary : DesignTokens.TextMuted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        // Mũi tên Chevron vẽ Vector bên phải
        float cx = Width - 16f;
        float cy = Height / 2f;
        using var chevPen = new Pen(DesignTokens.TextMuted, 1.7f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        g.DrawLines(chevPen, new[]
        {
            new PointF(cx - 4.5f, cy - 2f),
            new PointF(cx, cy + 2.5f),
            new PointF(cx + 4.5f, cy - 2f)
        });
    }
}

/// <summary>
/// Ô tìm kiếm bo tròn hiện đại có icon Kính lúp Vector và nút xóa nhanh.
/// </summary>
public class ModernSearchBox : Panel
{
    private readonly TextBox _input;
    private bool _focused;

    public event EventHandler? SearchTextChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SearchText
    {
        get => _input.Text;
        set => _input.Text = value;
    }

    public ModernSearchBox()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        Size = new Size(210, 32);

        _input = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = DesignTokens.FontBody,
            PlaceholderText = "Tìm tên máy, IP, HĐH...",
            Location = new Point(30, 7),
            Width = Width - 38
        };
        _input.TextChanged += (_, _) => SearchTextChanged?.Invoke(this, EventArgs.Empty);
        _input.GotFocus += (_, _) => { _focused = true; Invalidate(); };
        _input.LostFocus += (_, _) => { _focused = false; Invalidate(); };
        Controls.Add(_input);

        Resize += (_, _) => _input.Width = Math.Max(40, Width - 38);
        ApplyTheme();
    }

    public void ApplyTheme()
    {
        BackColor = DesignTokens.SurfaceElevated;
        _input.BackColor = DesignTokens.SurfaceElevated;
        _input.ForeColor = DesignTokens.TextPrimary;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle rect = new(0, 0, Width - 1, Height - 1);
        Color border = _focused ? DesignTokens.AccentPrimary : DesignTokens.BorderStrong;
        using (var path = GraphicsExtensions.CreateRoundedRect(rect, 7))
        using (var bg = new SolidBrush(DesignTokens.SurfaceElevated))
        using (var pen = new Pen(border, _focused ? 1.5f : 1f))
        {
            g.FillPath(bg, path);
            g.DrawPath(pen, path);
        }

        // Vẽ icon kính lúp Vector bên trái
        using var iconPen = new Pen(_focused ? DesignTokens.AccentPrimary : DesignTokens.TextMuted, 1.6f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        g.DrawEllipse(iconPen, 10, 9, 10, 10);
        g.DrawLine(iconPen, 18.5f, 17.5f, 22, 21);
    }
}

/// <summary>
/// Panel cuộn mượt với thanh kéo mảnh bo tròn hiện đại (ẩn hoàn toàn thanh kéo mặc định của Windows).
/// </summary>
public class SmoothScrollPanel : Panel
{
    public Panel Content { get; } = new Panel();

    private int _scrollY;
    private int _contentHeight;
    private bool _thumbHovered;
    private bool _thumbDragging;
    private int _dragStartMouseY;
    private int _dragStartScrollY;

    public SmoothScrollPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        Content.Location = new Point(0, 0);
        Content.Width = 324;
        Controls.Add(Content);

        Resize += (_, _) =>
        {
            Content.Width = Math.Max(200, Width - 14);
            ClampScroll();
            Invalidate();
        };
    }

    public void SetContentHeight(int height)
    {
        _contentHeight = height;
        Content.Height = height;
        ClampScroll();
        WireWheelRecursive(Content);
        Invalidate();
    }

    private void WireWheelRecursive(Control parent)
    {
        foreach (Control c in parent.Controls)
        {
            c.MouseWheel -= Child_MouseWheel;
            c.MouseWheel += Child_MouseWheel;
            if (c.HasChildren)
                WireWheelRecursive(c);
        }
    }

    private void Child_MouseWheel(object? sender, MouseEventArgs e)
    {
        // Không cuộn panel nếu con trỏ đang chỉnh số trên ModernStepper
        if (sender is ModernStepper) return;
        ScrollBy(-e.Delta / 2);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        ScrollBy(-e.Delta / 2);
        base.OnMouseWheel(e);
    }

    private void ScrollBy(int delta)
    {
        int maxScroll = Math.Max(0, _contentHeight - Height);
        int next = Math.Clamp(_scrollY + delta, 0, maxScroll);
        if (next == _scrollY) return;
        _scrollY = next;
        Content.Top = -_scrollY;
        Invalidate();
    }

    private void ClampScroll()
    {
        int maxScroll = Math.Max(0, _contentHeight - Height);
        _scrollY = Math.Clamp(_scrollY, 0, maxScroll);
        Content.Top = -_scrollY;
    }

    private Rectangle GetThumbRect()
    {
        if (_contentHeight <= Height || Height <= 20)
            return Rectangle.Empty;

        int trackH = Height - 12;
        int thumbH = Math.Max(36, (int)Math.Round(trackH * ((double)Height / _contentHeight)));
        int maxScroll = _contentHeight - Height;
        int thumbY = 6 + (int)Math.Round((trackH - thumbH) * ((double)_scrollY / Math.Max(1, maxScroll)));
        int thumbW = (_thumbHovered || _thumbDragging) ? 7 : 5;
        return new Rectangle(Width - thumbW - 4, thumbY, thumbW, thumbH);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_thumbDragging)
        {
            int maxScroll = Math.Max(1, _contentHeight - Height);
            int trackH = Math.Max(1, Height - 12 - GetThumbRect().Height);
            int dy = e.Y - _dragStartMouseY;
            _scrollY = Math.Clamp(_dragStartScrollY + (int)Math.Round(dy * ((double)maxScroll / trackH)), 0, maxScroll);
            Content.Top = -_scrollY;
            Invalidate();
            return;
        }

        var thumb = GetThumbRect();
        bool hit = !thumb.IsEmpty && e.X >= Width - 16 && e.Y >= thumb.Top - 4 && e.Y <= thumb.Bottom + 4;
        if (hit != _thumbHovered)
        {
            _thumbHovered = hit;
            Cursor = hit ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        if (_thumbHovered && !_thumbDragging)
        {
            _thumbHovered = false;
            Cursor = Cursors.Default;
            Invalidate();
        }
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            var thumb = GetThumbRect();
            if (!thumb.IsEmpty && e.X >= Width - 16)
            {
                _thumbDragging = true;
                _dragStartMouseY = e.Y;
                _dragStartScrollY = _scrollY;
                Capture = true;
                Invalidate();
            }
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_thumbDragging)
        {
            _thumbDragging = false;
            Capture = false;
            Invalidate();
        }
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var thumb = GetThumbRect();
        if (thumb.IsEmpty) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color thumbColor = (_thumbHovered || _thumbDragging)
            ? DesignTokens.AccentPrimary
            : DesignTokens.Blend(DesignTokens.SurfaceElevated, DesignTokens.TextMuted, 0.45);

        using var path = GraphicsExtensions.CreateRoundedRect(thumb, thumb.Width / 2);
        using var brush = new SolidBrush(thumbColor);
        g.FillPath(brush, path);
    }
}

/// <summary>
/// Ngăn kéo Cài đặt (Settings Drawer) trượt mượt bên phải với đầy đủ tính năng cấu hình.
/// </summary>
public class SettingsDrawer : Panel
{
    private const int TargetWidth = 344;
    private const int AnimDurationMs = 190;

    private readonly Panel _header;
    private readonly Label _title;
    private readonly ModernButton _btnClose;
    private readonly SmoothScrollPanel _scrollHost;
    private Panel _scrollBody => _scrollHost.Content;

    private readonly SegmentedControl _segTheme;
    private readonly AccentSwatchPicker _accentPicker;
    private readonly ModernToggleSwitch _togCompact;
    private readonly ModernToggleSwitch _togChartLine;

    private readonly ModernStepper _stepCpu;
    private readonly ModernStepper _stepRam;
    private readonly ModernStepper _stepDisk;
    private readonly ModernStepper _stepInterval;
    private readonly ModernStepper _stepTimeoutMult;

    private readonly ModernToggleSwitch _togNotifyThreshold;
    private readonly ModernToggleSwitch _togNotifyOffline;
    private readonly ModernToggleSwitch _togSound;
    private readonly ModernToggleSwitch _togTray;

    private readonly ModernButton _btnExportCsv;
    private readonly ModernButton _btnExportLog;
    private readonly ModernButton _btnSimulate;
    private readonly ModernButton _btnReset;
    private readonly Panel _netInfoCard;
    private readonly Label _lblTcpVal;
    private readonly Label _lblUdpVal;

    private readonly List<Label> _sectionLabels = new();
    private readonly List<Label> _itemLabels = new();
    private readonly List<Label> _descLabels = new();
    private readonly List<Panel> _dividers = new();

    private readonly System.Windows.Forms.Timer _slideTimer;
    private DateTime _animStart;
    private int _startWidth;
    private int _endWidth;
    private bool _closing;
    private bool _suppressEvents;

    public event Action<AppSettings>? SettingsChanged;
    public event Action? CloseRequested;
    public event Action? ExportCsvRequested;
    public event Action? ExportLogRequested;
    public event Action? ToggleDemoAgentsRequested;
    public event Action? ResetDefaultsRequested;

    public SettingsDrawer()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Width = 0;
        Dock = DockStyle.Right;
        Visible = false;
        Padding = new Padding(1, 0, 0, 0);

        _header = new Panel { Dock = DockStyle.Top, Height = 52 };
        _title = new Label
        {
            Text = "CÀI ĐẶT HỆ THỐNG",
            AutoSize = true,
            Location = new Point(18, 16),
            Font = DesignTokens.FontSection,
            UseMnemonic = false
        };
        _btnClose = new ModernButton
        {
            Text = "✕",
            Size = new Size(32, 28),
            Tone = ButtonTone.Secondary,
            Location = new Point(296, 12)
        };
        _btnClose.Click += (_, _) => CloseRequested?.Invoke();
        _header.Controls.Add(_title);
        _header.Controls.Add(_btnClose);

        _scrollHost = new SmoothScrollPanel { Dock = DockStyle.Fill };

        int y = 12;

        // --- NHÓM 1: GIAO DIỆN ---
        y = AddSectionHeader("GIAO DIỆN & HIỂN THỊ", y);

        y = AddFieldLabel("Chế độ màu nền (Theme)", y);
        _segTheme = new SegmentedControl { Location = new Point(18, y), Size = new Size(288, 34) };
        _segTheme.SelectedIndexChanged += (_, _) => NotifyChanged();
        _scrollBody.Controls.Add(_segTheme);
        y += 44;

        y = AddFieldLabel("Màu nhấn chủ đạo (Accent)", y);
        _accentPicker = new AccentSwatchPicker { Location = new Point(18, y), Size = new Size(220, 34) };
        _accentPicker.SelectedIndexChanged += (_, _) => NotifyChanged();
        _scrollBody.Controls.Add(_accentPicker);
        y += 42;

        (_togCompact, y) = AddToggleRow("Bảng gọn (Compact Rows)", "Hiển thị nhiều máy hơn trên 1 màn hình", y);
        (_togChartLine, y) = AddToggleRow("Vẽ đường ngưỡng trên biểu đồ", "Đường nét đứt đỏ đánh dấu mức cảnh báo", y);

        y = AddDivider(y);

        // --- NHÓM 2: NGƯỠNG & MẠNG (Dùng ModernStepper thay hoàn toàn NumericUpDown) ---
        y = AddSectionHeader("NGƯỠNG CẢNH BÁO & MẠNG", y);

        (_stepCpu, y) = AddStepperRow("Ngưỡng cảnh báo CPU", 10, 100, 80, "%", y);
        (_stepRam, y) = AddStepperRow("Ngưỡng cảnh báo RAM", 10, 100, 85, "%", y);
        (_stepDisk, y) = AddStepperRow("Ngưỡng cảnh báo Ổ đĩa", 10, 100, 90, "%", y);
        (_stepInterval, y) = AddStepperRow("Chu kỳ Agent gửi tin", 1, 60, 3, "s", y);
        (_stepTimeoutMult, y) = AddStepperRow("Hệ số Timeout Offline", 2, 10, 3, "x", y);

        y = AddDivider(y);

        // --- NHÓM 3: THÔNG BÁO & HỆ THỐNG ---
        y = AddSectionHeader("CẢNH BÁO & KHAY HỆ THỐNG", y);

        (_togNotifyThreshold, y) = AddToggleRow("Thông báo Windows vượt ngưỡng", "Hiện popup góc màn hình khi quá tải", y);
        (_togNotifyOffline, y) = AddToggleRow("Thông báo khi máy mất kết nối", "Báo ngay khi Agent bị ngắt đột ngột", y);
        (_togSound, y) = AddToggleRow("Âm thanh cảnh báo (Beep)", "Phát tiếng bíp nhẹ khi máy vượt ngưỡng", y);
        (_togTray, y) = AddToggleRow("Thu gọn vào khay hệ thống", "Bấm nút X sẽ ẩn xuống System Tray", y);

        y = AddDivider(y);

        // --- NHÓM 4: TIỆN ÍCH & DỮ LIỆU ---
        y = AddSectionHeader("TIỆN ÍCH & XUẤT BÁO CÁO", y);

        _btnExportCsv = new ModernButton
        {
            Text = "Xuất báo cáo máy trạm (.CSV)",
            Location = new Point(18, y),
            Size = new Size(288, 34),
            Tone = ButtonTone.Secondary
        };
        _btnExportCsv.Click += (_, _) => ExportCsvRequested?.Invoke();
        _scrollBody.Controls.Add(_btnExportCsv);
        y += 42;

        _btnExportLog = new ModernButton
        {
            Text = "Lưu nhật ký sự kiện (.TXT)",
            Location = new Point(18, y),
            Size = new Size(288, 34),
            Tone = ButtonTone.Secondary
        };
        _btnExportLog.Click += (_, _) => ExportLogRequested?.Invoke();
        _scrollBody.Controls.Add(_btnExportLog);
        y += 42;

        _btnSimulate = new ModernButton
        {
            Text = "Mô phỏng máy trạm ảo (Demo / Test)",
            Location = new Point(18, y),
            Size = new Size(288, 34),
            Tone = ButtonTone.Primary
        };
        _btnSimulate.Click += (_, _) => ToggleDemoAgentsRequested?.Invoke();
        _scrollBody.Controls.Add(_btnSimulate);
        y += 46;

        _btnReset = new ModernButton
        {
            Text = "Khôi phục cài đặt mặc định",
            Location = new Point(18, y),
            Size = new Size(288, 34),
            Tone = ButtonTone.Danger
        };
        _btnReset.Click += (_, _) => ResetDefaultsRequested?.Invoke();
        _scrollBody.Controls.Add(_btnReset);
        y += 46;

        y = AddDivider(y);

        // --- NHÓM 5: THÔNG TIN CỔNG MẠNG (Card 2 dòng rõ nét, đủ 100% số port) ---
        y = AddSectionHeader("THÔNG TIN CỔNG MẠNG", y);

        _netInfoCard = new Panel
        {
            Location = new Point(18, y),
            Size = new Size(288, 64)
        };
        _netInfoCard.Paint += (_, pe) =>
        {
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new(0, 0, _netInfoCard.Width - 1, _netInfoCard.Height - 1);
            using var path = GraphicsExtensions.CreateRoundedRect(r, 8);
            using var bg = new SolidBrush(DesignTokens.SurfaceSubtle);
            using var pen = new Pen(DesignTokens.BorderSubtle, 1f);
            pe.Graphics.FillPath(bg, path);
            pe.Graphics.DrawPath(pen, path);
        };

        var lblTcpKey = new Label
        {
            Text = "TCP Server Port",
            Location = new Point(12, 10),
            AutoSize = true,
            Font = DesignTokens.FontBody,
            UseMnemonic = false
        };
        _lblTcpVal = new Label
        {
            Text = Protocol.TcpPort.ToString(),
            Location = new Point(216, 10),
            Size = new Size(60, 20),
            TextAlign = ContentAlignment.MiddleRight,
            Font = DesignTokens.FontTabularBold,
            UseMnemonic = false
        };

        var lblUdpKey = new Label
        {
            Text = "UDP Discovery Port",
            Location = new Point(12, 34),
            AutoSize = true,
            Font = DesignTokens.FontBody,
            UseMnemonic = false
        };
        _lblUdpVal = new Label
        {
            Text = Protocol.UdpPort.ToString(),
            Location = new Point(216, 34),
            Size = new Size(60, 20),
            TextAlign = ContentAlignment.MiddleRight,
            Font = DesignTokens.FontTabularBold,
            UseMnemonic = false
        };

        _itemLabels.Add(lblTcpKey);
        _itemLabels.Add(lblUdpKey);
        _sectionLabels.Add(_lblTcpVal);
        _sectionLabels.Add(_lblUdpVal);
        _netInfoCard.Controls.Add(lblTcpKey);
        _netInfoCard.Controls.Add(_lblTcpVal);
        _netInfoCard.Controls.Add(lblUdpKey);
        _netInfoCard.Controls.Add(_lblUdpVal);
        _scrollBody.Controls.Add(_netInfoCard);
        y += 84;

        _scrollHost.SetContentHeight(y);

        Controls.Add(_scrollHost);
        Controls.Add(_header);

        _slideTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _slideTimer.Tick += (_, _) =>
        {
            float t = (float)(DateTime.UtcNow - _animStart).TotalMilliseconds / AnimDurationMs;
            if (t >= 1f)
            {
                Width = _endWidth;
                _slideTimer.Stop();
                if (_closing) Visible = false;
            }
            else
            {
                float eased = GraphicsExtensions.EaseOutCubic(t);
                Width = (int)Math.Round(_startWidth + (_endWidth - _startWidth) * eased);
            }
        };

        ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _slideTimer.Dispose();
        base.Dispose(disposing);
    }

    public void AnimateOpen(bool open)
    {
        _closing = !open;
        if (open)
        {
            if (!Visible)
            {
                Width = 0;
                Visible = true;
            }
            _startWidth = Width;
            _endWidth = TargetWidth;
        }
        else
        {
            if (!Visible) return;
            _startWidth = Width;
            _endWidth = 0;
        }

        _animStart = DateTime.UtcNow;
        _slideTimer.Start();
    }

    public void LoadFrom(AppSettings s)
    {
        _suppressEvents = true;
        _segTheme.SetSelectedIndexImmediate((int)s.Theme);
        _accentPicker.SelectedIndex = s.AccentIndex;
        _togCompact.SetCheckedImmediate(s.CompactRows);
        _togChartLine.SetCheckedImmediate(s.ShowThresholdLineOnChart);

        _stepCpu.Value = Math.Clamp(s.CpuLimit, 10, 100);
        _stepRam.Value = Math.Clamp(s.RamLimit, 10, 100);
        _stepDisk.Value = Math.Clamp(s.DiskLimit, 10, 100);
        _stepInterval.Value = Math.Clamp(s.ReportIntervalSeconds, 1, 60);
        _stepTimeoutMult.Value = Math.Clamp(s.OfflineTimeoutMultiplier, 2, 10);

        _togNotifyThreshold.SetCheckedImmediate(s.NotifyOnThreshold);
        _togNotifyOffline.SetCheckedImmediate(s.NotifyOnOffline);
        _togSound.SetCheckedImmediate(s.SoundAlert);
        _togTray.SetCheckedImmediate(s.MinimizeToTray);
        _suppressEvents = false;
    }

    public void SetDemoActive(bool active)
    {
        _btnSimulate.Text = active
            ? "Dừng máy trạm ảo đang mô phỏng"
            : "Mô phỏng máy trạm ảo (Demo / Test)";
        _btnSimulate.Tone = active ? ButtonTone.Danger : ButtonTone.Primary;
        _btnSimulate.Invalidate();
    }

    public void ApplyTheme()
    {
        BackColor = DesignTokens.BorderStrong;
        _header.BackColor = DesignTokens.SurfaceSubtle;
        _scrollHost.BackColor = DesignTokens.SurfaceElevated;
        _scrollBody.BackColor = DesignTokens.SurfaceElevated;
        _netInfoCard.BackColor = DesignTokens.SurfaceElevated;
        _title.ForeColor = DesignTokens.TextPrimary;

        foreach (var l in _sectionLabels) l.ForeColor = DesignTokens.AccentPrimary;
        foreach (var l in _itemLabels) l.ForeColor = DesignTokens.TextPrimary;
        foreach (var l in _descLabels) l.ForeColor = DesignTokens.TextMuted;
        foreach (var d in _dividers) d.BackColor = DesignTokens.BorderSubtle;

        Invalidate(true);
    }

    private void NotifyChanged()
    {
        if (_suppressEvents) return;
        var s = new AppSettings
        {
            Theme = (ThemeMode)_segTheme.SelectedIndex,
            AccentIndex = _accentPicker.SelectedIndex,
            CompactRows = _togCompact.Checked,
            ShowThresholdLineOnChart = _togChartLine.Checked,
            CpuLimit = _stepCpu.Value,
            RamLimit = _stepRam.Value,
            DiskLimit = _stepDisk.Value,
            ReportIntervalSeconds = _stepInterval.Value,
            OfflineTimeoutMultiplier = _stepTimeoutMult.Value,
            NotifyOnThreshold = _togNotifyThreshold.Checked,
            NotifyOnOffline = _togNotifyOffline.Checked,
            SoundAlert = _togSound.Checked,
            MinimizeToTray = _togTray.Checked
        };
        SettingsChanged?.Invoke(s);
    }

    private int AddSectionHeader(string text, int y)
    {
        var lbl = new Label
        {
            Text = text,
            Location = new Point(18, y),
            AutoSize = true,
            Font = DesignTokens.FontBadge,
            UseMnemonic = false
        };
        _sectionLabels.Add(lbl);
        _scrollBody.Controls.Add(lbl);
        return y + 24;
    }

    private int AddFieldLabel(string text, int y)
    {
        var lbl = new Label
        {
            Text = text,
            Location = new Point(18, y),
            AutoSize = true,
            Font = DesignTokens.FontBody,
            UseMnemonic = false
        };
        _itemLabels.Add(lbl);
        _scrollBody.Controls.Add(lbl);
        return y + 22;
    }

    private (ModernToggleSwitch toggle, int nextY) AddToggleRow(string title, string subtitle, int y)
    {
        var lbl = new Label
        {
            Text = title,
            Location = new Point(18, y),
            Size = new Size(235, 18),
            Font = DesignTokens.FontBodyStrong,
            UseMnemonic = false
        };
        var sub = new Label
        {
            Text = subtitle,
            Location = new Point(18, y + 19),
            Size = new Size(235, 16),
            Font = DesignTokens.FontLabel,
            UseMnemonic = false
        };
        var tog = new ModernToggleSwitch { Location = new Point(262, y + 6) };
        tog.CheckedChanged += (_, _) => NotifyChanged();

        _itemLabels.Add(lbl);
        _descLabels.Add(sub);
        _scrollBody.Controls.Add(lbl);
        _scrollBody.Controls.Add(sub);
        _scrollBody.Controls.Add(tog);
        return (tog, y + 44);
    }

    private (ModernStepper stepper, int nextY) AddStepperRow(string title, int min, int max, int val, string suffix, int y)
    {
        var lbl = new Label
        {
            Text = title,
            Location = new Point(18, y + 5),
            Size = new Size(165, 20),
            Font = DesignTokens.FontBody,
            UseMnemonic = false
        };
        var stepper = new ModernStepper
        {
            Location = new Point(188, y),
            Size = new Size(118, 30),
            Minimum = min,
            Maximum = max,
            Value = val,
            Suffix = suffix
        };
        stepper.ValueChanged += (_, _) => NotifyChanged();

        _itemLabels.Add(lbl);
        _scrollBody.Controls.Add(lbl);
        _scrollBody.Controls.Add(stepper);
        return (stepper, y + 38);
    }

    private int AddDivider(int y)
    {
        var div = new Panel { Location = new Point(18, y + 4), Size = new Size(288, 1) };
        _dividers.Add(div);
        _scrollBody.Controls.Add(div);
        return y + 16;
    }
}
