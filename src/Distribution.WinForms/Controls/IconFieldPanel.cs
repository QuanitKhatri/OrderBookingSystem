using System.Drawing.Drawing2D;

namespace Distribution.WinForms.Controls;

public class IconFieldPanel : Panel
{
    private const int CornerRadius = 8;
    private string _glyph = string.Empty;
    private bool _focused;
    private GraphicsPath? _cachedPath;
    private Size _cachedPathSize;

    public string Glyph
    {
        get => _glyph;
        set
        {
            _glyph = value;
            Padding = string.IsNullOrEmpty(value) ? new Padding(10, 0, 8, 0) : new Padding(34, 0, 8, 0);
            Invalidate();
        }
    }

    /// <summary>Pill fill. Inputs hosted inside must use the same BackColor.</summary>
    public Color FillColor { get; set; } = Theme.Theme.InputBackground;

    public IconFieldPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Theme.InputBackground;
        Height = 32;
        Padding = new Padding(10, 0, 8, 0);
    }

    public void SetFocused(bool focused)
    {
        if (_focused == focused) return;
        _focused = focused;
        Invalidate();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);

        // TextBox/ComboBox clamp their own Height to the font's preferred height even under Dock.Fill,
        // but still get docked at Top=0 — leaving dead space below and the text looking top-aligned.
        // Re-center them vertically after the Dock engine has positioned Left/Width.
        foreach (Control c in Controls)
        {
            if (c is TextBox or ComboBox)
            {
                c.Top = Math.Max(0, (ClientSize.Height - c.Height) / 2);
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Nothing to draw when the control has been squeezed to nothing (minimised
        // window, or mid-layout): a rounded path cannot be built over an empty rectangle.
        if (Width < 2 || Height < 2) return;

        var g = e.Graphics;
        // Paint the corners outside the rounded pill with what's behind it, not the pill colour.
        g.Clear(Theme.Theme.EffectiveBackColor(Parent));
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        var path = GetPath(rect);

        g.FillPath(Theme.Theme.GetBrush(FillColor), path);

        var pen = Theme.Theme.GetPen(_focused ? Theme.Theme.InputBorderFocus : Theme.Theme.Border, _focused ? 1.6f : 1f);
        g.DrawPath(pen, path);

        if (!string.IsNullOrEmpty(_glyph))
        {
            var iconFont = Theme.Icons.GetFont(10.5f);
            var iconRect = new Rectangle(8, 0, 20, Height);
            TextRenderer.DrawText(g, _glyph, iconFont, iconRect, Theme.Theme.IconColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
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
