using Distribution.WinForms.Controls;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens;

public class DashboardControl : UserControl
{
    public event EventHandler<string>? NavigateRequested;

    public DashboardControl()
    {
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

        kpiRow.Controls.Add(MakeKpi("Today's Net Sale", "Rs 0", Icons.Up, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen));
        kpiRow.Controls.Add(MakeKpi("Today's Expenses", "Rs 0", Icons.PaymentCard, Theme.Theme.PastelPink, Theme.Theme.AccentPink));
        kpiRow.Controls.Add(MakeKpi("Today's Net Profit", "Rs 0", Icons.Library, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen));
        kpiRow.Controls.Add(MakeKpi("Closing Stock Value", "Rs 0", Icons.Package, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue));
        kpiRow.Controls.Add(MakeKpi("Market Credit", "Rs 0", Icons.Contact2, Theme.Theme.PastelOrange, Theme.Theme.AccentOrange));

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

        var grid = new ThemedGrid { Dock = DockStyle.Fill, Visible = false };
        grid.Columns.Add("BillNo", "Bill No");
        grid.Columns.Add("Customer", "Customer");
        grid.Columns.Add("Amount", "Amount");

        var emptyState = new EmptyState
        {
            Dock = DockStyle.Fill,
            IconGlyph = Icons.Page,
            Title = "No bills found for today",
            Subtitle = "Create a new bill to start your sales for today."
        };
        var emptyStateBillBtn = new ThemedButton { Text = "New Bill (F2)", Width = 150, IconGlyph = Icons.Page };
        emptyStateBillBtn.Click += (_, _) => NavigateRequested?.Invoke(this, "sales.billing");
        emptyState.SetAction(emptyStateBillBtn);

        billsCard.ContentArea.Controls.Add(grid);
        billsCard.ContentArea.Controls.Add(emptyState);

        var bodyPanel = new Panel { Dock = DockStyle.Fill };
        bodyPanel.Controls.Add(billsCard);
        bodyPanel.Controls.Add(quickButtons);

        Controls.Add(bodyPanel);
        Controls.Add(kpiRow);
        ResumeLayout(true);
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
