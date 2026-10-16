using Distribution.Connect;
using Distribution.Services.Admin;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Forms;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Admin;

public class SettingsControl : UserControl, IRefreshableScreen
{
    private readonly ISettingsService _service;
    private readonly IBackupService _backupService;
    private readonly IUpdateService _updateService;
    private readonly string _currentUser;

    private readonly TextBox _companyNameBox;
    private readonly TextBox _companyAddressBox;
    private readonly TextBox _companyPhoneBox;
    private readonly TextBox _invoicePrefixBox;
    private readonly CheckBox _allowNegativeStockCheck;
    private readonly ThemedButton _saveButton;
    private readonly ThemedButton _backupButton;
    private readonly ThemedButton _restoreButton;
    private readonly ThemedButton _checkUpdateButton;
    private readonly Label _versionLabel;

    private int _defaultWarehouseId;

    public SettingsControl(ISettingsService service, IBackupService backupService, IUpdateService updateService, string currentUser)
    {
        _service = service;
        _backupService = backupService;
        _updateService = updateService;
        _currentUser = currentUser;
        BackColor = Theme.Theme.Background;

        var card = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, Title = "Company & Invoice Settings", Subtitle = "Shown on printed bills and used across the app", IconGlyph = Icons.Admin };

        var row1 = Shared.FieldRow();
        row1.Height = 70;
        _companyNameBox = new TextBox();
        _companyPhoneBox = new TextBox();
        row1.Controls.Add(Shared.BadgeField("Company Name", Icons.Shop, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _companyNameBox, "e.g. Al Noor Traders", 320));
        row1.Controls.Add(Shared.BadgeField("Phone", Icons.Phone, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _companyPhoneBox, "e.g. 0300-1234567", 220));

        var row2 = Shared.FieldRow();
        row2.Height = 70;
        row2.Margin = new Padding(0, 8, 0, 0);
        _companyAddressBox = new TextBox();
        row2.Controls.Add(Shared.BadgeField("Address", Icons.MapPin, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _companyAddressBox, "e.g. Main Market, Lahore", 540));

        var row3 = Shared.FieldRow();
        row3.Height = 70;
        row3.Margin = new Padding(0, 8, 0, 0);
        _invoicePrefixBox = new TextBox();
        row3.Controls.Add(Shared.BadgeField("Invoice Prefix", Icons.Page, Theme.Theme.PastelOrange, Theme.Theme.AccentOrange, _invoicePrefixBox, "e.g. BILL-", 200));

        _allowNegativeStockCheck = new CheckBox { Dock = DockStyle.Top, Text = "Allow negative stock (sell beyond available quantity)", AutoSize = true, Font = Theme.Theme.BaseFont, Margin = new Padding(0, 16, 0, 0) };

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 16, 0, 0) };
        _saveButton = new ThemedButton { Text = "Save Settings", Width = 160, IconGlyph = Icons.Save };
        buttonRow.Controls.Add(_saveButton);

        card.ContentArea.Controls.Add(buttonRow);
        card.ContentArea.Controls.Add(_allowNegativeStockCheck);
        card.ContentArea.Controls.Add(row3);
        card.ContentArea.Controls.Add(row2);
        card.ContentArea.Controls.Add(row1);

        var backupCard = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, Title = "Backup & Restore", Subtitle = "Export or restore the full database", IconGlyph = Icons.Upload, Margin = new Padding(0, 16, 0, 0) };
        var backupRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44 };
        _backupButton = new ThemedButton { Text = "Backup Database", Width = 170, IconGlyph = Icons.Upload };
        _restoreButton = new ThemedButton { Text = "Restore Database", Width = 170, Variant = ThemedButtonVariant.Secondary, Margin = new Padding(8, 0, 0, 0) };
        backupRow.Controls.Add(_backupButton);
        backupRow.Controls.Add(_restoreButton);
        backupCard.ContentArea.Controls.Add(backupRow);

        var updateCard = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, Title = "Software Update", Subtitle = "Check whether a newer version is available", IconGlyph = Icons.Download, Margin = new Padding(0, 16, 0, 0) };

        _versionLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Font = Theme.Theme.BaseFont,
            ForeColor = Theme.Theme.Text,
            UseMnemonic = false,
            Text = $"Installed version: {_updateService.CurrentVersion}"
        };

        var updateRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 8, 0, 0) };
        _checkUpdateButton = new ThemedButton { Text = "Check for Updates", Width = 190, IconGlyph = Icons.Sync };
        updateRow.Controls.Add(_checkUpdateButton);

        updateCard.ContentArea.Controls.Add(updateRow);
        updateCard.ContentArea.Controls.Add(_versionLabel);

        Controls.Add(updateCard);
        Controls.Add(backupCard);
        Controls.Add(card);

        _checkUpdateButton.Click += async (_, _) => await CheckForUpdateAsync();
        _saveButton.Click += async (_, _) => await SaveAsync();
        _backupButton.Click += async (_, _) => await BackupAsync();
        _restoreButton.Click += async (_, _) => await RestoreAsync();
        Load += async (_, _) => await LoadAsync();
    }

    private async Task BackupAsync()
    {
        using var dialog = new SaveFileDialog { Filter = "Database Backup (*.backup)|*.backup", FileName = $"distribution-{DateTime.Now:yyyyMMdd-HHmmss}.backup" };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

        try
        {
            await _backupService.BackupToFileAsync(dialog.FileName);
            Toast.Show(FindForm()!, "Backup created successfully.");
        }
        catch (Exception ex)
        {
            Toast.Show(FindForm()!, $"Backup failed: {ex.Message}", ToastKind.Danger);
        }
    }

    private async Task RestoreAsync()
    {
        using var dialog = new OpenFileDialog { Filter = "Database Backup (*.backup)|*.backup" };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

        var confirm = MessageBox.Show("Restoring will overwrite existing data. Continue?", "Confirm restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        try
        {
            await _backupService.RestoreFromFileAsync(dialog.FileName);
            Toast.Show(FindForm()!, "Database restored. Please restart the application.");
        }
        catch (Exception ex)
        {
            Toast.Show(FindForm()!, $"Restore failed: {ex.Message}", ToastKind.Danger);
        }
    }

    private async Task CheckForUpdateAsync()
    {
        _checkUpdateButton.Enabled = false;
        _checkUpdateButton.Text = "Checking…";
        try
        {
            var result = await _updateService.CheckAsync();
            _versionLabel.Text = $"Installed version: {result.CurrentVersion ?? _updateService.CurrentVersion}";

            if (result.HasUpdate)
            {
                using var form = new UpdateForm(_updateService, result);
                form.ShowDialog(FindForm());
                return;
            }

            var message = result.Status switch
            {
                UpdateCheckStatus.UpToDate => $"You already have the latest version ({result.CurrentVersion}).",
                UpdateCheckStatus.NotInstalled =>
                    "This copy was not installed by the setup program, so it cannot update itself.\n\n" +
                    "Ask your supplier for the installer to get automatic updates.",
                UpdateCheckStatus.Disabled => "Updates are switched off for this computer.",
                UpdateCheckStatus.NotConfigured => "No update source is set up for this computer.",
                _ => $"The update check did not work: {result.Error}"
            };
            MessageBox.Show(FindForm(), message, "Software Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            _checkUpdateButton.Enabled = true;
            _checkUpdateButton.Text = "Check for Updates";
        }
    }

    public Task RefreshAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        var model = await _service.GetAsync();
        _companyNameBox.Text = model.CompanyName;
        _companyAddressBox.Text = model.CompanyAddress;
        _companyPhoneBox.Text = model.CompanyPhone;
        _invoicePrefixBox.Text = model.InvoicePrefix;
        _allowNegativeStockCheck.Checked = model.AllowNegativeStock;
        _defaultWarehouseId = model.DefaultWarehouseId;
    }

    private async Task SaveAsync()
    {
        var model = new AppSettingsModel(
            _companyNameBox.Text.Trim(),
            _companyAddressBox.Text.Trim(),
            _companyPhoneBox.Text.Trim(),
            _invoicePrefixBox.Text.Trim(),
            _allowNegativeStockCheck.Checked,
            _defaultWarehouseId);

        await _service.SaveAsync(model, _currentUser);
        Toast.Show(FindForm()!, "Settings saved.");
    }
}
