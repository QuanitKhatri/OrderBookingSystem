using Distribution.Services.Sales;
using Distribution.WinForms.Controls;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Sales;

public class MarketCreditControl : UserControl
{
    private readonly IMarketCreditService _service;
    private readonly ThemedGrid _grid;
    private readonly EmptyState _emptyState;
    private readonly Label _totalLabel;

    public MarketCreditControl(IMarketCreditService service)
    {
        _service = service;
        BackColor = Theme.Theme.Background;

        var totalCard = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, Title = "Market Credit", Subtitle = "Total outstanding across all customers", IconGlyph = Icons.PaymentCard };
        _totalLabel = new Label { Dock = DockStyle.Top, AutoSize = true, UseMnemonic = false, Font = Theme.Theme.KpiFont, ForeColor = Theme.Theme.Warning, Text = "Rs 0" };
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

    private async Task LoadAsync()
    {
        var rows = await _service.GetOutstandingAsync();
        _grid.Rows.Clear();
        foreach (var r in rows)
        {
            _grid.Rows.Add(r.Code, r.ShopName, r.AreaName, r.Balance, r.LastPaymentDate?.ToString("dd MMM yyyy") ?? "—");
        }
        _grid.RightAlignNumericColumns();
        _grid.Visible = rows.Count > 0;
        _emptyState.Visible = rows.Count == 0;
        _totalLabel.Text = $"Rs {rows.Sum(r => r.Balance):N0}";
    }
}
