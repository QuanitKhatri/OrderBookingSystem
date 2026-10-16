using System.Drawing.Drawing2D;

namespace Distribution.WinForms.Controls;

public enum ThemedButtonVariant
{
    Primary,
    Secondary,
    Danger
}

public class ThemedButton : Button
{
    private const int CornerRadius = 6;
    private bool _isHovered;
    private ThemedButtonVariant _variant = ThemedButtonVariant.Primary;
    private GraphicsPath? _cachedPath;
    private Size _cachedPathSize;

    public ThemedButtonVariant Variant
    {
        get => _variant;
        set { _variant = value; ApplyVariantColors(); Invalidate(); }
    }

    public string? ShortcutHint { get; set; }
    public string? IconGlyph { get; set; }

    public ThemedButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = Theme.Theme.BaseFont;
        Cursor = Cursors.Hand;
        Height = 36;
        Padding = new Padding(12, 0, 12, 0);
        // Default Margin(3) pushed the first button in every row 3px below its siblings (which set Margin.Top = 0).
        Margin = new Padding(0);
        ApplyVariantColors();

        MouseEnter += (_, _) => { _isHovered = true; Invalidate(); };
        MouseLeave += (_, _) => { _isHovered = false; Invalidate(); };
    }

    private void ApplyVariantColors()
    {
        ForeColor = Color.White;
        BackColor = _variant switch
        {
            ThemedButtonVariant.Primary => Theme.Theme.Primary,
            ThemedButtonVariant.Secondary => Theme.Theme.Surface,
            ThemedButtonVariant.Danger => Theme.Theme.Danger,
            _ => Theme.Theme.Primary
        };
        if (_variant == ThemedButtonVariant.Secondary)
        {
            ForeColor = Theme.Theme.Text;
        }
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        // Nothing to draw when the control has been squeezed to nothing (minimised
        // window, or mid-layout): a rounded path cannot be built over an empty rectangle.
        if (Width < 2 || Height < 2) return;

        var g = pevent.Graphics;

        // The rounded shape leaves the corners unpainted, and ButtonBase does not reliably paint them
        // either, so stale pixels from the shared double-buffer (e.g. a neighbouring teal button) showed
        // through. Clear to whatever is visually behind the button first.
        g.Clear(Theme.Theme.EffectiveBackColor(Parent));
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        var path = GetPath(rect);

        var fill = _variant switch
        {
            ThemedButtonVariant.Primary => _isHovered ? Theme.Theme.PrimaryDark : Theme.Theme.Primary,
            ThemedButtonVariant.Danger => _isHovered ? ControlPaint.Dark(Theme.Theme.Danger) : Theme.Theme.Danger,
            ThemedButtonVariant.Secondary => _isHovered ? Theme.Theme.Background : Theme.Theme.Surface,
            _ => Theme.Theme.Primary
        };

        g.FillPath(Theme.Theme.GetBrush(fill), path);

        if (_variant == ThemedButtonVariant.Secondary)
        {
            g.DrawPath(Theme.Theme.GetPen(Theme.Theme.Border), path);
        }

        if (string.IsNullOrEmpty(IconGlyph))
        {
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
        else if (string.IsNullOrEmpty(Text))
        {
            TextRenderer.DrawText(g, IconGlyph, Theme.Icons.GetFont(10.5f), ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
        else
        {
            var iconFont = Theme.Icons.GetFont(10.5f);
            var textSize = TextRenderer.MeasureText(g, Text, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            const int iconWidth = 20;
            const int gap = 6;
            int blockWidth = iconWidth + gap + textSize.Width;
            int startX = (Width - blockWidth) / 2;

            var iconRect = new Rectangle(startX, 0, iconWidth, Height);
            TextRenderer.DrawText(g, IconGlyph, iconFont, iconRect, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            var textRect = new Rectangle(startX + iconWidth + gap, 0, textSize.Width, Height);
            TextRenderer.DrawText(g, Text, Font, textRect, ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }
    }


    private GraphicsPath GetPath(Rectangle bounds)
    {
        if (_cachedPath is null || _cachedPathSize != Size)
        {
            _cachedPath?.Dispose();
            _cachedPath = RoundedRect(bounds, CornerRadius);
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
