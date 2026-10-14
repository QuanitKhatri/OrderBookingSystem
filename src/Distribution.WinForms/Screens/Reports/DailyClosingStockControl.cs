using Distribution.Services.Masters;
using Distribution.Services.Reports;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Reports;

public class DailyClosingStockControl : UserControl
{
    private readonly IReportService _service;
    private readonly ICompanyService _companyService;
    private readonly ICategoryService _categoryService;

    private readonly DateTimePicker _dateBox;
    private readonly ComboBox _companyFilterCombo;
    private readonly ComboBox _categoryFilterCombo;
    private readonly ThemedButton _viewButton;
    private readonly ThemedGrid _grid;
    private readonly EmptyState _emptyState;

    public DailyClosingStockControl(IReportService service, ICompanyService companyService, ICategoryService categoryService)
    {
        _service = service;
        _companyService = companyService;
        _categoryService = categoryService;
        BackColor = Theme.Theme.Background;

        var card = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, Title = "Daily Closing Stock", Subtitle = "Opening, received, sold, damage and closing per product", IconGlyph = Icons.Package };
        var row = Shared.FieldRow();
        row.Height = 70;

        _dateBox = new DateTimePicker { Format = DateTimePickerFormat.Short };
        _companyFilterCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id" };
        _categoryFilterCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id" };

        row.Controls.Add(Shared.BadgeField("Date", Icons.Calendar, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _dateBox, "", 170));
        row.Controls.Add(Shared.BadgeField("Company", Icons.Shop, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _companyFilterCombo, "", 220));
        row.Controls.Add(Shared.BadgeField("Category", Icons.Tag, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _categoryFilterCombo, "", 220));

        var btnRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 16, 0, 0) };
        _viewButton = new ThemedButton { Text = "View Report", Width = 150, IconGlyph = Icons.Search };
        btnRow.Controls.Add(_viewButton);

        card.ContentArea.Controls.Add(btnRow);
        card.ContentArea.Controls.Add(row);

        var listCard = new CardPanel { Dock = DockStyle.Fill, Title = "Stock Summary", IconGlyph = Icons.ReportDocument, Margin = new Padding(0, 16, 0, 0) };
        _grid = new ThemedGrid { Dock = DockStyle.Fill, Visible = false };
        _grid.Columns.Add("Code", "Code");
        _grid.Columns.Add("Product", "Product");
        _grid.Columns.Add("Company", "Company");
        foreach (var (name, header) in new[] { ("Opening", "Opening"), ("Received", "Received"), ("Sold", "Sold"), ("Damage", "Damage"), ("Closing", "Closing"), ("AvgCost", "Avg Cost"), ("Value", "Value") })
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = header, ValueType = typeof(decimal) });
        }

        _emptyState = new EmptyState { Dock = DockStyle.Fill, IconGlyph = Icons.Package, Title = "No products to show", Subtitle = "Add products under Masters > Products first." };
        listCard.ContentArea.Controls.Add(_grid);
        listCard.ContentArea.Controls.Add(_emptyState);

        Controls.Add(listCard);
        Controls.Add(card);

        _viewButton.Click += async (_, _) => await LoadAsync();

        Load += async (_, _) =>
        {
            var companies = await _companyService.GetActiveLookupAsync();
            var compItems = new List<object> { new { Id = (int?)null, Name = "All companies" } };
            compItems.AddRange(companies.Select(c => new { Id = (int?)c.Id, c.Name }));
            _companyFilterCombo.DataSource = compItems;

            var categories = await _categoryService.GetActiveLookupAsync();
            var catItems = new List<object> { new { Id = (int?)null, Name = "All categories" } };
            catItems.AddRange(categories.Select(c => new { Id = (int?)c.Id, c.Name }));
            _categoryFilterCombo.DataSource = catItems;

            await LoadAsync();
        };
    }

    private async Task LoadAsync()
    {
        int? companyId = _companyFilterCombo.SelectedValue as int?;
        int? categoryId = _categoryFilterCombo.SelectedValue as int?;
        var rows = await _service.GetDailyClosingStockAsync(_dateBox.Value.Date, companyId, categoryId);

        _grid.Rows.Clear();
        foreach (var r in rows) _grid.Rows.Add(r.Code, r.ProductName, r.CompanyName, r.Opening, r.Received, r.Sold, r.Damage, r.Closing, r.AvgCost, r.Value);
        _grid.RightAlignNumericColumns();
        _grid.Visible = rows.Count > 0;
        _emptyState.Visible = rows.Count == 0;
    }
}
