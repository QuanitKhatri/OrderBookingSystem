using Distribution.Services.Expenses;
using Distribution.Services.Reports;
using Distribution.Services.Sales;
using Distribution.WinForms.Controls;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens;

public class DashboardControl : UserControl, IRefreshableScreen
{
    public event EventHandler<string>? NavigateRequested;

    private readonly IReportService _reports;
    private readonly IExpenseService _expenses;
    private readonly IMarketCreditService _credit;

    private readonly KpiCard _netSaleKpi;
    private readonly KpiCard _expenseKpi;
    private readonly KpiCard _profitKpi;
    private readonly KpiCard _stockKpi;
    private readonly KpiCard _creditKpi;
    private readonly ThemedGrid _billGrid;
    private readonly EmptyState _billEmptyState;

    public DashboardControl(IReportService reports, IExpenseService expenses, IMarketCreditService credit)
    {
        _reports = reports;
        _expenses = expenses;
        _credit = credit;

        SuspendLayout();
        BackColor = Theme.Theme.Background;

        var kpiRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 130,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false
        };

        _netSaleKpi = MakeKpi("Today's Net Sale", "Rs 0", Icons.Up, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen);
        _expenseKpi = MakeKpi("Today's Expenses", "Rs 0", Icons.PaymentCard, Theme.Theme.PastelPink, Theme.Theme.AccentPink);
        _profitKpi = MakeKpi("Today's Net Profit", "Rs 0", Icons.Library, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen);
        _stockKpi = MakeKpi("Closing Stock Value", "Rs 0", Icons.Package, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue);
        _creditKpi = MakeKpi("Market Credit", "Rs 0", Icons.Contact2, Theme.Theme.PastelOrange, Theme.Theme.AccentOrange);

        kpiRow.Controls.Add(_netSaleKpi);
        kpiRow.Controls.Add(_expenseKpi);
        kpiRow.Controls.Add(_profitKpi);
        kpiRow.Controls.Add(_stockKpi);
        kpiRow.Controls.Add(_creditKpi);

        var newBillBtn = new ThemedButton { Text = "New Bill (F2)", Width = 150, IconGlyph = Icons.Page };
        var addExpenseBtn = new ThemedButton { Text = "Add Expense", Width = 150, Variant = ThemedButtonVariant.Secondary, Margin = new Padding(12, 0, 0, 0), IconGlyph = Icons.Add };
        newBillBtn.Click += (_, _) => NavigateRequested?.Invoke(this, "sales.billing");
        addExpenseBtn.Click += (_, _) => NavigateRequested?.Invoke(this, "expenses.entry");

        var quickButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Margin = new Padding(0, 16, 0, 0)
        };
        quickButtons.Controls.Add(newBillBtn);
        quickButtons.Controls.Add(addExpenseBtn);

        var billsCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Title = "Today's Bills",
            IconGlyph = Icons.Page,
            Margin = new Padding(0, 16, 0, 0)
        };

        _billGrid = new ThemedGrid { Dock = DockStyle.Fill, Visible = false };
        _billGrid.Columns.Add("BillNo", "Bill No");
        _billGrid.Columns.Add("Customer", "Customer");
        _billGrid.Columns.Add("Amount", "Amount");

        _billEmptyState = new EmptyState
        {
            Dock = DockStyle.Fill,
            IconGlyph = Icons.Page,
            Title = "No bills found for today",
            Subtitle = "Create a new bill to start your sales for today."
        };
        var emptyStateBillBtn = new ThemedButton { Text = "New Bill (F2)", Width = 150, IconGlyph = Icons.Page };
        emptyStateBillBtn.Click += (_, _) => NavigateRequested?.Invoke(this, "sales.billing");
        _billEmptyState.SetAction(emptyStateBillBtn);

        billsCard.ContentArea.Controls.Add(_billGrid);
        billsCard.ContentArea.Controls.Add(_billEmptyState);

        var bodyPanel = new Panel { Dock = DockStyle.Fill };
        bodyPanel.Controls.Add(billsCard);
        bodyPanel.Controls.Add(quickButtons);

        Controls.Add(bodyPanel);
        Controls.Add(kpiRow);
        ResumeLayout(true);

        Load += async (_, _) => await LoadAsync();
    }

    public Task RefreshAsync() => LoadAsync();

    /// <summary>
    /// Reads today's figures. The dashboard is the first thing opened after a sale or an expense is
    /// saved elsewhere, so it reloads every time rather than keeping what it showed before.
    /// </summary>
    private async Task LoadAsync()
    {
        var today = DateTime.Today;

        var bills = await _reports.GetDsrByBillAsync(today);
        var profitLoss = await _reports.GetProfitLossAsync(today, today);
        var closingStock = await _reports.GetDailyClosingStockAsync(today, null, null);
        var todaysExpenses = await _expenses.GetTodaysExpensesAsync();
        var outstanding = await _credit.GetOutstandingAsync();

        _netSaleKpi.Value = $"Rs {bills.Sum(b => b.Net):N0}";
        _expenseKpi.Value = $"Rs {todaysExpenses.Sum(e => e.Amount):N0}";
        _profitKpi.Value = $"Rs {profitLoss.NetProfit:N0}";
        _stockKpi.Value = $"Rs {closingStock.Sum(r => r.Value):N0}";
        _creditKpi.Value = $"Rs {outstanding.Sum(c => c.Balance):N0}";

        _billGrid.Rows.Clear();
        foreach (var b in bills)
        {
            _billGrid.Rows.Add(b.BillNo, b.CustomerName, b.Net);
        }
        _billGrid.RightAlignNumericColumns();
        _billGrid.Visible = bills.Count > 0;
        _billEmptyState.Visible = bills.Count == 0;
    }

    private static KpiCard MakeKpi(string label, string value, string glyph, Color badgeColor, Color accent)
    {
        return new KpiCard
        {
            Label = label,
            Value = value,
            IconGlyph = glyph,
            IconBadgeColor = badgeColor,
            IconColor = accent,
            AccentColor = accent,
            Margin = new Padding(0, 0, 16, 0)
        };
    }
}
