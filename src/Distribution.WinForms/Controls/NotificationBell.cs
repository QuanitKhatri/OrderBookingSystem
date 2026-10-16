using System.Drawing.Drawing2D;

namespace Distribution.WinForms.Controls;

/// <summary>Top-bar bell that shows how many broadcast messages have not been read yet.</summary>
public class NotificationBell : Control
{
    private int _unreadCount;
    private bool _hovered;

    public NotificationBell()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        Size = new Size(40, 40);
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        AccessibleName = "Notifications";
        AccessibleRole = AccessibleRole.PushButton;
        TabStop = false;

        MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
    }

    public int UnreadCount
    {
        get => _unreadCount;
        set
        {
            var clamped = Math.Max(0, value);
            if (_unreadCount == clamped) return;
            _unreadCount = clamped;
            AccessibleDescription = clamped == 0 ? "No unread messages" : $"{clamped} unread messages";
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        g.Clear(Theme.Theme.EffectiveBackColor(Parent));

        if (_hovered)
        {
            using var path = Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 8);
            g.FillPath(Theme.Theme.GetBrush(Theme.Theme.IconBadgeBackground), path);
        }

        var glyphColor = _unreadCount > 0 ? Theme.Theme.Primary : Theme.Theme.Muted;
        TextRenderer.DrawText(g, Theme.Icons.Bell, Theme.Icons.GetFont(15f),
            new Rectangle(0, 0, Width, Height), glyphColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        if (_unreadCount == 0) return;

        var text = _unreadCount > 9 ? "9+" : _unreadCount.ToString();
        var badgeFont = Theme.Theme.GetBoldFont(7.5f);
        var textSize = TextRenderer.MeasureText(g, text, badgeFont, Size.Empty, TextFormatFlags.NoPadding);
        int diameter = Math.Max(16, textSize.Width + 8);
        var badge = new Rectangle(Width - diameter - 3, 3, diameter, 16);

        g.FillEllipse(Theme.Theme.GetBrush(Theme.Theme.Danger), badge);
        TextRenderer.DrawText(g, text, badgeFont, badge, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
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
