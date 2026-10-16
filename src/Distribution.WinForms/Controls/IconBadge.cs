using System.Drawing.Drawing2D;

namespace Distribution.WinForms.Controls;

public class IconBadge : Control
{
    private string _glyph = Theme.Icons.Page;
    private string? _plainText;
    private Color _badgeColor = Theme.Theme.IconBadgeBackground;
    private Color _iconColor = Theme.Theme.IconColor;
    private GraphicsPath? _cachedPath;
    private Size _cachedPathSize;

    public string Glyph
    {
        get => _glyph;
        set { _glyph = value; _plainText = null; Invalidate(); }
    }

    /// <summary>When set, draws this literal text (regular font) instead of an icon glyph.</summary>
    public string? PlainText
    {
        get => _plainText;
        set { _plainText = value; Invalidate(); }
    }

    public Color BadgeColor
    {
        get => _badgeColor;
        set { _badgeColor = value; Invalidate(); }
    }

    public Color IconColor
    {
        get => _iconColor;
        set { _iconColor = value; Invalidate(); }
    }

    public IconBadge()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Size = new Size(44, 44);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Nothing to draw when the control has been squeezed to nothing (minimised
        // window, or mid-layout): a rounded path cannot be built over an empty rectangle.
        if (Width < 2 || Height < 2) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        var path = GetPath(rect);
        g.FillPath(Theme.Theme.GetBrush(_badgeColor), path);

        if (_plainText is not null)
        {
            var textFont = Theme.Theme.GetBoldFont(MathF.Round(Height * 0.34f, 1));
            TextRenderer.DrawText(g, _plainText, textFont, ClientRectangle, _iconColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
        else
        {
            var iconFont = Theme.Icons.GetFont(MathF.Round(Height * 0.42f, 1));
            TextRenderer.DrawText(g, _glyph, iconFont, ClientRectangle, _iconColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    private GraphicsPath GetPath(Rectangle bounds)
    {
        if (_cachedPath is null || _cachedPathSize != Size)
        {
            _cachedPath?.Dispose();
            _cachedPath = RoundedRect(bounds, 10);
            _cachedPathSize = Size;
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
