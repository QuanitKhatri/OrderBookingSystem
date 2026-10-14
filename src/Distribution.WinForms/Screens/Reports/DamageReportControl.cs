using Distribution.Services.Reports;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Reports;

public class DamageReportControl : UserControl
{
    private readonly IReportService _service;
    private readonly DateTimePicker _fromDate;
    private readonly DateTimePicker _toDate;
    private readonly ComboBox _groupByCombo;
    private readonly ThemedButton _viewButton;
    private readonly ThemedGrid _grid;
    private readonly EmptyState _emptyState;

    public DamageReportControl(IReportService service)
    {
        _service = service;
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
