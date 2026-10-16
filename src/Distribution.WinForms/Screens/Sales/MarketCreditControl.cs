using Distribution.Reports;
using Distribution.Services.Admin;
using Distribution.Services.Sales;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Printing;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Sales;

public class MarketCreditControl : UserControl, IRefreshableScreen
{
    private readonly IMarketCreditService _service;
    private readonly ISettingsService _settingsService;
    private readonly ThemedGrid _grid;
    private readonly EmptyState _emptyState;
    private readonly Label _totalLabel;

    private List<MarketCreditItem> _rows = [];

    public MarketCreditControl(IMarketCreditService service, ISettingsService settingsService)
    {
        _service = service;
        _settingsService = settingsService;
        BackColor = Theme.Theme.Background;

        var totalCard = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, Title = "Market Credit", Subtitle = "Total outstanding across all customers", IconGlyph = Icons.PaymentCard };
        _totalLabel = new Label { Dock = DockStyle.Top, AutoSize = true, UseMnemonic = false, Font = Theme.Theme.KpiFont, ForeColor = Theme.Theme.Warning, Text = "Rs 0" };

        var btnRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 12, 0, 0) };
        var refreshButton = new ThemedButton { Text = "Refresh", Width = 120, IconGlyph = Icons.Refresh };
        refreshButton.Click += async (_, _) => await LoadAsync();
        btnRow.Controls.Add(refreshButton);
        ReportOutput.AddButtons(btnRow, BuildReportAsync);

        totalCard.ContentArea.Controls.Add(btnRow);
        totalCard.ContentArea.Controls.Add(_totalLabel);

        var listCard = new CardPanel { Dock = DockStyle.Fill, Title = "Outstanding Balances", IconGlyph = Icons.ReportDocument, Margin = new Padding(0, 16, 0, 0) };
        _grid = new ThemedGrid { Dock = DockStyle.Fill, Visible = false };
        _grid.Columns.Add("Code", "Code");
        _grid.Columns.Add("Shop", "Shop");
        _grid.Columns.Add("Area", "Area");
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "Balance", ValueType = typeof(decimal) });
        _grid.Columns.Add("LastPayment", "Last Payment");

        _emptyState = new EmptyState { Dock = DockStyle.Fill, IconGlyph = Icons.PaymentCard, Title = "No outstanding credit", Subtitle = "All customers are settled." };
        listCard.ContentArea.Controls.Add(_grid);
        listCard.ContentArea.Controls.Add(_emptyState);

        Controls.Add(listCard);
        Controls.Add(totalCard);

        Load += async (_, _) => await LoadAsync();
    }

    public Task RefreshAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _rows = await _service.GetOutstandingAsync();
        _grid.Rows.Clear();
        foreach (var r in _rows)
        {
            _grid.Rows.Add(r.Code, r.ShopName, r.AreaName, r.Balance, r.LastPaymentDate?.ToString("dd MMM yyyy") ?? "—");
        }
        _grid.RightAlignNumericColumns();
        _grid.Visible = _rows.Count > 0;
        _emptyState.Visible = _rows.Count == 0;
        _totalLabel.Text = $"Rs {_rows.Sum(r => r.Balance):N0}";
    }

    private async Task<ReportDocument?> BuildReportAsync()
    {
        var report = await ReportOutput.CreateAsync(_settingsService, "Market Credit", $"Outstanding balances as on {DateTime.Now:dd MMM yyyy}");

        var table = new ReportTable(null,
            new ReportColumn("Code", Width: 1.1f),
            new ReportColumn("Shop", Width: 2.6f),
            new ReportColumn("Area", Width: 1.8f),
            new ReportColumn("Balance", ReportColumnKind.Money),
            new ReportColumn("Last Payment", Width: 1.3f))
        {
            EmptyMessage = "No outstanding credit."
        };

        foreach (var r in _rows)
            table.AddRow(r.Code, r.ShopName, r.AreaName, r.Balance, r.LastPaymentDate?.ToString("dd MMM yyyy") ?? "—");

        if (_rows.Count > 0) table.Totals = ["", "Total", "", _rows.Sum(r => r.Balance), ""];
        report.Tables.Add(table);
        return report;
    }
}
