using System.Diagnostics;
using Distribution.Connect;

namespace Distribution.Publisher;

/// <summary>
/// The distributor's own tool: writes a message, attaches a poster or video, and publishes it so
/// that every customer's copy of the application shows it.
/// </summary>
public class PublisherForm : Form
{
    private static readonly Color Primary = ColorTranslator.FromHtml("#0F5C63");
    private static readonly Color Background = ColorTranslator.FromHtml("#F7F6F2");
    private static readonly Color Heading = ColorTranslator.FromHtml("#16233D");
    private static readonly Color Muted = ColorTranslator.FromHtml("#5A5F66");
    private static readonly Color Danger = ColorTranslator.FromHtml("#B42318");

    private PublisherSettings _settings = PublisherSettings.Load();
    private BroadcastPublisher _publisher;

    // Compose tab
    private readonly TextBox _titleBox = new();
    private readonly TextBox _bodyBox = new();
    private readonly ComboBox _kindCombo = new();
    private readonly TextBox _imageBox = new();
    private readonly TextBox _videoBox = new();
    private readonly TextBox _linkBox = new();
    private readonly TextBox _linkTextBox = new();
    private readonly CheckBox _expiryCheck = new();
    private readonly DateTimePicker _expiryPicker = new();
    private readonly Button _sendButton = new();
    private readonly Label _statusLabel = new();

    // Sent tab
    private readonly ListView _sentList = new();
    private readonly Button _refreshButton = new();
    private readonly Button _deleteButton = new();

    // Settings tab
    private readonly TextBox _ownerBox = new();
    private readonly TextBox _repoBox = new();
    private readonly TextBox _tokenBox = new();
    private readonly Label _settingsStatus = new();

    public PublisherForm()
    {
        _publisher = new BroadcastPublisher(_settings);

        Text = "Distribution Publisher — send notifications to customers";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(900, 700);
        MinimumSize = new Size(820, 640);
        BackColor = Background;
        Font = new Font("Segoe UI", 9.75f);

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(14, 6) };
        tabs.TabPages.Add(BuildComposeTab());
        tabs.TabPages.Add(BuildSentTab());
        tabs.TabPages.Add(BuildSettingsTab());
        Controls.Add(tabs);

        tabs.SelectedIndexChanged += async (_, _) =>
        {
            if (tabs.SelectedIndex == 1 && _settings.IsConfigured) await LoadSentAsync();
        };

        Load += (_, _) =>
        {
            LoadSettingsIntoForm();
            if (!_settings.IsConfigured)
            {
                tabs.SelectedIndex = 2;
                _settingsStatus.ForeColor = Danger;
                _settingsStatus.Text = "Fill these in once before you can send anything.";
            }
        };
    }

    // ---------------- Compose ----------------

    private TabPage BuildComposeTab()
    {
        var page = new TabPage("Send Notification") { BackColor = Background, Padding = new Padding(16) };

        // Docked to the top and auto-sized: every row then takes just the height it needs, instead of
        // the leftover space being shared out and pushing labels away from their boxes.
        var scroller = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            Padding = new Padding(4),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        int row = 0;

        void AddLabel(string text, int r) =>
            layout.Controls.Add(new Label { Text = text, ForeColor = Heading, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) }, 0, r);

        void AutoRow() => layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        AddLabel("Title", row);
        _titleBox.Dock = DockStyle.Fill;
        _titleBox.MaxLength = 120;
        layout.Controls.Add(_titleBox, 1, row);
        layout.SetColumnSpan(_titleBox, 2);
        AutoRow(); row++;

        AddLabel("Type", row);
        _kindCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _kindCombo.Width = 200;
        _kindCombo.Items.AddRange(["Info — general news", "Offer — marketing or promotion", "Alert — needs attention"]);
        _kindCombo.SelectedIndex = 0;
        _kindCombo.Anchor = AnchorStyles.Left;
        layout.Controls.Add(_kindCombo, 1, row);
        AutoRow(); row++;

        AddLabel("Message", row);
        _bodyBox.Dock = DockStyle.Fill;
        _bodyBox.Multiline = true;
        _bodyBox.ScrollBars = ScrollBars.Vertical;
        _bodyBox.Height = 150;
        layout.Controls.Add(_bodyBox, 1, row);
        layout.SetColumnSpan(_bodyBox, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 158));
        row++;

        AddLabel("Picture", row);
        _imageBox.Dock = DockStyle.Fill;
        _imageBox.PlaceholderText = "Optional — poster or picture (PNG, JPG)";
        layout.Controls.Add(_imageBox, 1, row);
        layout.Controls.Add(BrowseButton(_imageBox, "Pictures|*.png;*.jpg;*.jpeg;*.gif;*.webp"), 2, row);
        AutoRow(); row++;

        AddLabel("Video", row);
        _videoBox.Dock = DockStyle.Fill;
        _videoBox.PlaceholderText = "Optional — MP4 plays on every PC";
        layout.Controls.Add(_videoBox, 1, row);
        layout.Controls.Add(BrowseButton(_videoBox, "Videos|*.mp4;*.webm"), 2, row);
        AutoRow(); row++;

        AddLabel("Link", row);
        _linkBox.Dock = DockStyle.Fill;
        _linkBox.PlaceholderText = "Optional — https://…";
        layout.Controls.Add(_linkBox, 1, row);
        layout.SetColumnSpan(_linkBox, 2);
        AutoRow(); row++;

        AddLabel("Link button", row);
        _linkTextBox.Dock = DockStyle.Fill;
        _linkTextBox.PlaceholderText = "Optional — text on the button, e.g. See the offer";
        layout.Controls.Add(_linkTextBox, 1, row);
        layout.SetColumnSpan(_linkTextBox, 2);
        AutoRow(); row++;

        AddLabel("Remove on", row);
        var expiryPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0) };
        _expiryCheck.Text = "Stop showing after";
        _expiryCheck.AutoSize = true;
        _expiryCheck.Margin = new Padding(0, 6, 8, 0);
        _expiryPicker.Format = DateTimePickerFormat.Short;
        _expiryPicker.Width = 130;
        _expiryPicker.Value = DateTime.Today.AddDays(30);
        _expiryPicker.Enabled = false;
        _expiryCheck.CheckedChanged += (_, _) => _expiryPicker.Enabled = _expiryCheck.Checked;
        expiryPanel.Controls.Add(_expiryCheck);
        expiryPanel.Controls.Add(_expiryPicker);
        layout.Controls.Add(expiryPanel, 1, row);
        AutoRow(); row++;

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
        StyleButton(_sendButton, "Send to all customers", 200, primary: true);
        _sendButton.Click += async (_, _) => await SendAsync();
        var previewButton = new Button();
        StyleButton(previewButton, "Preview", 110, primary: false);
        previewButton.Margin = new Padding(8, 0, 0, 0);
        previewButton.Click += (_, _) => Preview();
        buttonPanel.Controls.Add(_sendButton);
        buttonPanel.Controls.Add(previewButton);
        layout.Controls.Add(buttonPanel, 1, row);
        layout.SetColumnSpan(buttonPanel, 2);
        AutoRow(); row++;

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = Muted;
        _statusLabel.AutoSize = false;
        _statusLabel.Height = 40;
        layout.Controls.Add(_statusLabel, 1, row);
        layout.SetColumnSpan(_statusLabel, 2);
        AutoRow();

        scroller.Controls.Add(layout);
        page.Controls.Add(scroller);
        return page;
    }

    private Button BrowseButton(TextBox target, string filter)
    {
        var button = new Button();
        StyleButton(button, "Choose…", 100, primary: false);
        button.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = filter + "|All files|*.*" };
            if (dialog.ShowDialog(this) == DialogResult.OK) target.Text = dialog.FileName;
        };
        return button;
    }

    private static void StyleButton(Button button, string text, int width, bool primary)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 34;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.BackColor = primary ? Primary : Color.White;
        button.ForeColor = primary ? Color.White : Heading;
        button.Cursor = Cursors.Hand;
    }

    private BroadcastMessage? BuildMessage(out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(_titleBox.Text))
        {
            error = "Write a title.";
            return null;
        }
        if (string.IsNullOrWhiteSpace(_bodyBox.Text))
        {
            error = "Write the message.";
            return null;
        }

        foreach (var (box, what) in new[] { (_imageBox, "picture"), (_videoBox, "video") })
        {
            if (!string.IsNullOrWhiteSpace(box.Text) && !File.Exists(box.Text.Trim()))
            {
                error = $"The {what} file could not be found.";
                return null;
            }
        }

        var link = _linkBox.Text.Trim();
        if (link.Length > 0 && !(Uri.TryCreate(link, UriKind.Absolute, out var uri) &&
                                 (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
        {
            error = "The link must start with https:// or http://";
            return null;
        }

        return new BroadcastMessage
        {
            Id = BroadcastPublisher.NewMessageId(),
            Title = _titleBox.Text.Trim(),
            Body = _bodyBox.Text.TrimEnd(),
            Kind = (BroadcastKind)_kindCombo.SelectedIndex,
            LinkUrl = link.Length > 0 ? link : null,
            LinkText = _linkTextBox.Text.Trim() is { Length: > 0 } t ? t : null,
            PublishedAt = DateTimeOffset.UtcNow,
            ExpiresAt = _expiryCheck.Checked ? new DateTimeOffset(_expiryPicker.Value.Date.AddDays(1)) : null
        };
    }

    private void Preview()
    {
        var message = BuildMessage(out var error);
        if (message is null)
        {
            ShowStatus(error!, isError: true);
            return;
        }

        var media = new List<string>();
        if (!string.IsNullOrWhiteSpace(_imageBox.Text)) media.Add("picture: " + Path.GetFileName(_imageBox.Text));
        if (!string.IsNullOrWhiteSpace(_videoBox.Text)) media.Add("video: " + Path.GetFileName(_videoBox.Text));

        MessageBox.Show(this,
            $"{message.Kind.ToString().ToUpperInvariant()}\n\n{message.Title}\n\n{message.Body}\n\n" +
            (media.Count > 0 ? string.Join("\n", media) + "\n\n" : "") +
            (message.ExpiresAt is { } e ? $"Stops showing on {e.LocalDateTime:dd MMM yyyy}" : "Never expires"),
            "Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task SendAsync()
    {
        if (!_settings.IsConfigured)
        {
            ShowStatus("Open the Settings tab and fill in the repository and token first.", isError: true);
            return;
        }

        var message = BuildMessage(out var error);
        if (message is null)
        {
            ShowStatus(error!, isError: true);
            return;
        }

        var confirm = MessageBox.Show(this,
            $"Send \"{message.Title}\" to every customer?\n\nIt appears the next time their application checks, usually within an hour.",
            "Send notification", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        _sendButton.Enabled = false;
        UseWaitCursor = true;
        try
        {
            var progress = new Progress<string>(text => ShowStatus(text, isError: false));
            await _publisher.PublishAsync(
                message,
                string.IsNullOrWhiteSpace(_imageBox.Text) ? null : _imageBox.Text.Trim(),
                string.IsNullOrWhiteSpace(_videoBox.Text) ? null : _videoBox.Text.Trim(),
                progress);

            ShowStatus($"Sent. Customers will see \"{message.Title}\" at their next check.", isError: false);
            _titleBox.Clear();
            _bodyBox.Clear();
            _imageBox.Clear();
            _videoBox.Clear();
            _linkBox.Clear();
            _linkTextBox.Clear();
            _expiryCheck.Checked = false;
        }
        catch (Exception ex)
        {
            ShowStatus($"Could not send: {ex.Message}", isError: true);
        }
        finally
        {
            _sendButton.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private void ShowStatus(string text, bool isError)
    {
        _statusLabel.ForeColor = isError ? Danger : Muted;
        _statusLabel.Text = text;
    }

    // ---------------- Sent ----------------

    private TabPage BuildSentTab()
    {
        var page = new TabPage("Sent Messages") { BackColor = Background, Padding = new Padding(16) };

        _sentList.Dock = DockStyle.Fill;
        _sentList.View = View.Details;
        _sentList.FullRowSelect = true;
        _sentList.MultiSelect = false;
        _sentList.Columns.Add("Sent", 150);
        _sentList.Columns.Add("Type", 70);
        _sentList.Columns.Add("Title", 290);
        _sentList.Columns.Add("Media", 90);
        _sentList.Columns.Add("Expires", 110);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(0, 8, 0, 0) };
        StyleButton(_refreshButton, "Refresh", 110, primary: false);
        _refreshButton.Click += async (_, _) => await LoadSentAsync();
        StyleButton(_deleteButton, "Remove selected", 160, primary: false);
        _deleteButton.ForeColor = Danger;
        _deleteButton.Margin = new Padding(8, 0, 0, 0);
        _deleteButton.Click += async (_, _) => await DeleteSelectedAsync();
        buttons.Controls.Add(_refreshButton);
        buttons.Controls.Add(_deleteButton);

        page.Controls.Add(_sentList);
        page.Controls.Add(buttons);
        return page;
    }

    private async Task LoadSentAsync()
    {
        _sentList.Items.Clear();
        if (!_settings.IsConfigured) return;

        UseWaitCursor = true;
        try
        {
            var feed = await _publisher.GetFeedAsync();
            foreach (var m in feed.Messages.OrderByDescending(m => m.PublishedAt))
            {
                var media = m.VideoUrl is { Length: > 0 } ? "Video"
                    : m.ImageUrl is { Length: > 0 } ? "Picture" : "—";
                var item = new ListViewItem(m.PublishedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"))
                {
                    Tag = m.Id
                };
                item.SubItems.Add(m.Kind.ToString());
                item.SubItems.Add(m.Title);
                item.SubItems.Add(media);
                item.SubItems.Add(m.ExpiresAt?.ToLocalTime().ToString("dd MMM yyyy") ?? "Never");
                _sentList.Items.Add(item);
            }

            if (_sentList.Items.Count == 0)
            {
                _sentList.Items.Add(new ListViewItem("Nothing sent yet") { ForeColor = Muted });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not read the sent list: {ex.Message}", "Sent Messages",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (_sentList.SelectedItems.Count == 0) return;
        var item = _sentList.SelectedItems[0];
        if (item.Tag is not string id) return;

        var confirm = MessageBox.Show(this,
            $"Remove \"{item.SubItems[2].Text}\"?\n\nCustomers stop seeing it at their next check.",
            "Remove notification", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        UseWaitCursor = true;
        try
        {
            await _publisher.DeleteAsync(id);
            await LoadSentAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not remove it: {ex.Message}", "Remove notification",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    // ---------------- Settings ----------------

    private TabPage BuildSettingsTab()
    {
        var page = new TabPage("Settings") { BackColor = Background, Padding = new Padding(16) };

        var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Padding = new Padding(4) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        void AddRow(string label, Control control, int r)
        {
            layout.Controls.Add(new Label { Text = label, ForeColor = Heading, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 8, 8, 0) }, 0, r);
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0, 4, 0, 4);
            layout.Controls.Add(control, 1, r);
        }

        AddRow("GitHub owner", _ownerBox, 0);
        _ownerBox.PlaceholderText = "for example QuanitKhatri";
        AddRow("Repository", _repoBox, 1);
        _repoBox.PlaceholderText = "for example OrderBookingSystem";
        AddRow("Access token", _tokenBox, 2);
        _tokenBox.UseSystemPasswordChar = true;
        _tokenBox.PlaceholderText = "ghp_… (stored encrypted on this PC only)";

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
        var saveButton = new Button();
        StyleButton(saveButton, "Save", 110, primary: true);
        saveButton.Click += (_, _) => SaveSettings();
        var testButton = new Button();
        StyleButton(testButton, "Test connection", 150, primary: false);
        testButton.Margin = new Padding(8, 0, 0, 0);
        testButton.Click += async (_, _) => await TestConnectionAsync();
        var helpButton = new Button();
        StyleButton(helpButton, "How to get a token", 170, primary: false);
        helpButton.Margin = new Padding(8, 0, 0, 0);
        helpButton.Click += (_, _) => ShowTokenHelp();
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(testButton);
        buttons.Controls.Add(helpButton);
        layout.Controls.Add(new Label(), 0, 3);
        layout.Controls.Add(buttons, 1, 3);

        _settingsStatus.Dock = DockStyle.Top;
        _settingsStatus.Height = 60;
        _settingsStatus.ForeColor = Muted;
        _settingsStatus.Padding = new Padding(4, 12, 4, 0);

        var note = new Label
        {
            Dock = DockStyle.Top,
            Height = 90,
            ForeColor = Muted,
            Padding = new Padding(4, 8, 4, 0),
            Text =
                "The token is kept only on this computer, encrypted with your Windows account.\n" +
                "It needs write access to the repository so notifications and updates can be published.\n" +
                "Customers never need a token: they only read."
        };

        page.Controls.Add(note);
        page.Controls.Add(_settingsStatus);
        page.Controls.Add(layout);
        return page;
    }

    private void LoadSettingsIntoForm()
    {
        _ownerBox.Text = _settings.RepositoryOwner;
        _repoBox.Text = _settings.RepositoryName;
        _tokenBox.Text = _settings.GetToken() ?? string.Empty;
    }

    private void SaveSettings()
    {
        _settings.RepositoryOwner = _ownerBox.Text.Trim();
        _settings.RepositoryName = _repoBox.Text.Trim();
        _settings.SetToken(_tokenBox.Text);

        try
        {
            _settings.Save();
            _publisher = new BroadcastPublisher(_settings);
            _settingsStatus.ForeColor = Muted;
            _settingsStatus.Text = "Saved.";
        }
        catch (Exception ex)
        {
            _settingsStatus.ForeColor = Danger;
            _settingsStatus.Text = $"Could not save: {ex.Message}";
        }
    }

    private async Task TestConnectionAsync()
    {
        SaveSettings();
        _settingsStatus.ForeColor = Muted;
        _settingsStatus.Text = "Checking…";
        UseWaitCursor = true;
        try
        {
            _settingsStatus.Text = await _publisher.TestConnectionAsync();
        }
        catch (Exception ex)
        {
            _settingsStatus.ForeColor = Danger;
            _settingsStatus.Text = $"Not connected: {ex.Message}";
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void ShowTokenHelp()
    {
        var answer = MessageBox.Show(this,
            "On GitHub:\n\n" +
            "1. Sign in with the account that owns the repository.\n" +
            "2. Settings → Developer settings → Personal access tokens → Tokens (classic).\n" +
            "3. Generate new token (classic).\n" +
            "4. Tick the 'repo' box.\n" +
            "5. Copy the token and paste it here, then press Save.\n\n" +
            "Open the token page now?",
            "How to get a token", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

        if (answer != DialogResult.Yes) return;
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/settings/tokens") { UseShellExecute = true });
        }
        catch (Exception)
        {
            // The user can open it by hand.
        }
    }
}
