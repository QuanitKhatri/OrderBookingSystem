using Distribution.Services.Reports;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Reports;

public class DailySaleReportControl : UserControl
{
    private readonly IReportService _service;
    private readonly DateTimePicker _dateBox;
    private readonly ThemedButton _viewButton;
    private readonly ThemedGrid _billGrid;
    private readonly ThemedGrid _productGrid;
    private readonly ThemedGrid _companyGrid;

    public DailySaleReportControl(IReportService service)
    {
        _service = service;
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

    private async Task LoadAsync()
    {
        var date = _dateBox.Value.Date;

        var bills = await _service.GetDsrByBillAsync(date);
        _billGrid.Rows.Clear();
        foreach (var b in bills) _billGrid.Rows.Add(b.BillNo, b.CustomerName, b.Gross, b.Discount, b.Net, b.Cash, b.Credit);
        _billGrid.RightAlignNumericColumns();

        var products = await _service.GetDsrByProductAsync(date);
        _productGrid.Rows.Clear();
        foreach (var p in products) _productGrid.Rows.Add(p.ProductName, p.Qty, p.NetAmount);
        _productGrid.RightAlignNumericColumns();

        var companies = await _service.GetDsrByCompanyAsync(date);
        _companyGrid.Rows.Clear();
        foreach (var c in companies) _companyGrid.Rows.Add(c.CompanyName, c.NetAmount);
        _companyGrid.RightAlignNumericColumns();
    }
}
