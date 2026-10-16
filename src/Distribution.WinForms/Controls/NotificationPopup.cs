using System.Drawing.Drawing2D;
using Distribution.Connect;

namespace Distribution.WinForms.Controls;

/// <summary>
/// A card that slides in at the bottom-right when a message arrives, so the user notices it without
/// having to open the notification list. Clicking it opens the message.
/// </summary>
public class NotificationPopup : Form
{
    private const int ShowMilliseconds = 9000;

    private readonly System.Windows.Forms.Timer _timer = new() { Interval = ShowMilliseconds };
    private readonly BroadcastMessage _message;
    private readonly Action _onOpen;
    private bool _hovered;

    private NotificationPopup(BroadcastMessage message, int alsoCount, Action onOpen)
    {
        _message = message;
        _onOpen = onOpen;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        Size = new Size(360, alsoCount > 0 ? 136 : 116);
        BackColor = Theme.Theme.Surface;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

        var accent = message.Kind switch
        {
            BroadcastKind.Offer => Theme.Theme.AccentGreen,
            BroadcastKind.Alert => Theme.Theme.Danger,
            _ => Theme.Theme.AccentBlue
        };

        var badge = new IconBadge
        {
            Glyph = Theme.Icons.Bell,
            Size = new Size(36, 36),
            Location = new Point(16, 18),
            BadgeColor = Theme.Theme.IconBadgeBackground,
            IconColor = accent
        };

        var kindLabel = new Label
        {
            Text = message.Kind.ToString().ToUpperInvariant(),
            Font = Theme.Theme.GetBoldFont(7.5f),
            ForeColor = accent,
            AutoSize = true,
            UseMnemonic = false,
            Location = new Point(64, 16),
            BackColor = Color.Transparent
        };

        var titleLabel = new Label
        {
            Text = message.Title,
            Font = Theme.Theme.GetBoldFont(10f),
            ForeColor = Theme.Theme.HeadingNavy,
            AutoSize = false,
            UseMnemonic = false,
            AutoEllipsis = true,
            Location = new Point(64, 32),
            Size = new Size(276, 20),
            BackColor = Color.Transparent
        };

        var bodyLabel = new Label
        {
            Text = message.Body.Replace('\n', ' ').Replace('\r', ' ').Trim(),
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted,
            AutoSize = false,
            UseMnemonic = false,
            AutoEllipsis = true,
            Location = new Point(64, 54),
            Size = new Size(276, 32),
            BackColor = Color.Transparent
        };

        var hintLabel = new Label
        {
            Text = alsoCount > 0
                ? $"Click to read  ·  and {alsoCount} more new message{(alsoCount == 1 ? "" : "s")}"
                : "Click to read",
            Font = Theme.Theme.GetFont(8f),
            ForeColor = Theme.Theme.Primary,
            AutoSize = true,
            UseMnemonic = false,
            Location = new Point(64, Height - 28),
            BackColor = Color.Transparent
        };

        var closeLabel = new Label
        {
            Text = Theme.Icons.Cancel,
            Font = Theme.Icons.GetFont(8f),
            ForeColor = Theme.Theme.Muted,
            Size = new Size(22, 22),
            Location = new Point(Width - 28, 8),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };
        closeLabel.Click += (_, _) => Close();

        Controls.Add(closeLabel);
        Controls.Add(hintLabel);
        Controls.Add(bodyLabel);
        Controls.Add(titleLabel);
        Controls.Add(kindLabel);
        Controls.Add(badge);

        foreach (Control c in new Control[] { this, badge, kindLabel, titleLabel, bodyLabel, hintLabel })
        {
            c.Click += (_, _) => OpenMessage();
            c.MouseEnter += (_, _) => { _hovered = true; _timer.Stop(); Invalidate(); };
            c.MouseLeave += (_, _) => { _hovered = false; _timer.Start(); Invalidate(); };
            if (!ReferenceEquals(c, this)) c.Cursor = Cursors.Hand;
        }

        Cursor = Cursors.Hand;
        _timer.Tick += (_, _) => { _timer.Stop(); Close(); };
    }

    private void OpenMessage()
    {
        _timer.Stop();
        Close();
        _onOpen();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var p = base.CreateParams;
            p.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE: never steals focus from the bill being typed.
            return p;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Nothing to draw when the control has been squeezed to nothing (minimised
        // window, or mid-layout): a rounded path cannot be built over an empty rectangle.
        if (Width < 2 || Height < 2) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.Theme.Surface);

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = new GraphicsPath();
        const int d = 16;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();

        g.FillPath(Theme.Theme.GetBrush(Theme.Theme.Surface), path);
        g.DrawPath(Theme.Theme.GetPen(_hovered ? Theme.Theme.Primary : Theme.Theme.Border), path);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyRoundedRegion();
    }

    private void ApplyRoundedRegion()
    {
        using var path = new GraphicsPath();
        const int d = 16;
        var rect = new Rectangle(0, 0, Width, Height);
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();

        Region?.Dispose();
        Region = new Region(path);
    }

    /// <summary>
    /// Shows one card for the newest message. <paramref name="onOpen"/> runs if the user clicks it.
    /// </summary>
    public static void Show(Form owner, IReadOnlyList<BroadcastMessage> newMessages, Action onOpen)
    {
        if (owner.IsDisposed || newMessages.Count == 0) return;

        var popup = new NotificationPopup(newMessages[0], newMessages.Count - 1, onOpen);
        popup.Load += (_, _) =>
        {
            // Just under the bell in the top bar: it points at where the messages live, and it stays
            // clear of the Windows notification that appears in the bottom-right corner.
            var area = Screen.FromControl(owner).WorkingArea;
            int x = Math.Min(owner.Right, area.Right) - popup.Width - 24;
            int y = Math.Max(area.Top + 8, owner.Top + 96);
            popup.Location = new Point(Math.Max(area.Left + 8, x), y);
        };
        popup.FormClosed += (_, _) => popup._timer.Dispose();
        popup.Show(owner);
        popup._timer.Start();
    }
}
