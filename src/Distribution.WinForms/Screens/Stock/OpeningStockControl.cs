using Distribution.Services.Masters;
using Distribution.Services.Stock;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Stock;

public class OpeningStockControl : UserControl, IRefreshableScreen
{
    private readonly IOpeningStockService _service;
    private readonly IProductService _productService;
    private readonly string _currentUser;

    private readonly DateTimePicker _dateBox;
    private readonly ComboBox _productCombo;
    private readonly NumericStepperField _qtyBox;
    private readonly NumericStepperField _costRateBox;
    private readonly ThemedButton _saveButton;
    private readonly ThemedGrid _grid;
    private readonly EmptyState _emptyState;

    public OpeningStockControl(IOpeningStockService service, IProductService productService, string currentUser)
    {
        _service = service;
        _productService = productService;
        _currentUser = currentUser;
        BackColor = Theme.Theme.Background;

        var formCard = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoHeight = true,
            Title = "Opening Stock",
            Subtitle = "One-time setup of starting stock for a product",
            IconGlyph = Icons.Add
        };

        var row = Shared.FieldRow();
        row.Height = 70;

        _dateBox = new DateTimePicker { Format = DateTimePickerFormat.Short };
        _productCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Label", ValueMember = "Id" };
        _qtyBox = new NumericStepperField { Maximum = 9_999_999, DecimalPlaces = 3, Increment = 1 };
        _costRateBox = new NumericStepperField { Maximum = 9_999_999, DecimalPlaces = 2, Increment = 1 };

        row.Controls.Add(Shared.BadgeField("As On Date", Icons.Calendar, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _dateBox, "", 190));
        row.Controls.Add(Shared.BadgeField("Product", Icons.Package, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _productCombo, "", 260));
        row.Controls.Add(Shared.BadgeStepperField("Qty", Icons.Package, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _qtyBox, 190));
        row.Controls.Add(Shared.BadgeStepperField("Cost Rate", Icons.Library, Theme.Theme.PastelOrange, Theme.Theme.AccentOrange, _costRateBox, 210));

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 16, 0, 0) };
        _saveButton = new ThemedButton { Text = "Save (Ctrl+S)", Width = 150, IconGlyph = Icons.Save };
        buttonRow.Controls.Add(_saveButton);

        formCard.ContentArea.Controls.Add(buttonRow);
        formCard.ContentArea.Controls.Add(row);

        var listCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Title = "Recent Opening Stock Entries",
            IconGlyph = Icons.ReportDocument,
            Margin = new Padding(0, 16, 0, 0)
        };

        _grid = new ThemedGrid { Dock = DockStyle.Fill, Visible = false };
        _grid.Columns.Add("Date", "Date");
        _grid.Columns.Add("Code", "Code");
        _grid.Columns.Add("Product", "Product");
        var qtyCol = new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "Qty", ValueType = typeof(decimal) };
        var rateCol = new DataGridViewTextBoxColumn { Name = "Rate", HeaderText = "Cost Rate", ValueType = typeof(decimal) };
        var valCol = new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "Value", ValueType = typeof(decimal) };
        _grid.Columns.Add(qtyCol);
        _grid.Columns.Add(rateCol);
        _grid.Columns.Add(valCol);

        _emptyState = new EmptyState
        {
            Dock = DockStyle.Fill,
            IconGlyph = Icons.Package,
            Title = "No opening stock recorded yet",
            Subtitle = "Record each product's starting quantity and cost using the form above."
        };

        listCard.ContentArea.Controls.Add(_grid);
        listCard.ContentArea.Controls.Add(_emptyState);

        Controls.Add(listCard);
        Controls.Add(formCard);

        _saveButton.Click += async (_, _) => await SaveAsync();

        Load += async (_, _) =>
        {
            await LoadProductsAsync();
            await LoadAsync();
        };
    }

    public async Task RefreshAsync()
    {
        await LoadProductsAsync();
        await LoadAsync();
    }

    private async Task LoadProductsAsync()
    {
        var products = await _productService.GetActiveLookupAsync();
        Shared.Rebind(_productCombo, products.Select(p => new { p.Id, Label = $"{p.Code} - {p.Name}" }).ToList());
    }

    private async Task LoadAsync()
    {
        var rows = await _service.GetRecentAsync();
        _grid.Rows.Clear();
        foreach (var row in rows)
        {
            _grid.Rows.Add(row.Date.ToString("dd MMM yyyy"), row.ProductCode, row.ProductName, row.Qty, row.CostRate, row.Value);
        }
        _grid.RightAlignNumericColumns();

        _grid.Visible = rows.Count > 0;
        _emptyState.Visible = rows.Count == 0;
    }

    private async Task SaveAsync()
    {
        if (_productCombo.SelectedValue is null)
        {
            Toast.Show(FindForm()!, "Select a product.", ToastKind.Danger);
            return;
        }

        if (_qtyBox.Value <= 0)
        {
            Toast.Show(FindForm()!, "Quantity must be greater than zero.", ToastKind.Danger);
            return;
        }

        var model = new OpeningStockEntryModel((int)_productCombo.SelectedValue, _dateBox.Value.Date, _qtyBox.Value, _costRateBox.Value);
        await _service.RecordAsync(model, _currentUser);

        Toast.Show(FindForm()!, "Opening stock saved.");
        _qtyBox.Value = 0;
        _costRateBox.Value = 0;
        await LoadAsync();
    }
}
