using Distribution.Domain.Common;
using Distribution.Services.Masters;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Theme;

namespace Distribution.WinForms.Screens.Masters;

public class ProductsControl : UserControl, IRefreshableScreen
{
    private readonly IProductService _service;
    private readonly ICompanyService _companyService;
    private readonly ICategoryService _categoryService;
    private readonly string _currentUser;

    private readonly TextBox _codeBox;
    private readonly TextBox _nameBox;
    private readonly ComboBox _companyCombo;
    private readonly ComboBox _categoryCombo;
    private readonly ComboBox _unitCombo;
    private readonly NumericStepperField _pcsPerUnitBox;
    private readonly NumericStepperField _purchaseRateBox;
    private readonly NumericStepperField _saleRateBox;
    private readonly NumericStepperField _maxDiscountBox;
    private readonly ThemedButton _saveButton;
    private readonly ThemedButton _clearButton;
    private readonly ThemedButton _deleteButton;
    private readonly ThemedButton _importButton;

    private readonly SearchBox _searchBox;
    private readonly ComboBox _companyFilterCombo;
    private readonly ComboBox _categoryFilterCombo;
    private readonly ThemedGrid _grid;

    private int? _editingId;
    private List<ProductListItem> _currentRows = [];

    public ProductsControl(IProductService service, ICompanyService companyService, ICategoryService categoryService, string currentUser)
    {
        _service = service;
        _companyService = companyService;
        _categoryService = categoryService;
        _currentUser = currentUser;
        BackColor = Theme.Theme.Background;
        AutoScroll = true;

        // --- Basic Information card ---
        var basicCard = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoHeight = true,
            IconGlyph = Icons.Page,
            Title = "Basic Information",
            Subtitle = "Enter the product's basic details"
        };

        var row1 = Shared.FieldRow();

        _codeBox = new TextBox { MaxLength = 30 };
        _nameBox = new TextBox();
        _companyCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id" };
        _categoryCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id" };
        _unitCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _unitCombo.Items.AddRange(Enum.GetNames<UnitType>().Cast<object>().ToArray());
        _unitCombo.SelectedIndex = 0;

        row1.Controls.Add(Shared.IconField("Code", Icons.Scan, _codeBox, 160));
        row1.Controls.Add(Shared.IconField("Name", Icons.Package, _nameBox, 240));
        row1.Controls.Add(Shared.IconField("Company", Icons.Shop, _companyCombo, 200));
        row1.Controls.Add(Shared.IconField("Category", Icons.Tag, _categoryCombo, 200));
        row1.Controls.Add(Shared.IconField("Unit", Icons.Package, _unitCombo, 120));

        basicCard.ContentArea.Controls.Add(row1);

        // --- Pricing & Inventory card ---
        var pricingCard = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoHeight = true,
            Margin = new Padding(0, 16, 0, 0),
            IconGlyph = Icons.Library,
            Title = "Pricing & Inventory",
            Subtitle = "Enter pricing, discount and inventory details"
        };

        var row2 = Shared.FieldRow();

        _pcsPerUnitBox = new NumericStepperField { Maximum = 99999, DecimalPlaces = 3, Increment = 1 };
        _purchaseRateBox = new NumericStepperField { Maximum = 9999999, DecimalPlaces = 2, Increment = 1 };
        _saleRateBox = new NumericStepperField { Maximum = 9999999, DecimalPlaces = 2, Increment = 1 };
        _maxDiscountBox = new NumericStepperField { Maximum = 100, DecimalPlaces = 2, Increment = 1 };

        row2.Controls.Add(Shared.StepperField("Pcs per Unit", Icons.Package, _pcsPerUnitBox, 150));
        row2.Controls.Add(Shared.StepperField("Purchase Rate", null, _purchaseRateBox, 170));
        row2.Controls.Add(Shared.StepperField("Sale Rate", null, _saleRateBox, 170));
        row2.Controls.Add(Shared.StepperField("Max Discount %", null, _maxDiscountBox, 170));

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 16, 0, 0) };
        _saveButton = new ThemedButton { Text = "Save (Ctrl+S)", Width = 140, IconGlyph = Icons.Save };
        _clearButton = new ThemedButton { Text = "Clear (Esc)", Width = 120, Variant = ThemedButtonVariant.Secondary, Margin = new Padding(8, 0, 0, 0), IconGlyph = Icons.Refresh };
        _deleteButton = new ThemedButton { Text = "Delete", Width = 120, Variant = ThemedButtonVariant.Danger, Margin = new Padding(8, 0, 0, 0), Enabled = false, IconGlyph = Icons.Delete };
        _importButton = new ThemedButton { Text = "Import from Excel", Width = 190, Variant = ThemedButtonVariant.Secondary, Margin = new Padding(24, 0, 0, 0), IconGlyph = Icons.Upload };
        buttonRow.Controls.Add(_saveButton);
        buttonRow.Controls.Add(_clearButton);
        buttonRow.Controls.Add(_deleteButton);
        buttonRow.Controls.Add(_importButton);

        pricingCard.ContentArea.Controls.Add(buttonRow);
        pricingCard.ContentArea.Controls.Add(row2);

        // --- Products list card ---
        var listCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 16, 0, 0),
            IconGlyph = Icons.Package,
            Title = "Products",
            Subtitle = "Search and view your products"
        };

        var filterRow = new TableLayoutPanel { Dock = DockStyle.Top, Height = 46, ColumnCount = 3, RowCount = 1, Padding = new Padding(0, 0, 0, 10) };
        filterRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        _searchBox = new SearchBox { Dock = DockStyle.Fill, Margin = new Padding(0), Placeholder = "Search by code, name or barcode..." };
        _companyFilterCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id" };
        _categoryFilterCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id" };

        filterRow.Controls.Add(_searchBox, 0, 0);
        filterRow.Controls.Add(FilterPill(_companyFilterCombo), 1, 0);
        filterRow.Controls.Add(FilterPill(_categoryFilterCombo), 2, 0);

        _grid = new ThemedGrid { Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
        _grid.Columns.Add("Code", "Code");
        _grid.Columns.Add("Name", "Product");
        _grid.Columns.Add("Company", "Company");
        _grid.Columns.Add("Category", "Category");
        var avgCostCol = new DataGridViewTextBoxColumn { Name = "AvgCost", HeaderText = "Avg Cost", ValueType = typeof(decimal) };
        var saleRateCol = new DataGridViewTextBoxColumn { Name = "SaleRate", HeaderText = "Sale Rate", ValueType = typeof(decimal) };
        var stockCol = new DataGridViewTextBoxColumn { Name = "InStock", HeaderText = "In Stock", ValueType = typeof(decimal) };
        _grid.Columns.Add(avgCostCol);
        _grid.Columns.Add(saleRateCol);
        _grid.Columns.Add(stockCol);

        listCard.ContentArea.Controls.Add(_grid);
        listCard.ContentArea.Controls.Add(filterRow);

        Controls.Add(listCard);
        Controls.Add(pricingCard);
        Controls.Add(basicCard);

        _saveButton.Click += async (_, _) => await SaveAsync();
        _clearButton.Click += (_, _) => ClearForm();
        _deleteButton.Click += async (_, _) => await DeleteAsync();
        _importButton.Click += async (_, _) => await ImportAsync();
        _searchBox.SearchTextChanged += async (_, _) => await LoadAsync();
        _companyFilterCombo.SelectedIndexChanged += async (_, _) => { if (!_suppressFilterEvents) await LoadAsync(); };
        _categoryFilterCombo.SelectedIndexChanged += async (_, _) => { if (!_suppressFilterEvents) await LoadAsync(); };
        _grid.CellClick += (_, e) => { if (e.RowIndex >= 0) LoadIntoForm(e.RowIndex); };

        Load += async (_, _) =>
        {
            await LoadLookupsAsync();
            await LoadAsync();
        };
    }

    private static IconFieldPanel FilterPill(ComboBox combo)
    {
        var pill = new IconFieldPanel { Dock = DockStyle.Fill, Margin = new Padding(8, 0, 0, 0) };
        combo.FlatStyle = FlatStyle.Flat;
        combo.BackColor = Theme.Theme.InputBackground;
        combo.Font = Theme.Theme.BaseFont;
        combo.Dock = DockStyle.Fill;
        combo.GotFocus += (_, _) => pill.SetFocused(true);
        combo.LostFocus += (_, _) => pill.SetFocused(false);
        pill.Controls.Add(combo);
        return pill;
    }

    private bool _suppressFilterEvents;

    public async Task RefreshAsync()
    {
        await LoadLookupsAsync();
        await LoadAsync();
    }

    private async Task LoadLookupsAsync()
    {
        var companies = await _companyService.GetActiveLookupAsync();
        var categories = await _categoryService.GetActiveLookupAsync();

        _suppressFilterEvents = true;

        Shared.Rebind(_companyCombo, companies.Select(c => new { c.Id, c.Name }).ToList());
        Shared.Rebind(_categoryCombo, categories.Select(c => new { c.Id, c.Name }).ToList());

        var companyFilterItems = new List<object> { new { Id = (int?)null, Name = "All companies" } };
        companyFilterItems.AddRange(companies.Select(c => new { Id = (int?)c.Id, c.Name }));
        Shared.Rebind(_companyFilterCombo, companyFilterItems);

        var categoryFilterItems = new List<object> { new { Id = (int?)null, Name = "All categories" } };
        categoryFilterItems.AddRange(categories.Select(c => new { Id = (int?)c.Id, c.Name }));
        Shared.Rebind(_categoryFilterCombo, categoryFilterItems);

        _suppressFilterEvents = false;
    }

    private async Task LoadAsync()
    {
        int? companyId = _companyFilterCombo.SelectedValue as int?;
        int? categoryId = _categoryFilterCombo.SelectedValue as int?;

        _currentRows = await _service.SearchAsync(_searchBox.Text, companyId, categoryId);
        _grid.Rows.Clear();
        foreach (var row in _currentRows)
        {
            _grid.Rows.Add(row.Code, row.Name, row.CompanyName, row.CategoryName, row.AvgCost, row.SaleRate, row.InStock);
        }
        _grid.RightAlignNumericColumns();
    }

    private void LoadIntoForm(int rowIndex)
    {
        var row = _currentRows[rowIndex];
        _editingId = row.Id;
        LoadFull(row.Id);
        _deleteButton.Enabled = true;
        _saveButton.Text = "Update (Ctrl+S)";
    }

    private async void LoadFull(int id)
    {
        var model = await _service.GetAsync(id);
        if (model is null) return;

        _codeBox.Text = model.Code;
        _nameBox.Text = model.Name;
        _companyCombo.SelectedValue = model.CompanyId;
        _categoryCombo.SelectedValue = model.CategoryId;
        _unitCombo.SelectedItem = model.Unit.ToString();
        _pcsPerUnitBox.Value = model.PcsPerUnit;
        _purchaseRateBox.Value = model.PurchaseRate;
        _saleRateBox.Value = model.SaleRate;
        _maxDiscountBox.Value = model.MaxDiscountPct;
    }

    private void ClearForm()
    {
        _editingId = null;
        _codeBox.Clear();
        _nameBox.Clear();
        if (_companyCombo.Items.Count > 0) _companyCombo.SelectedIndex = 0;
        if (_categoryCombo.Items.Count > 0) _categoryCombo.SelectedIndex = 0;
        if (_unitCombo.Items.Count > 0) _unitCombo.SelectedIndex = 0;
        _pcsPerUnitBox.Value = 1;
        _purchaseRateBox.Value = 0;
        _saleRateBox.Value = 0;
        _maxDiscountBox.Value = 0;
        _deleteButton.Enabled = false;
        _saveButton.Text = "Save (Ctrl+S)";
        _grid.ClearSelection();
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_codeBox.Text) || string.IsNullOrWhiteSpace(_nameBox.Text) ||
            _companyCombo.SelectedValue is null || _categoryCombo.SelectedValue is null || _unitCombo.SelectedItem is null)
        {
            Toast.Show(FindForm()!, "Code, Name, Company, Category and Unit are required.", ToastKind.Danger);
            return;
        }

        var unit = Enum.Parse<UnitType>((string)_unitCombo.SelectedItem);
        var model = new ProductEditModel(
            _editingId ?? 0, _codeBox.Text, _nameBox.Text,
            (int)_companyCombo.SelectedValue, (int)_categoryCombo.SelectedValue,
            unit, _pcsPerUnitBox.Value, _purchaseRateBox.Value, _saleRateBox.Value,
            _maxDiscountBox.Value, null);

        if (_editingId.HasValue)
        {
            await _service.UpdateAsync(model, _currentUser);
            Toast.Show(FindForm()!, "Product updated.");
        }
        else
        {
            await _service.CreateAsync(model, _currentUser);
            Toast.Show(FindForm()!, "Product saved.");
        }

        ClearForm();
        await LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (!_editingId.HasValue) return;

        var confirm = MessageBox.Show("Delete this product?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        await _service.DeleteAsync(_editingId.Value, _currentUser);
        Toast.Show(FindForm()!, "Product deleted.");
        ClearForm();
        await LoadAsync();
    }

    private async Task ImportAsync()
    {
        using var dialog = new OpenFileDialog { Filter = "Excel Files|*.xlsx", Title = "Import Products" };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

        try
        {
            await using var stream = File.OpenRead(dialog.FileName);
            var result = await _service.ImportFromExcelAsync(stream, _currentUser);

            var message = $"Imported: {result.Imported}, Updated: {result.Updated}, Errors: {result.Errors.Count}";
            if (result.Errors.Count > 0)
            {
                var detail = string.Join(Environment.NewLine, result.Errors.Take(20).Select(e => $"Row {e.RowNumber}: {e.Error}"));
                MessageBox.Show(detail, message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                Toast.Show(FindForm()!, message);
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
