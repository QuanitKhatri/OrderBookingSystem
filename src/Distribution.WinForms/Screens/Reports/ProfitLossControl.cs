using Distribution.Services.Reports;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Reports;

public class ProfitLossControl : UserControl
{
    private readonly IReportService _service;
    private readonly DateTimePicker _fromDate;
    private readonly DateTimePicker _toDate;
    private readonly ThemedButton _viewButton;
    private readonly CheckBox _withExpensesCheck;
    private readonly CheckBox _includeSelfCheck;
    private readonly ThemedGrid _grid;

    public ProfitLossControl(IReportService service)
    {
        _service = service;
        BackColor = Theme.Theme.Background;

        var card = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, Title = "Profit & Loss", Subtitle = "Net sale, cost, expenses and net profit", IconGlyph = Icons.Up };
        var row = Shared.FieldRow();
        row.Height = 70;

        _fromDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        _toDate = new DateTimePicker { Format = DateTimePickerFormat.Short };

        row.Controls.Add(Shared.BadgeField("From", Icons.Calendar, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _fromDate, "", 170));
        row.Controls.Add(Shared.BadgeField("To", Icons.Calendar, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _toDate, "", 170));

        var toggleRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 32, Margin = new Padding(0, 4, 0, 4) };
        _withExpensesCheck = new CheckBox { Text = "With Expenses", AutoSize = true, Checked = true, Font = Theme.Theme.BaseFont };
        _includeSelfCheck = new CheckBox { Text = "Include Self Expense", AutoSize = true, Margin = new Padding(24, 0, 0, 0), Font = Theme.Theme.BaseFont };
        toggleRow.Controls.Add(_withExpensesCheck);
        toggleRow.Controls.Add(_includeSelfCheck);

        var btnRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 8, 0, 0) };
        _viewButton = new ThemedButton { Text = "View Report", Width = 150, IconGlyph = Icons.Search };
        btnRow.Controls.Add(_viewButton);

        card.ContentArea.Controls.Add(btnRow);
        card.ContentArea.Controls.Add(toggleRow);
        card.ContentArea.Controls.Add(row);

        var resultCard = new CardPanel { Dock = DockStyle.Fill, Title = "Summary", IconGlyph = Icons.ReportDocument, Margin = new Padding(0, 16, 0, 0) };
        _grid = new ThemedGrid { Dock = DockStyle.Fill };
        _grid.Columns.Add("Line", "Line Item");
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Amount (Rs)", ValueType = typeof(decimal) });
        resultCard.ContentArea.Controls.Add(_grid);

        Controls.Add(resultCard);
        Controls.Add(card);

        _viewButton.Click += async (_, _) => await LoadAsync();
        _withExpensesCheck.CheckedChanged += (_, _) => { _includeSelfCheck.Enabled = _withExpensesCheck.Checked; };
        Load += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var result = await _service.GetProfitLossAsync(_fromDate.Value.Date, _toDate.Value.Date);

        _grid.Rows.Clear();
        _grid.Rows.Add("Gross Sale", result.GrossSale);
        _grid.Rows.Add("Discount", -result.Discount);
        _grid.Rows.Add("Net Sale", result.NetSale);
        _grid.Rows.Add("Cost of Goods Sold (COGS)", -result.Cogs);
        _grid.Rows.Add("Gross Profit", result.GrossProfit);
        _grid.Rows.Add("Damage Loss", -result.DamageLoss);
        _grid.Rows.Add("Profit Before Expense", result.ProfitBeforeExpense);

        if (_withExpensesCheck.Checked)
        {
            _grid.Rows.Add("Business Expenses", -result.BusinessExpenses);
            _grid.Rows.Add("Net Profit", result.NetProfit);

            if (_includeSelfCheck.Checked)
            {
                _grid.Rows.Add("Self Expenses", -result.SelfExpenses);
                _grid.Rows.Add("Net Profit (After Self Expense)", result.NetAfterSelf);
            }
        }

        _grid.RightAlignNumericColumns();
        foreach (DataGridViewRow gridRow in _grid.Rows)
        {
            var label = (string)gridRow.Cells[0].Value!;
            if (label.StartsWith("Net Profit") || label == "Gross Profit")
            {
                gridRow.DefaultCellStyle.Font = new Font(Theme.Theme.FontFamily, 10f, FontStyle.Bold);
            }
        }
    }
}
