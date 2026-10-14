using System.Drawing.Drawing2D;

namespace Distribution.WinForms.Controls;

public class CardPanel : Panel
{
    private const int CornerRadius = 10;
    private const int PadX = 20;
    private const int PadY = 16;
    private const int IconSize = 40;
    private const int IconTextGap = 12;

    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private bool _showIcon = true;
    private bool _autoHeight;
    private int _fillMinHeight;
    private bool _recalculating;
    private int _gap;
    private Size _regionSize = Size.Empty;
    private GraphicsPath? _cachedPath;
    private readonly IconBadge _iconBadge;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Panel _headerPanel;

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            _titleLabel.Text = value;
            LayoutHeader();
        }
    }

    public string Subtitle
    {
        get => _subtitle;
        set
        {
            _subtitle = value;
            _subtitleLabel.Text = value;
            LayoutHeader();
        }
    }

    public string IconGlyph
    {
        get => _iconBadge.Glyph;
        set { _iconBadge.Glyph = value; LayoutHeader(); }
    }

    public bool ShowIcon
    {
        get => _showIcon;
        set { _showIcon = value; LayoutHeader(); }
    }

    /// <summary>Size the card to its docked content instead of a hand-tuned fixed Height.</summary>
    public bool AutoHeight
    {
        get => _autoHeight;
        set { _autoHeight = value; RecalcAutoHeight(); }
    }

    /// <summary>With AutoHeight, the height reserved for a Dock=Fill child (e.g. a grid).</summary>
    public int FillMinHeight
    {
        get => _fillMinHeight;
        set { _fillMinHeight = value; RecalcAutoHeight(); }
    }

    public Panel ContentArea { get; }

    public CardPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Theme.Surface;
        Padding = new Padding(PadX, PadY, PadX, PadY);

        _iconBadge = new IconBadge { Size = new Size(IconSize, IconSize), Visible = false };

        _titleLabel = new Label
        {
            AutoSize = true,
            UseMnemonic = false,
            BackColor = Color.Transparent,
            Font = Theme.Theme.SectionTitleFont,
            ForeColor = Theme.Theme.HeadingNavy,
            Visible = false
        };

        _subtitleLabel = new Label
        {
            AutoSize = true,
            UseMnemonic = false,
            BackColor = Color.Transparent,
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted,
            Visible = false
        };

        _headerPanel = new Panel { Dock = DockStyle.Top, Height = 0, Visible = false };
        _headerPanel.Controls.Add(_subtitleLabel);
        _headerPanel.Controls.Add(_titleLabel);
        _headerPanel.Controls.Add(_iconBadge);

        ContentArea = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 12, 0, 0) };
        ContentArea.ControlAdded += (_, _) => RecalcAutoHeight();
        ContentArea.ControlRemoved += (_, _) => RecalcAutoHeight();
        ContentArea.Layout += (_, _) => RecalcAutoHeight();

        Controls.Add(ContentArea);
        Controls.Add(_headerPanel);
    }

    // Title and subtitle are stacked from their measured heights (never hard-coded offsets), so a
    // larger font or DPI can't push the subtitle over the title's descenders.
    private void LayoutHeader()
    {
        bool hasTitle = !string.IsNullOrEmpty(_title);
        bool hasSubtitle = !string.IsNullOrEmpty(_subtitle);

        _titleLabel.Visible = hasTitle;
        _subtitleLabel.Visible = hasTitle && hasSubtitle;
        _iconBadge.Visible = hasTitle && _showIcon;
        _headerPanel.Visible = hasTitle;

        if (!hasTitle)
        {
            _headerPanel.Height = 0;
            RecalcAutoHeight();
            return;
        }

        int titleHeight = _titleLabel.PreferredHeight;
        int subtitleHeight = hasSubtitle ? _subtitleLabel.PreferredHeight : 0;
        int textBlock = titleHeight + (hasSubtitle ? subtitleHeight - 2 : 0);
        int iconHeight = _showIcon ? IconSize : 0;
        int rowHeight = Math.Max(iconHeight, textBlock);
        int textLeft = _showIcon ? IconSize + IconTextGap : 0;
        int textTop = (rowHeight - textBlock) / 2;

        _iconBadge.Location = new Point(0, (rowHeight - iconHeight) / 2);
        _titleLabel.Location = new Point(textLeft, textTop);
        _subtitleLabel.Location = new Point(textLeft + 1, textTop + titleHeight - 2);
        _headerPanel.Height = rowHeight + 4;
        RecalcAutoHeight();
    }

    private void RecalcAutoHeight()
    {
        if (!_autoHeight || _recalculating || ContentArea is null) return;
        _recalculating = true;
        try
        {
            int height = Padding.Vertical + ContentArea.Padding.Vertical;
            if (!string.IsNullOrEmpty(_title)) height += _headerPanel.Height;

            bool hasFill = false;
            foreach (Control child in ContentArea.Controls)
            {
                switch (child.Dock)
                {
                    case DockStyle.Top:
                    case DockStyle.Bottom:
                        height += child.Height;
                        break;
                    case DockStyle.Fill:
                        hasFill = true;
                        break;
                }
            }

            if (hasFill) height += _fillMinHeight;
            if (Height != height) Height = height;
        }
        finally
        {
            _recalculating = false;
        }
    }

    // Dock layout ignores Margin, so cards stacked with Margin.Top touched each other. Render the
    // margin as a real gap: shift the card down inside its own bounds and leave the gap outside the
    // clip region so the parent background shows through.
    protected override void OnMarginChanged(EventArgs e)
    {
        base.OnMarginChanged(e);
        _gap = Math.Max(0, Margin.Top);
        Padding = new Padding(PadX, PadY + _gap, PadX, PadY);
        _regionSize = Size.Empty;
        Invalidate();
        RecalcAutoHeight();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var sizeChanged = _regionSize != Size;
        if (_cachedPath is null || sizeChanged)
        {
            _cachedPath?.Dispose();
            _cachedPath = RoundedRect(new Rectangle(0, _gap, Width - 1, Height - 1 - _gap), CornerRadius);
        }

        g.FillPath(Theme.Theme.GetBrush(BackColor), _cachedPath);
        g.DrawPath(Theme.Theme.GetPen(Theme.Theme.Border), _cachedPath);

        // SetWindowRgn is expensive — only rebuild the clip region when the size actually changed.
        if (sizeChanged)
        {
            Region = new Region(_cachedPath);
            _regionSize = Size;
        }
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
