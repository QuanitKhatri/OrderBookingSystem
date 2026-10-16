using System.Drawing.Drawing2D;
using Distribution.Connect;

namespace Distribution.WinForms.Controls;

/// <summary>One row in the notification list: title, date and an unread marker.</summary>
public class BroadcastListItem : Panel
{
    private const int CornerRadius = 8;

    private readonly BroadcastMessage _message;
    private bool _hovered;
    private bool _selected;
    private bool _unread;

    public BroadcastMessage Message => _message;

    public bool Selected
    {
        get => _selected;
        set { _selected = value; Invalidate(); }
    }

    public bool Unread
    {
        get => _unread;
        set { _unread = value; Invalidate(); }
    }

    public BroadcastListItem(BroadcastMessage message, bool unread)
    {
        _message = message;
        _unread = unread;

        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Height = 66;
        Margin = new Padding(0, 0, 0, 6);
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;
        AccessibleName = message.Title;
        AccessibleRole = AccessibleRole.ListItem;

        MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
    }

    private Color KindColor => _message.Kind switch
    {
        BroadcastKind.Offer => Theme.Theme.AccentGreen,
        BroadcastKind.Alert => Theme.Theme.Danger,
        _ => Theme.Theme.AccentBlue
    };

    protected override void OnPaint(PaintEventArgs e)
    {
        // Nothing to draw when the control has been squeezed to nothing (minimised
        // window, or mid-layout): a rounded path cannot be built over an empty rectangle.
        if (Width < 2 || Height < 2) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        g.Clear(Theme.Theme.EffectiveBackColor(Parent));

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Rounded(rect, CornerRadius);

        if (_selected) g.FillPath(Theme.Theme.GetBrush(Theme.Theme.IconBadgeBackground), path);
        else if (_hovered) g.FillPath(Theme.Theme.GetBrush(Theme.Theme.Background), path);
        else g.FillPath(Theme.Theme.GetBrush(Theme.Theme.Surface), path);

        g.DrawPath(Theme.Theme.GetPen(_selected ? Theme.Theme.Primary : Theme.Theme.Border), path);

        // Colour stripe showing the message kind.
        using (var stripe = Rounded(new Rectangle(0, 0, 8, Height - 1), CornerRadius))
        {
            var clip = g.Clip;
            g.SetClip(new Rectangle(0, 0, 4, Height));
            g.FillPath(Theme.Theme.GetBrush(KindColor), stripe);
            g.Clip = clip;
        }

        int left = 16;
        int right = Width - 12;

        if (_unread)
        {
            g.FillEllipse(Theme.Theme.GetBrush(Theme.Theme.Danger), new Rectangle(right - 10, 12, 8, 8));
            right -= 16;
        }

        var titleFont = _unread ? Theme.Theme.GetBoldFont(10f) : Theme.Theme.BaseFont;
        TextRenderer.DrawText(g, _message.Title, titleFont,
            new Rectangle(left, 10, right - left, 20), Theme.Theme.HeadingNavy,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        var preview = _message.Body.Replace('\n', ' ').Replace('\r', ' ').Trim();
        TextRenderer.DrawText(g, preview, Theme.Theme.MutedFont,
            new Rectangle(left, 30, Width - left - 12, 18), Theme.Theme.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        var meta = _message.PublishedAt.ToLocalTime().ToString("dd MMM yyyy, hh:mm tt");
        if (_message.HasMedia) meta += _message.VideoUrl is { Length: > 0 } ? "  ·  Video" : "  ·  Picture";
        TextRenderer.DrawText(g, meta, Theme.Theme.GetFont(8f),
            new Rectangle(left, 46, Width - left - 12, 16), Theme.Theme.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
    }

    private static GraphicsPath Rounded(Rectangle bounds, int radius)
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
}
