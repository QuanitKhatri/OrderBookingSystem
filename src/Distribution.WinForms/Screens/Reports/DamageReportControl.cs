using Distribution.Reports;
using Distribution.Services.Admin;
using Distribution.Services.Reports;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Printing;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Reports;

public class DamageReportControl : UserControl, IRefreshableScreen
{
    private readonly IReportService _service;
    private readonly ISettingsService _settingsService;
    private readonly DateTimePicker _fromDate;
    private readonly DateTimePicker _toDate;
    private readonly ComboBox _groupByCombo;
    private readonly ThemedButton _viewButton;
    private readonly ThemedGrid _grid;
    private readonly EmptyState _emptyState;

    public DamageReportControl(IReportService service, ISettingsService settingsService)
    {
        _service = service;
        _settingsService = settingsService;
        BackColor = Theme.Theme.Background;

        var card = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, Title = "Damage Report", Subtitle = "Grouped loss summary for any date range", IconGlyph = Icons.Admin };
        var row = Shared.FieldRow();
        row.Height = 70;

        _fromDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddYears(-1) };
        _toDate = new DateTimePicker { Format = DateTimePickerFormat.Short };
        _groupByCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _groupByCombo.Items.AddRange(["Product", "Company", "Reason", "Month"]);
        _groupByCombo.SelectedIndex = 0;

        row.Controls.Add(Shared.BadgeField("From", Icons.Calendar, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _fromDate, "", 170));
        row.Controls.Add(Shared.BadgeField("To", Icons.Calendar, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _toDate, "", 170));
        row.Controls.Add(Shared.BadgeField("Group By", Icons.Tag, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _groupByCombo, "", 170));

        var btnRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 16, 0, 0) };
        _viewButton = new ThemedButton { Text = "View Report", Width = 150, IconGlyph = Icons.Search };
        btnRow.Controls.Add(_viewButton);
        ReportOutput.AddButtons(btnRow, BuildReportAsync);

        card.ContentArea.Controls.Add(btnRow);
        card.ContentArea.Controls.Add(row);

        var listCard = new CardPanel { Dock = DockStyle.Fill, Title = "Damage Summary", IconGlyph = Icons.ReportDocument, Margin = new Padding(0, 16, 0, 0) };
        _grid = new ThemedGrid { Dock = DockStyle.Fill, Visible = false };
        _grid.Columns.Add("Group", "Group");
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "Qty", ValueType = typeof(decimal) });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Loss", HeaderText = "Loss Amount", ValueType = typeof(decimal) });
        _grid.Columns.Add("TopReason", "Main Reason");

        _emptyState = new EmptyState { Dock = DockStyle.Fill, IconGlyph = Icons.Admin, Title = "No damage in this range", Subtitle = "Try a wider date range." };
        listCard.ContentArea.Controls.Add(_grid);
        listCard.ContentArea.Controls.Add(_emptyState);

        Controls.Add(listCard);
        Controls.Add(card);

        _viewButton.Click += async (_, _) => await LoadAsync();
        Load += async (_, _) => await LoadAsync();
    }

    public Task RefreshAsync() => LoadAsync();

    private async Task<ReportDocument?> BuildReportAsync()
    {
        var groupBy = _groupByCombo.SelectedItem as string ?? "Product";
        var subtitle = $"{_fromDate.Value:dd MMM yyyy} to {_toDate.Value:dd MMM yyyy}  |  Grouped by {groupBy}";
        var report = await ReportOutput.CreateAsync(_settingsService, "Damage Report", subtitle);

        var table = new ReportTable(null,
            new ReportColumn(groupBy, Width: 2.6f),
            new ReportColumn("Qty", ReportColumnKind.Quantity),
            new ReportColumn("Loss Amount", ReportColumnKind.Money),
            new ReportColumn("Main Reason", Width: 2f))
        {
            EmptyMessage = "No damage in this range."
        };

        decimal totalQty = 0, totalLoss = 0;
        foreach (DataGridViewRow gridRow in _grid.Rows)
        {
            var qty = gridRow.Cells[1].Value as decimal? ?? 0;
            var loss = gridRow.Cells[2].Value as decimal? ?? 0;
            totalQty += qty;
            totalLoss += loss;
            table.AddRow(gridRow.Cells[0].Value, qty, loss, gridRow.Cells[3].Value);
        }

        if (table.Rows.Count > 0) table.Totals = ["Total", totalQty, totalLoss, ""];
        report.Tables.Add(table);
        return report;
    }

    private async Task LoadAsync()
    {
        var rows = await _service.GetDamageReportAsync(_fromDate.Value.Date, _toDate.Value.Date);

        Func<DamageReportRow, string> keySelector = _groupByCombo.SelectedItem as string switch
        {
            "Company" => r => r.CompanyName,
            "Reason" => r => r.Reason,
            "Month" => r => r.Date.ToString("MMM yyyy"),
            _ => r => r.ProductName
        };

        var grouped = rows.GroupBy(keySelector)
            .Select(g => new
            {
                Group = g.Key,
                Qty = g.Sum(x => x.Qty),
                Loss = g.Sum(x => x.LossAmount),
                TopReason = g.GroupBy(x => x.Reason).OrderByDescending(rg => rg.Sum(x => x.LossAmount)).First().Key
            })
            .OrderByDescending(x => x.Loss)
            .ToList();

        _grid.Rows.Clear();
        foreach (var g in grouped) _grid.Rows.Add(g.Group, g.Qty, g.Loss, g.TopReason);
        _grid.RightAlignNumericColumns();
        _grid.Visible = grouped.Count > 0;
        _emptyState.Visible = grouped.Count == 0;
    }
}
