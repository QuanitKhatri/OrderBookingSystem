using Distribution.Services.Expenses;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Expenses;

public class ExpenseReportControl : UserControl
{
    private readonly IExpenseService _service;
    private readonly IExpenseCategoryService _categoryService;

    private readonly DateTimePicker _fromDate;
    private readonly DateTimePicker _toDate;
    private readonly ComboBox _categoryFilterCombo;
    private readonly ThemedButton _filterButton;

    private readonly ThemedGrid _detailGrid;
    private readonly ThemedGrid _summaryGrid;
    private readonly EmptyState _emptyState;

    public ExpenseReportControl(IExpenseService service, IExpenseCategoryService categoryService)
    {
        _service = service;
        _categoryService = categoryService;
        BackColor = Theme.Theme.Background;
        AutoScroll = true;

        var filterCard = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, Title = "Expense Report", Subtitle = "Filter expenses by date range and category", IconGlyph = Icons.ReportDocument };
        var row = Shared.FieldRow();
        row.Height = 70;

        _fromDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
        _toDate = new DateTimePicker { Format = DateTimePickerFormat.Short };
        _categoryFilterCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id" };

        row.Controls.Add(Shared.BadgeField("From", Icons.Calendar, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _fromDate, "", 170));
        row.Controls.Add(Shared.BadgeField("To", Icons.Calendar, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _toDate, "", 170));
        row.Controls.Add(Shared.BadgeField("Category", Icons.Tag, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _categoryFilterCombo, "", 220));

        var btnRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 16, 0, 0) };
        _filterButton = new ThemedButton { Text = "Apply Filter", Width = 150, IconGlyph = Icons.Search };
        btnRow.Controls.Add(_filterButton);

        filterCard.ContentArea.Controls.Add(btnRow);
        filterCard.ContentArea.Controls.Add(row);

        var summaryCard = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, FillMinHeight = 150, Margin = new Padding(0, 16, 0, 0), Title = "Category-wise Summary (Month to Date)", IconGlyph = Icons.Up };
        _summaryGrid = new ThemedGrid { Dock = DockStyle.Fill };
        _summaryGrid.Columns.Add("Category", "Category");
        _summaryGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Today", HeaderText = "Today", ValueType = typeof(decimal) });
        _summaryGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "MTD", HeaderText = "Month to Date", ValueType = typeof(decimal) });
        _summaryGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Share", HeaderText = "% Share", ValueType = typeof(decimal) });
        summaryCard.ContentArea.Controls.Add(_summaryGrid);

        var detailCard = new CardPanel { Dock = DockStyle.Fill, Title = "Expense Details", IconGlyph = Icons.ReportDocument, Margin = new Padding(0, 16, 0, 0), Height = 280 };
        _detailGrid = new ThemedGrid { Dock = DockStyle.Fill, Visible = false };
        _detailGrid.Columns.Add("Voucher", "Voucher No");
        _detailGrid.Columns.Add("Date", "Date");
        _detailGrid.Columns.Add("Category", "Category");
        _detailGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Amount", ValueType = typeof(decimal) });
        _detailGrid.Columns.Add("PaidFrom", "Paid From");

        _emptyState = new EmptyState { Dock = DockStyle.Fill, IconGlyph = Icons.ReportDocument, Title = "No expenses in this range", Subtitle = "Try a wider date range or different category." };
        detailCard.ContentArea.Controls.Add(_detailGrid);
        detailCard.ContentArea.Controls.Add(_emptyState);

        Controls.Add(detailCard);
        Controls.Add(summaryCard);
        Controls.Add(filterCard);

        _filterButton.Click += async (_, _) => await LoadDetailAsync();

        Load += async (_, _) =>
        {
            await LoadCategoriesAsync();
            await LoadSummaryAsync();
            await LoadDetailAsync();
        };
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _categoryService.GetActiveLookupAsync();
        var items = new List<object> { new { Id = (int?)null, Name = "All categories" } };
        items.AddRange(categories.Select(c => new { Id = (int?)c.Id, c.Name }));
        _categoryFilterCombo.DataSource = items;
    }

    private async Task LoadSummaryAsync()
    {
        var summary = await _service.GetCategorySummaryAsync();
        _summaryGrid.Rows.Clear();
        foreach (var s in summary) _summaryGrid.Rows.Add(s.CategoryName, s.TodayTotal, s.MonthToDateTotal, Math.Round(s.SharePct, 1));
        _summaryGrid.RightAlignNumericColumns();
    }

    private async Task LoadDetailAsync()
    {
        int? categoryId = _categoryFilterCombo.SelectedValue as int?;
        var rows = await _service.SearchAsync(_fromDate.Value.Date, _toDate.Value.Date, categoryId);
        _detailGrid.Rows.Clear();
        foreach (var r in rows) _detailGrid.Rows.Add(r.VoucherNo, r.Date.ToString("dd MMM yyyy"), r.CategoryName, r.Amount, r.PaidFrom);
        _detailGrid.RightAlignNumericColumns();
        _detailGrid.Visible = rows.Count > 0;
        _emptyState.Visible = rows.Count == 0;
    }
}
