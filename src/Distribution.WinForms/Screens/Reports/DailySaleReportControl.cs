using Distribution.Reports;
using Distribution.Services.Admin;
using Distribution.Services.Reports;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Printing;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Reports;

public class DailySaleReportControl : UserControl, IRefreshableScreen
{
    private readonly IReportService _service;
    private readonly ISettingsService _settingsService;
    private readonly DateTimePicker _dateBox;
    private readonly ThemedButton _viewButton;
    private readonly ThemedGrid _billGrid;
    private readonly ThemedGrid _productGrid;
    private readonly ThemedGrid _companyGrid;

    private List<DsrBillRow> _bills = [];
    private List<DsrProductRow> _productRows = [];
    private List<DsrCompanyRow> _companyRows = [];

    public DailySaleReportControl(IReportService service, ISettingsService settingsService)
    {
        _service = service;
        _settingsService = settingsService;
        BackColor = Theme.Theme.Background;
        AutoScroll = true;

        var filterCard = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, Title = "Daily Sale Report", Subtitle = "Bill-wise, product-wise and company-wise sales", IconGlyph = Icons.ReportDocument };
        _dateBox = new DateTimePicker { Format = DateTimePickerFormat.Short };
        var row = Shared.FieldRow();
        row.Height = 70;
        row.Controls.Add(Shared.BadgeField("Date", Icons.Calendar, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _dateBox, "", 190));

        var btnRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 16, 0, 0) };
        _viewButton = new ThemedButton { Text = "View Report", Width = 150, IconGlyph = Icons.Search };
        btnRow.Controls.Add(_viewButton);
        ReportOutput.AddButtons(btnRow, BuildReportAsync);
        filterCard.ContentArea.Controls.Add(btnRow);
        filterCard.ContentArea.Controls.Add(row);

        var billCard = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, FillMinHeight = 170, Margin = new Padding(0, 16, 0, 0), Title = "Bill-wise", IconGlyph = Icons.Page };
        _billGrid = new ThemedGrid { Dock = DockStyle.Fill };
        _billGrid.Columns.Add("BillNo", "Bill No");
        _billGrid.Columns.Add("Customer", "Customer");
        foreach (var (n, h) in new[] { ("Gross", "Gross"), ("Discount", "Discount"), ("Net", "Net"), ("Cash", "Cash"), ("Credit", "Credit") })
            _billGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = n, HeaderText = h, ValueType = typeof(decimal) });
        billCard.ContentArea.Controls.Add(_billGrid);

        var productCard = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, FillMinHeight = 150, Margin = new Padding(0, 16, 0, 0), Title = "Product-wise", IconGlyph = Icons.Package };
        _productGrid = new ThemedGrid { Dock = DockStyle.Fill };
        _productGrid.Columns.Add("Product", "Product");
        _productGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "Qty", ValueType = typeof(decimal) });
        _productGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Net", HeaderText = "Net Amount", ValueType = typeof(decimal) });
        productCard.ContentArea.Controls.Add(_productGrid);

        // Top + AutoHeight (not Fill) so the page scrolls instead of squeezing this card to its header.
        var companyCard = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, FillMinHeight = 150, Margin = new Padding(0, 16, 0, 0), Title = "Company-wise", IconGlyph = Icons.Shop };
        _companyGrid = new ThemedGrid { Dock = DockStyle.Fill };
        _companyGrid.Columns.Add("Company", "Company");
        _companyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Net", HeaderText = "Net Amount", ValueType = typeof(decimal) });
        companyCard.ContentArea.Controls.Add(_companyGrid);

        Controls.Add(companyCard);
        Controls.Add(productCard);
        Controls.Add(billCard);
        Controls.Add(filterCard);

        _viewButton.Click += async (_, _) => await LoadAsync();
        Load += async (_, _) => await LoadAsync();
    }

    public Task RefreshAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        var date = _dateBox.Value.Date;

        _bills = await _service.GetDsrByBillAsync(date);
        _billGrid.Rows.Clear();
        foreach (var b in _bills) _billGrid.Rows.Add(b.BillNo, b.CustomerName, b.Gross, b.Discount, b.Net, b.Cash, b.Credit);
        _billGrid.RightAlignNumericColumns();

        _productRows = await _service.GetDsrByProductAsync(date);
        _productGrid.Rows.Clear();
        foreach (var p in _productRows) _productGrid.Rows.Add(p.ProductName, p.Qty, p.NetAmount);
        _productGrid.RightAlignNumericColumns();

        _companyRows = await _service.GetDsrByCompanyAsync(date);
        _companyGrid.Rows.Clear();
        foreach (var c in _companyRows) _companyGrid.Rows.Add(c.CompanyName, c.NetAmount);
        _companyGrid.RightAlignNumericColumns();
    }

    private async Task<ReportDocument?> BuildReportAsync()
    {
        var report = await ReportOutput.CreateAsync(_settingsService, "Daily Sale Report", $"Date: {_dateBox.Value:dd MMM yyyy}");

        var billTable = new ReportTable("Bill-wise",
            new ReportColumn("Bill No", Width: 1.2f),
            new ReportColumn("Customer", Width: 2.4f),
            new ReportColumn("Gross", ReportColumnKind.Money),
            new ReportColumn("Discount", ReportColumnKind.Money),
            new ReportColumn("Net", ReportColumnKind.Money),
            new ReportColumn("Cash", ReportColumnKind.Money),
            new ReportColumn("Credit", ReportColumnKind.Money))
        {
            EmptyMessage = "No bills on this date."
        };
        foreach (var b in _bills) billTable.AddRow(b.BillNo, b.CustomerName, b.Gross, b.Discount, b.Net, b.Cash, b.Credit);
        if (_bills.Count > 0)
            billTable.Totals = ["Total", "", _bills.Sum(b => b.Gross), _bills.Sum(b => b.Discount), _bills.Sum(b => b.Net), _bills.Sum(b => b.Cash), _bills.Sum(b => b.Credit)];
        report.Tables.Add(billTable);

        var productTable = new ReportTable("Product-wise",
            new ReportColumn("Product", Width: 3f),
            new ReportColumn("Qty", ReportColumnKind.Quantity),
            new ReportColumn("Net Amount", ReportColumnKind.Money))
        {
            EmptyMessage = "No products sold on this date."
        };
        foreach (var p in _productRows) productTable.AddRow(p.ProductName, p.Qty, p.NetAmount);
        if (_productRows.Count > 0)
            productTable.Totals = ["Total", _productRows.Sum(p => p.Qty), _productRows.Sum(p => p.NetAmount)];
        report.Tables.Add(productTable);

        var companyTable = new ReportTable("Company-wise",
            new ReportColumn("Company", Width: 3f),
            new ReportColumn("Net Amount", ReportColumnKind.Money))
        {
            EmptyMessage = "No sales on this date."
        };
        foreach (var c in _companyRows) companyTable.AddRow(c.CompanyName, c.NetAmount);
        if (_companyRows.Count > 0)
            companyTable.Totals = ["Total", _companyRows.Sum(c => c.NetAmount)];
        report.Tables.Add(companyTable);

        return report;
    }
}
