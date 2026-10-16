using Distribution.Connect;
using Distribution.WinForms.Controls;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Forms;

/// <summary>
/// Offers an available update, downloads it with a progress bar, then restarts into the new version.
/// </summary>
public class UpdateForm : Form
{
    private readonly IUpdateService _updateService;
    private readonly UpdateCheckResult _result;

    private readonly Label _headlineLabel;
    private readonly Label _versionLabel;
    private readonly TextBox _notesBox;
    private readonly ProgressBar _progressBar;
    private readonly Label _statusLabel;
    private readonly ThemedButton _primaryButton;
    private readonly ThemedButton _laterButton;

    private bool _downloaded;
    private bool _busy;

    public UpdateForm(IUpdateService updateService, UpdateCheckResult result)
    {
        _updateService = updateService;
        _result = result;

        Text = "Software Update";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 420);
        BackColor = Theme.Theme.Background;
        Font = Theme.Theme.BaseFont;

        var badge = new IconBadge
        {
            Glyph = Icons.Download,
            Size = new Size(48, 48),
            Location = new Point(24, 24),
            BadgeColor = Theme.Theme.PastelGreen,
            IconColor = Theme.Theme.AccentGreen
        };

        _headlineLabel = new Label
        {
            Text = "A new version is available",
            Font = new Font(Theme.Theme.FontFamily, 14f, FontStyle.Bold),
            ForeColor = Theme.Theme.HeadingNavy,
            AutoSize = true,
            UseMnemonic = false,
            Location = new Point(88, 26)
        };

        _versionLabel = new Label
        {
            Text = $"You have {result.CurrentVersion}.  New version: {result.NewVersion}",
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted,
            AutoSize = true,
            UseMnemonic = false,
            Location = new Point(90, 54)
        };

        var notesTitle = new Label
        {
            Text = "What's new",
            Font = Theme.Theme.GetBoldFont(10f),
            ForeColor = Theme.Theme.HeadingNavy,
            AutoSize = true,
            Location = new Point(24, 96)
        };

        _notesBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Theme.Theme.Surface,
            ForeColor = Theme.Theme.Text,
            Font = Theme.Theme.BaseFont,
            Location = new Point(24, 120),
            Size = new Size(512, 180),
            // A multi-line TextBox only breaks on \r\n, and the notes arrive with plain \n.
            Text = string.IsNullOrWhiteSpace(result.ReleaseNotes)
                ? "This update contains fixes and improvements."
                : result.ReleaseNotes!.Trim().ReplaceLineEndings(Environment.NewLine)
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(24, 316),
            Size = new Size(512, 10),
            Style = ProgressBarStyle.Continuous,
            Visible = false
        };

        _statusLabel = new Label
        {
            Location = new Point(24, 332),
            Size = new Size(512, 20),
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted,
            UseMnemonic = false,
            Visible = false
        };

        _primaryButton = new ThemedButton
        {
            Text = "Download and Install",
            Width = 210,
            Location = new Point(24, 362),
            IconGlyph = Icons.Download
        };
        _laterButton = new ThemedButton
        {
            Text = "Later",
            Width = 110,
            Location = new Point(244, 362),
            Variant = ThemedButtonVariant.Secondary
        };

        _primaryButton.Click += async (_, _) => await PrimaryAsync();
        _laterButton.Click += (_, _) => Close();

        Controls.Add(badge);
        Controls.Add(_headlineLabel);
        Controls.Add(_versionLabel);
        Controls.Add(notesTitle);
        Controls.Add(_notesBox);
        Controls.Add(_progressBar);
        Controls.Add(_statusLabel);
        Controls.Add(_primaryButton);
        Controls.Add(_laterButton);

        CancelButton = _laterButton;
        FormClosing += (_, e) =>
        {
            // Stopping half way would leave a partly written package behind.
            if (_busy) e.Cancel = true;
        };
    }

    private async Task PrimaryAsync()
    {
        if (_downloaded)
        {
            Restart();
            return;
        }

        _busy = true;
        _primaryButton.Enabled = false;
        _laterButton.Enabled = false;
        _progressBar.Visible = true;
        _progressBar.Value = 0;
        _statusLabel.Visible = true;
        _statusLabel.Text = "Downloading…";

        try
        {
            var progress = new Progress<int>(p =>
            {
                _progressBar.Value = Math.Clamp(p, 0, 100);
                _statusLabel.Text = $"Downloading… {Math.Clamp(p, 0, 100)}%";
            });

            await _updateService.DownloadAsync(progress);

            _downloaded = true;
            _progressBar.Value = 100;
            _statusLabel.Text = "Download finished. The update installs when the application restarts.";
            _headlineLabel.Text = "Ready to install";
            _primaryButton.Text = "Restart Now";
            _primaryButton.IconGlyph = Icons.Refresh;
            _laterButton.Text = "Restart Later";
        }
        catch (Exception ex)
        {
            _progressBar.Visible = false;
            _statusLabel.ForeColor = Theme.Theme.Danger;
            _statusLabel.Text = $"Download failed: {ex.Message}";
            _primaryButton.Text = "Try Again";
        }
        finally
        {
            _busy = false;
            _primaryButton.Enabled = true;
            _laterButton.Enabled = true;
        }
    }

    private void Restart()
    {
        var answer = MessageBox.Show(
            this,
            "The application will close and open again in the new version.\n\nAnything you have typed but not saved will be lost. Continue?",
            "Restart to install",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer != DialogResult.Yes) return;

        try
        {
            _updateService.ApplyAndRestart();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"The update could not be installed: {ex.Message}",
                "Software Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
