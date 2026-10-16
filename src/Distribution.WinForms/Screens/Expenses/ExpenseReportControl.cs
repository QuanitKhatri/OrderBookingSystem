using Distribution.Reports;
using Distribution.Services.Admin;
using Distribution.Services.Expenses;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Printing;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Expenses;

public class ExpenseReportControl : UserControl, IRefreshableScreen
{
    private readonly IExpenseService _service;
    private readonly IExpenseCategoryService _categoryService;
    private readonly ISettingsService _settingsService;

    private readonly DateTimePicker _fromDate;
    private readonly DateTimePicker _toDate;
    private readonly ComboBox _categoryFilterCombo;
    private readonly ThemedButton _filterButton;

    private readonly ThemedGrid _detailGrid;
    private readonly ThemedGrid _summaryGrid;
    private readonly EmptyState _emptyState;

    private List<ExpenseCategorySummary> _summary = [];
    private List<ExpenseListItem> _details = [];

    public ExpenseReportControl(IExpenseService service, IExpenseCategoryService categoryService, ISettingsService settingsService)
    {
        _service = service;
        _categoryService = categoryService;
        _settingsService = settingsService;
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
        ReportOutput.AddButtons(btnRow, BuildReportAsync);

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

        // Apply Filter also refreshes the month-to-date card, which otherwise never reloaded.
        _filterButton.Click += async (_, _) =>
        {
            await LoadSummaryAsync();
            await LoadDetailAsync();
        };

        Load += async (_, _) =>
        {
            await LoadCategoriesAsync();
            await LoadSummaryAsync();
            await LoadDetailAsync();
        };
    }

    public async Task RefreshAsync()
    {
        await LoadCategoriesAsync();
        await LoadSummaryAsync();
        await LoadDetailAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _categoryService.GetActiveLookupAsync();
        var items = new List<object> { new { Id = (int?)null, Name = "All categories" } };
        items.AddRange(categories.Select(c => new { Id = (int?)c.Id, c.Name }));
        Shared.Rebind(_categoryFilterCombo, items);
    }

    private async Task LoadSummaryAsync()
    {
        _summary = await _service.GetCategorySummaryAsync();
        _summaryGrid.Rows.Clear();
        foreach (var s in _summary) _summaryGrid.Rows.Add(s.CategoryName, s.TodayTotal, s.MonthToDateTotal, Math.Round(s.SharePct, 1));
        _summaryGrid.RightAlignNumericColumns();
    }

    private async Task<ReportDocument?> BuildReportAsync()
    {
        var subtitle = $"{_fromDate.Value:dd MMM yyyy} to {_toDate.Value:dd MMM yyyy}  |  {_categoryFilterCombo.Text}";
        var report = await ReportOutput.CreateAsync(_settingsService, "Expense Report", subtitle);

        var summaryTable = new ReportTable("Category-wise Summary (Month to Date)",
            new ReportColumn("Category", Width: 2.4f),
            new ReportColumn("Today", ReportColumnKind.Money),
            new ReportColumn("Month to Date", ReportColumnKind.Money),
            new ReportColumn("% Share", ReportColumnKind.Percent))
        {
            EmptyMessage = "No expenses this month."
        };
        foreach (var s in _summary) summaryTable.AddRow(s.CategoryName, s.TodayTotal, s.MonthToDateTotal, Math.Round(s.SharePct, 1));
        if (_summary.Count > 0)
            summaryTable.Totals = ["Total", _summary.Sum(s => s.TodayTotal), _summary.Sum(s => s.MonthToDateTotal), null];
        report.Tables.Add(summaryTable);

        var detailTable = new ReportTable("Expense Details",
            new ReportColumn("Voucher No", Width: 1.3f),
            new ReportColumn("Date", ReportColumnKind.Date),
            new ReportColumn("Category", Width: 2f),
            new ReportColumn("Amount", ReportColumnKind.Money),
            new ReportColumn("Paid From"),
            new ReportColumn("Description", Width: 2.4f))
        {
            EmptyMessage = "No expenses in this range."
        };
        foreach (var r in _details) detailTable.AddRow(r.VoucherNo, r.Date, r.CategoryName, r.Amount, r.PaidFrom, r.Description);
        if (_details.Count > 0)
            detailTable.Totals = ["Total", null, "", _details.Sum(r => r.Amount), "", ""];
        report.Tables.Add(detailTable);

        return report;
    }

    private async Task LoadDetailAsync()
    {
        int? categoryId = _categoryFilterCombo.SelectedValue as int?;
        _details = await _service.SearchAsync(_fromDate.Value.Date, _toDate.Value.Date, categoryId);
        _detailGrid.Rows.Clear();
        foreach (var r in _details) _detailGrid.Rows.Add(r.VoucherNo, r.Date.ToString("dd MMM yyyy"), r.CategoryName, r.Amount, r.PaidFrom);
        _detailGrid.RightAlignNumericColumns();
        _detailGrid.Visible = _details.Count > 0;
        _emptyState.Visible = _details.Count == 0;
    }
}
