using Distribution.Connect;
using Distribution.WinForms.Controls;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Forms;

/// <summary>
/// Shown when the distributor has locked this installation. It covers the whole screen and the only
/// way out is to close the application or to have the lock lifted.
/// </summary>
public class LockedForm : Form
{
    private readonly ILicenseService _licenseService;
    private readonly ThemedButton _retryButton;
    private readonly Label _statusLabel;

    public LockedForm(ILicenseService licenseService, LicenseState state, string? siteId)
    {
        _licenseService = licenseService;

        Text = "Distribution Management System";
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Theme.Background;
        Font = Theme.Theme.BaseFont;
        TopMost = true;
        ShowInTaskbar = true;
        KeyPreview = true;

        var card = new Panel
        {
            Size = new Size(620, 420),
            BackColor = Theme.Theme.Surface,
            Anchor = AnchorStyles.None
        };

        var badge = new IconBadge
        {
            Glyph = Icons.Lock,
            Size = new Size(64, 64),
            Location = new Point((620 - 64) / 2, 44),
            BadgeColor = Color.FromArgb(255, 235, 235),
            IconColor = Theme.Theme.Danger
        };

        var title = new Label
        {
            Text = "The software is locked",
            Font = new Font(Theme.Theme.FontFamily, 18f, FontStyle.Bold),
            ForeColor = Theme.Theme.HeadingNavy,
            AutoSize = false,
            Size = new Size(560, 34),
            Location = new Point(30, 124),
            TextAlign = ContentAlignment.MiddleCenter,
            UseMnemonic = false
        };

        var message = new Label
        {
            Text = state.MessageOrDefault,
            Font = new Font(Theme.Theme.FontFamily, 11f),
            ForeColor = Theme.Theme.Text,
            AutoSize = false,
            Size = new Size(540, 120),
            Location = new Point(40, 168),
            TextAlign = ContentAlignment.TopCenter,
            UseMnemonic = false
        };

        _statusLabel = new Label
        {
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted,
            AutoSize = false,
            Size = new Size(540, 20),
            Location = new Point(40, 294),
            TextAlign = ContentAlignment.MiddleCenter,
            UseMnemonic = false,
            Text = string.IsNullOrWhiteSpace(siteId) ? string.Empty : $"Installation: {siteId}"
        };

        _retryButton = new ThemedButton
        {
            Text = "Check Again",
            Width = 160,
            Location = new Point(150, 330),
            IconGlyph = Icons.Sync
        };
        _retryButton.Click += async (_, _) => await RetryAsync();

        var closeButton = new ThemedButton
        {
            Text = "Close",
            Width = 140,
            Location = new Point(330, 330),
            Variant = ThemedButtonVariant.Secondary
        };
        closeButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        card.Controls.Add(badge);
        card.Controls.Add(title);
        card.Controls.Add(message);
        card.Controls.Add(_statusLabel);
        card.Controls.Add(_retryButton);
        card.Controls.Add(closeButton);

        Controls.Add(card);
        // Maximising finishes after Load, so centre again once the final client size is known.
        ClientSizeChanged += (_, _) => Centre(card);
        Shown += (_, _) => Centre(card);

        // Alt+F4 and the task bar can still close it; nothing else gets through.
        CancelButton = closeButton;
    }

    private void Centre(Control card) =>
        card.Location = new Point((ClientSize.Width - card.Width) / 2, (ClientSize.Height - card.Height) / 2);

    private async Task RetryAsync()
    {
        _retryButton.Enabled = false;
        _statusLabel.Text = "Checking…";
        try
        {
            var state = await _licenseService.CheckAsync();
            if (!state.IsLocked)
            {
                // Unlocked: let the caller carry on into the application.
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            _statusLabel.Text = "Still locked. Please contact your supplier.";
        }
        catch (Exception)
        {
            _statusLabel.Text = "Could not reach your supplier. Check the internet connection.";
        }
        finally
        {
            _retryButton.Enabled = true;
        }
    }
}
