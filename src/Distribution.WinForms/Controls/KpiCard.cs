using System.Drawing.Drawing2D;

namespace Distribution.WinForms.Controls;

public class KpiCard : Panel
{
    private const int CornerRadius = 12;

    private readonly IconBadge _iconBadge;
    private readonly Label _labelText;
    private readonly Label _valueText;
    private readonly Label _noteText;
    private Size _regionSize = Size.Empty;

    public string IconGlyph
    {
        get => _iconBadge.Glyph;
        set => _iconBadge.Glyph = value;
    }

    public Color IconBadgeColor
    {
        get => _iconBadge.BadgeColor;
        set { _iconBadge.BadgeColor = value; Invalidate(); }
    }

    public Color IconColor
    {
        get => _iconBadge.IconColor;
        set => _iconBadge.IconColor = value;
    }

    public string Label
    {
        get => _labelText.Text;
        set => _labelText.Text = value;
    }

    public string Value
    {
        get => _valueText.Text;
        set => _valueText.Text = value;
    }

    public string Note
    {
        get => _noteText.Text;
        set
        {
            _noteText.Text = value;
            _noteText.Visible = !string.IsNullOrEmpty(value);
        }
    }

    public Color AccentColor
    {
        get => _valueText.ForeColor;
        set => _valueText.ForeColor = value;
    }

    public KpiCard()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(230, 110);

        _iconBadge = new IconBadge { Size = new Size(40, 40), Location = new Point(16, 16) };

        _labelText = new Label
        {
            AutoSize = true,
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted,
            Location = new Point(64, 28),
            BackColor = Color.Transparent
        };

        _valueText = new Label
        {
            AutoSize = true,
            Font = Theme.Theme.KpiFont,
            ForeColor = Theme.Theme.Primary,
            Location = new Point(16, 68),
            BackColor = Color.Transparent
        };

        _noteText = new Label
        {
            AutoSize = true,
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted,
            Location = new Point(16, 92),
            BackColor = Color.Transparent,
            Visible = false
        };

        Controls.Add(_noteText);
        Controls.Add(_valueText);
        Controls.Add(_labelText);
        Controls.Add(_iconBadge);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        var sizeChanged = _regionSize != Size;
        var path = GetPath(rect, sizeChanged);

        var tint = Color.FromArgb(28, _iconBadge.BadgeColor);
        using (var gradBrush = new LinearGradientBrush(rect, Theme.Theme.Surface, Color.FromArgb(255,
            Math.Min(255, Theme.Theme.Surface.R + tint.R / 4),
            Math.Min(255, Theme.Theme.Surface.G + tint.G / 4),
            Math.Min(255, Theme.Theme.Surface.B + tint.B / 4)), LinearGradientMode.ForwardDiagonal))
        {
            g.FillPath(gradBrush, path);
        }

        g.DrawPath(Theme.Theme.GetPen(Theme.Theme.Border), path);

        if (sizeChanged)
        {
            Region = new Region(path);
            _regionSize = Size;
        }
    }

    private GraphicsPath? _cachedPath;

    private GraphicsPath GetPath(Rectangle bounds, bool sizeChanged)
    {
        if (_cachedPath is null || sizeChanged)
        {
            _cachedPath?.Dispose();
            _cachedPath = RoundedRect(bounds, CornerRadius);
        }
        return _cachedPath;
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _cachedPath?.Dispose();
        base.Dispose(disposing);
    }
}
