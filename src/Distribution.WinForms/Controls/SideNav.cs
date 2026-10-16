using System.Drawing.Drawing2D;

namespace Distribution.WinForms.Controls;

public record NavItem(string Key, string Text, string IconGlyph);
public record NavGroup(string Header, IReadOnlyList<NavItem> Items);

public class SideNav : Panel
{
    private readonly Panel _scrollHost;
    private readonly FlowLayoutPanel _stack;
    private readonly Dictionary<string, NavItemRow> _itemButtons = new();
    private readonly List<Label> _headers = [];
    private string? _activeKey;

    public event EventHandler<string>? ItemSelected;

    public SideNav()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Width = 240;
        Dock = DockStyle.Left;

        var brand = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.Transparent };
        var badge = new IconBadge
        {
            Glyph = Theme.Icons.Car,
            Size = new Size(40, 40),
            Location = new Point(16, 16)
        };
        var titleLabel = new Label
        {
            Text = "Distribution",
            ForeColor = Color.White,
            Font = new Font(Theme.Theme.FontFamily, 12f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(64, 14),
            BackColor = Color.Transparent
        };
        var subtitleLabel = new Label
        {
            Text = "Management System",
            ForeColor = Color.FromArgb(150, 190, 185),
            Font = new Font(Theme.Theme.FontFamily, 8f),
            AutoSize = true,
            Location = new Point(64, 36),
            BackColor = Color.Transparent
        };
        brand.Controls.Add(subtitleLabel);
        brand.Controls.Add(titleLabel);
        brand.Controls.Add(badge);

        _stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 4, 0, 6)
        };

        _scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent
        };
        _scrollHost.Controls.Add(_stack);
        _scrollHost.ClientSizeChanged += (_, _) => UpdateChildWidths();

        Controls.Add(_scrollHost);
        Controls.Add(brand);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // While the window is minimised, or mid-layout, the panel can be zero high. A gradient
        // brush cannot be built over an empty rectangle, so there is simply nothing to draw.
        if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0) return;

        var g = e.Graphics;
        using var brush = new LinearGradientBrush(ClientRectangle, Theme.Theme.SidebarGradientTop, Theme.Theme.SidebarGradientBottom, LinearGradientMode.Vertical);
        g.FillRectangle(brush, ClientRectangle);
    }

    public void SetGroups(IEnumerable<NavGroup> groups)
    {
        _scrollHost.SuspendLayout();
        _stack.SuspendLayout();

        _stack.Controls.Clear();
        _itemButtons.Clear();
        _headers.Clear();

        foreach (var group in groups)
        {
            var header = new Label
            {
                Text = group.Header.ToUpperInvariant(),
                ForeColor = Color.FromArgb(130, 160, 160),
                Font = Theme.Theme.GetBoldFont(8.5f),
                AutoSize = false,
                Height = 24,
                Padding = new Padding(20, 6, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
            _headers.Add(header);
            _stack.Controls.Add(header);

            foreach (var item in group.Items)
            {
                var row = new NavItemRow(item);
                _itemButtons[item.Key] = row;
                row.Click += (_, _) => SetActive(item.Key, raiseEvent: true);
                _stack.Controls.Add(row);
            }
        }

        _stack.ResumeLayout(true);
        _scrollHost.ResumeLayout(true);
        UpdateChildWidths();
    }

    private void UpdateChildWidths()
    {
        var w = _scrollHost.ClientSize.Width;
        if (w <= 0) return;

        foreach (var h in _headers) h.Width = w;
        foreach (var r in _itemButtons.Values) r.Width = Math.Max(10, w - 16);
    }

    public void SetActive(string key, bool raiseEvent)
    {
        if (_activeKey is not null && _itemButtons.TryGetValue(_activeKey, out var prev))
        {
            prev.IsActive = false;
        }

        if (_itemButtons.TryGetValue(key, out var current))
        {
            current.IsActive = true;
        }

        _activeKey = key;

        if (raiseEvent)
        {
            ItemSelected?.Invoke(this, key);
        }
    }
}

internal class NavItemRow : Panel
{
    private const int CornerRadius = 8;
    private bool _isActive;
    private bool _isHovered;
    private readonly string _glyph;
    private readonly string _text;
    private GraphicsPath? _cachedPath;
    private Size _cachedPathSize;

    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; Invalidate(); }
    }

    public NavItemRow(NavItem item)
    {
        _glyph = item.IconGlyph;
        _text = item.Text;

        AccessibleName = item.Text;
        AccessibleRole = AccessibleRole.PushButton;

        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        // Compact enough that all groups (incl. System > Users/Settings) fit on a 1080p screen
        // without the native scrollbar, which renders as a light strip on the dark sidebar.
        Height = 32;
        Margin = new Padding(8, 0, 8, 1);
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;

        MouseEnter += (_, _) => { _isHovered = true; Invalidate(); };
        MouseLeave += (_, _) => { _isHovered = false; Invalidate(); };
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

        if (_isActive)
        {
            g.FillPath(Theme.Theme.GetBrush(Theme.Theme.Primary), path);
        }
        else if (_isHovered)
        {
            g.FillPath(Theme.Theme.GetBrush(Color.FromArgb(28, 255, 255, 255)), path);
        }

        var iconFont = Theme.Icons.GetFont(11f);
        var iconRect = new Rectangle(14, 0, 22, Height);
        TextRenderer.DrawText(g, _glyph, iconFont, iconRect, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        var textRect = new Rectangle(42, 0, Width - 48, Height);
        // NoPrefix: otherwise "Profit & Loss" renders as "Profit _Loss" (& treated as a mnemonic).
        TextRenderer.DrawText(g, _text, Theme.Theme.BaseFont, textRect, Color.White,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
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
