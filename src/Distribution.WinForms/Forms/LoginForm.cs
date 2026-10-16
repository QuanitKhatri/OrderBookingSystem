using System.Drawing.Drawing2D;
using Distribution.Services.Auth;
using Distribution.WinForms.Controls;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Forms;

public class LoginForm : Form
{
    private readonly IAuthService _authService;
    private readonly TextBox _usernameBox;
    private readonly TextBox _passwordBox;
    private readonly Label _passwordToggle;
    private readonly Label _errorLabel;
    private readonly ThemedButton _loginButton;
    private bool _passwordVisible;

    public AuthResult? AuthenticatedUser { get; private set; }

    public LoginForm(IAuthService authService)
    {
        _authService = authService;

        Text = "Sign in — Distribution Management System";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 760);
        Font = Theme.Theme.BaseFont;
        AcceptButton = null;

        var background = new LoginBackgroundPanel { Dock = DockStyle.Fill };
        Controls.Add(background);

        var card = new LoginCardPanel
        {
            Size = new Size(420, 660),
            Location = new Point((560 - 420) / 2, 50)
        };

        // --- Logo + headings ---
        var logoBadge = new IconBadge
        {
            Glyph = Icons.Package,
            Size = new Size(64, 64),
            BadgeColor = Theme.Theme.Primary,
            IconColor = Color.White,
            Location = new Point((420 - 64) / 2, 32)
        };

        var title = new Label
        {
            Text = "Distribution",
            Font = new Font(Theme.Theme.FontFamily, 20f, FontStyle.Bold),
            ForeColor = Theme.Theme.HeadingNavy,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter
        };
        title.Location = new Point((420 - title.PreferredWidth) / 2, 106);

        var subtitle = new Label
        {
            Text = "Management System",
            Font = new Font(Theme.Theme.FontFamily, 12f),
            ForeColor = Theme.Theme.Muted,
            AutoSize = true
        };
        subtitle.Location = new Point((420 - subtitle.PreferredWidth) / 2, 144);

        var description = new Label
        {
            Text = "Manage your inventory, sales and distribution\neffortlessly",
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted,
            AutoSize = false,
            Size = new Size(360, 40),
            Location = new Point(30, 176),
            TextAlign = ContentAlignment.TopCenter
        };

        // --- Username field ---
        var usernameLabel = new Label
        {
            Text = "Username",
            Font = new Font(Theme.Theme.FontFamily, 10f, FontStyle.Bold),
            ForeColor = Theme.Theme.HeadingNavy,
            AutoSize = true,
            Location = new Point(32, 258)
        };

        var usernameField = new IconFieldPanel
        {
            Glyph = Icons.Contact,
            Location = new Point(32, 282),
            Size = new Size(356, 44)
        };
        _usernameBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.Theme.BaseFont,
            BackColor = Theme.Theme.InputBackground,
            Dock = DockStyle.Fill,
            PlaceholderText = "Enter your username"
        };
        _usernameBox.GotFocus += (_, _) => usernameField.SetFocused(true);
        _usernameBox.LostFocus += (_, _) => usernameField.SetFocused(false);
        usernameField.Controls.Add(_usernameBox);

        // --- Password field ---
        var passwordLabel = new Label
        {
            Text = "Password",
            Font = new Font(Theme.Theme.FontFamily, 10f, FontStyle.Bold),
            ForeColor = Theme.Theme.HeadingNavy,
            AutoSize = true,
            Location = new Point(32, 344)
        };

        var passwordField = new IconFieldPanel
        {
            Glyph = Icons.Lock,
            Padding = new Padding(34, 0, 36, 0),
            Location = new Point(32, 368),
            Size = new Size(356, 44)
        };
        _passwordBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.Theme.BaseFont,
            BackColor = Theme.Theme.InputBackground,
            Dock = DockStyle.Fill,
            UseSystemPasswordChar = true,
            PlaceholderText = "Enter your password"
        };
        _passwordBox.GotFocus += (_, _) => passwordField.SetFocused(true);
        _passwordBox.LostFocus += (_, _) => passwordField.SetFocused(false);

        _passwordToggle = new Label
        {
            Text = Icons.EyeOff,
            Font = new Font(Icons.FontFamily, 11f),
            ForeColor = Theme.Theme.Muted,
            Dock = DockStyle.Right,
            Width = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };
        _passwordToggle.Click += (_, _) => TogglePasswordVisibility();

        passwordField.Controls.Add(_passwordBox);
        passwordField.Controls.Add(_passwordToggle);

        _errorLabel = new Label
        {
            Location = new Point(32, 420),
            Size = new Size(356, 24),
            ForeColor = Theme.Theme.Danger,
            Font = Theme.Theme.MutedFont,
            Visible = false
        };

        _loginButton = new ThemedButton
        {
            Text = "Login (Enter)",
            Location = new Point(32, 452),
            Size = new Size(356, 46),
            Variant = ThemedButtonVariant.Primary,
            IconGlyph = Icons.Import
        };
        _loginButton.Click += async (_, _) => await TryLoginAsync();

        card.Controls.Add(logoBadge);
        card.Controls.Add(title);
        card.Controls.Add(subtitle);
        card.Controls.Add(description);
        card.Controls.Add(usernameLabel);
        card.Controls.Add(usernameField);
        card.Controls.Add(passwordLabel);
        card.Controls.Add(passwordField);
        card.Controls.Add(_errorLabel);
        card.Controls.Add(_loginButton);

        usernameField.TabIndex = 0;
        passwordField.TabIndex = 1;
        _loginButton.TabIndex = 2;
        _passwordToggle.TabStop = false;

        background.Controls.Add(card);

        _passwordBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await TryLoginAsync();
            }
        };
    }

    private void TogglePasswordVisibility()
    {
        _passwordVisible = !_passwordVisible;
        _passwordBox.UseSystemPasswordChar = !_passwordVisible;
        _passwordToggle.Text = _passwordVisible ? Icons.Eye : Icons.EyeOff;
    }

    private async Task TryLoginAsync()
    {
        _errorLabel.Visible = false;
        _loginButton.Enabled = false;
        try
        {
            var result = await _authService.LoginAsync(_usernameBox.Text.Trim(), _passwordBox.Text);
            if (!result.Succeeded)
            {
                _errorLabel.Text = result.Error;
                _errorLabel.Visible = true;
                return;
            }

            AuthenticatedUser = result;
            DialogResult = DialogResult.OK;
            Close();
        }
        finally
        {
            _loginButton.Enabled = true;
        }
    }
}

internal class LoginBackgroundPanel : Panel
{
    public LoginBackgroundPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Theme.Background;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Theme.Theme.Background);

        // Below this the wave is drawn with a gradient, which needs a rectangle with real sides.
        if (Width <= 0 || Height <= 0) return;

        using (var blobBrush = new SolidBrush(Color.FromArgb(60, Theme.Theme.IconBadgeBackground)))
        {
            g.FillEllipse(blobBrush, -80, -80, 260, 260);
            g.FillEllipse(blobBrush, Width - 220, -100, 300, 300);
            g.FillEllipse(blobBrush, -60, Height - 220, 180, 180);
        }

        int waveTop = Height - 140;
        using var path = new GraphicsPath();
        path.AddBezier(
            new Point(0, waveTop + 40),
            new Point(Width / 3, waveTop - 30),
            new Point(Width * 2 / 3, waveTop + 60),
            new Point(Width, waveTop - 10));
        path.AddLine(new Point(Width, waveTop - 10), new Point(Width, Height));
        path.AddLine(new Point(Width, Height), new Point(0, Height));
        path.CloseFigure();

        using var waveBrush = new LinearGradientBrush(
            new Rectangle(0, waveTop - 40, Width, Height - waveTop + 40),
            Theme.Theme.PrimaryDark, Theme.Theme.Primary, LinearGradientMode.Horizontal);
        g.FillPath(waveBrush, path);
    }
}

internal class LoginCardPanel : Panel
{
    private const int CornerRadius = 20;
    private Size _regionSize = Size.Empty;

    public LoginCardPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Theme.Surface;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        // Nothing to draw when the control has been squeezed to nothing (minimised
        // window, or mid-layout): a rounded path cannot be built over an empty rectangle.
        if (Width < 2 || Height < 2) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        var sizeChanged = _regionSize != Size;
        var path = GetPath(rect, sizeChanged);

        g.FillPath(Theme.Theme.GetBrush(Theme.Theme.Surface), path);
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
