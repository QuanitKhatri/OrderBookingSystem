using Distribution.Reports;
using Distribution.Services.Admin;
using Distribution.Services.Masters;
using Distribution.Services.Sales;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Printing;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Sales;

public class PartyLedgerControl : UserControl, IRefreshableScreen
{
    private readonly ICustomerLedgerService _service;
    private readonly ICustomerService _customerService;
    private readonly ISettingsService _settingsService;

    private readonly ComboBox _customerCombo;
    private readonly DateTimePicker _fromDate;
    private readonly DateTimePicker _toDate;
    private readonly ThemedButton _viewButton;
    private readonly ThemedGrid _grid;
    private readonly EmptyState _emptyState;

    private List<LedgerEntry> _entries = [];

    public PartyLedgerControl(ICustomerLedgerService service, ICustomerService customerService, ISettingsService settingsService)
    {
        _service = service;
        _customerService = customerService;
        _settingsService = settingsService;
        BackColor = Theme.Theme.Background;

        var card = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, Title = "Party Ledger", Subtitle = "View a customer's transaction history", IconGlyph = Icons.ReportDocument };
        var row = Shared.FieldRow();
        row.Height = 70;

        _customerCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Label", ValueMember = "Id" };
        _fromDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
        _toDate = new DateTimePicker { Format = DateTimePickerFormat.Short };

        row.Controls.Add(Shared.BadgeField("Customer", Icons.Contact2, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _customerCombo, "", 280));
        row.Controls.Add(Shared.BadgeField("From", Icons.Calendar, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _fromDate, "", 170));
        row.Controls.Add(Shared.BadgeField("To", Icons.Calendar, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _toDate, "", 170));

        var btnRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 16, 0, 0) };
        _viewButton = new ThemedButton { Text = "View Ledger", Width = 150, IconGlyph = Icons.Search };
        btnRow.Controls.Add(_viewButton);
        ReportOutput.AddButtons(btnRow, BuildReportAsync);

        card.ContentArea.Controls.Add(btnRow);
        card.ContentArea.Controls.Add(row);

        var listCard = new CardPanel { Dock = DockStyle.Fill, Title = "Ledger Entries", IconGlyph = Icons.ReportDocument, Margin = new Padding(0, 16, 0, 0) };
        _grid = new ThemedGrid { Dock = DockStyle.Fill, Visible = false };
        _grid.Columns.Add("Date", "Date");
        _grid.Columns.Add("Type", "Type");
        _grid.Columns.Add("Reference", "Reference");
        foreach (var (name, header) in new[] { ("Debit", "Debit"), ("Credit", "Credit"), ("Balance", "Running Balance") })
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = header, ValueType = typeof(decimal) });
        }

        _emptyState = new EmptyState { Dock = DockStyle.Fill, IconGlyph = Icons.ReportDocument, Title = "Select a customer and date range", Subtitle = "Click View Ledger to see transaction history." };
        listCard.ContentArea.Controls.Add(_grid);
        listCard.ContentArea.Controls.Add(_emptyState);

        Controls.Add(listCard);
        Controls.Add(card);

        _viewButton.Click += async (_, _) => await LoadAsync();
        Load += async (_, _) => await LoadCustomersAsync();
    }

    public async Task RefreshAsync()
    {
        await LoadCustomersAsync();
        if (_grid.Visible) await LoadAsync();
    }

    private async Task LoadCustomersAsync()
    {
        var customers = await _customerService.SearchAsync(null);
        Shared.Rebind(_customerCombo, customers.Select(c => new { c.Id, Label = $"{c.Code} - {c.ShopName}" }).ToList());
    }

    private async Task LoadAsync()
    {
        if (_customerCombo.SelectedValue is not int customerId) return;

        _entries = await _service.GetLedgerAsync(customerId, _fromDate.Value.Date, _toDate.Value.Date);
        _grid.Rows.Clear();
        foreach (var e in _entries)
        {
            _grid.Rows.Add(e.Date.ToString("dd MMM yyyy"), e.Type, e.Reference, e.Debit, e.Credit, e.RunningBalance);
        }
        _grid.RightAlignNumericColumns();
        _grid.Visible = _entries.Count > 0;
        _emptyState.Visible = _entries.Count == 0;
    }

    private async Task<ReportDocument?> BuildReportAsync()
    {
        if (_entries.Count == 0)
        {
            Toast.Show(FindForm()!, "Click View Ledger first.", ToastKind.Warning);
            return null;
        }

        var subtitle = $"{_customerCombo.Text}  |  {_fromDate.Value:dd MMM yyyy} to {_toDate.Value:dd MMM yyyy}";
        var report = await ReportOutput.CreateAsync(_settingsService, "Party Ledger", subtitle);

        var table = new ReportTable(null,
            new ReportColumn("Date", ReportColumnKind.Date),
            new ReportColumn("Type"),
            new ReportColumn("Reference", Width: 1.6f),
            new ReportColumn("Debit", ReportColumnKind.Money),
            new ReportColumn("Credit", ReportColumnKind.Money),
            new ReportColumn("Running Balance", ReportColumnKind.Money, 1.3f));

        foreach (var e in _entries) table.AddRow(e.Date, e.Type, e.Reference, e.Debit, e.Credit, e.RunningBalance);
        table.Totals = ["", "", "Closing Balance", _entries.Sum(e => e.Debit), _entries.Sum(e => e.Credit), _entries[^1].RunningBalance];
        report.Tables.Add(table);
        return report;
    }
}
