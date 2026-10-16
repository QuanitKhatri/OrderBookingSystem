using Distribution.Services.Masters;
using Distribution.Services.Stock;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Stock;

public class StockReceivedControl : UserControl, IRefreshableScreen
{
    private readonly IPurchaseService _service;
    private readonly ISupplierService _supplierService;
    private readonly IProductService _productService;
    private readonly string _currentUser;

    private readonly ComboBox _supplierCombo;
    private readonly TextBox _invoiceBox;
    private readonly DateTimePicker _dateBox;
    private readonly TextBox _vehicleBox;
    private readonly NumericStepperField _biltiBox;

    private readonly ComboBox _lineProductCombo;
    private readonly NumericStepperField _lineQtyBox;
    private readonly NumericStepperField _lineRateBox;
    private readonly ThemedButton _addLineButton;

    private readonly ThemedGrid _linesGrid;
    private readonly Label _totalLabel;
    private readonly ThemedButton _saveButton;
    private readonly ThemedButton _clearButton;

    private readonly ThemedGrid _historyGrid;
    private readonly EmptyState _historyEmptyState;

    private readonly List<(int ProductId, string Code, string Name, decimal Qty, decimal Rate)> _lines = [];

    public StockReceivedControl(IPurchaseService service, ISupplierService supplierService, IProductService productService, string currentUser)
    {
        _service = service;
        _supplierService = supplierService;
        _productService = productService;
        _currentUser = currentUser;
        BackColor = Theme.Theme.Background;
        AutoScroll = true;

        // --- Purchase header card ---
        var headerCard = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoHeight = true,
            Title = "Stock Received",
            Subtitle = "Receive stock from a supplier",
            IconGlyph = Icons.Package
        };

        var headerRow = Shared.FieldRow();
        headerRow.Height = 70;

        _supplierCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Label", ValueMember = "Id" };
        _invoiceBox = new TextBox();
        _dateBox = new DateTimePicker { Format = DateTimePickerFormat.Short };
        _vehicleBox = new TextBox();
        _biltiBox = new NumericStepperField { Maximum = 9_999_999, DecimalPlaces = 2, Increment = 1 };

        headerRow.Controls.Add(Shared.BadgeField("Supplier", Icons.Car, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _supplierCombo, "", 220));
        headerRow.Controls.Add(Shared.BadgeField("Invoice No", Icons.Scan, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _invoiceBox, "Supplier invoice #...", 170));
        headerRow.Controls.Add(Shared.BadgeField("Date", Icons.Calendar, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _dateBox, "", 170));
        headerRow.Controls.Add(Shared.BadgeField("Vehicle No", Icons.Car, Theme.Theme.PastelOrange, Theme.Theme.AccentOrange, _vehicleBox, "Optional...", 160));
        headerRow.Controls.Add(Shared.BadgeStepperField("Bilti Charges", Icons.PaymentCard, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _biltiBox, 200));

        headerCard.ContentArea.Controls.Add(headerRow);

        // --- Add item line card ---
        var lineCard = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoHeight = true,
            FillMinHeight = 160,
            Margin = new Padding(0, 16, 0, 0),
            Title = "Items",
            Subtitle = "Add each product received on this invoice",
            IconGlyph = Icons.Add
        };

        var lineRow = Shared.FieldRow();
        lineRow.Height = 70;

        _lineProductCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Label", ValueMember = "Id" };
        _lineQtyBox = new NumericStepperField { Maximum = 9_999_999, DecimalPlaces = 3, Increment = 1 };
        _lineRateBox = new NumericStepperField { Maximum = 9_999_999, DecimalPlaces = 2, Increment = 1 };

        lineRow.Controls.Add(Shared.BadgeField("Product", Icons.Package, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _lineProductCombo, "", 280));
        lineRow.Controls.Add(Shared.BadgeStepperField("Qty", Icons.Package, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _lineQtyBox, 190));
        lineRow.Controls.Add(Shared.BadgeStepperField("Rate", Icons.Library, Theme.Theme.PastelOrange, Theme.Theme.AccentOrange, _lineRateBox, 210));

        var lineButtonRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 4, 0, 8) };
        _addLineButton = new ThemedButton { Text = "Add Line", Width = 140, Variant = ThemedButtonVariant.Secondary, IconGlyph = Icons.Add };
        lineButtonRow.Controls.Add(_addLineButton);

        _linesGrid = new ThemedGrid { Dock = DockStyle.Fill, Height = 120 };
        _linesGrid.Columns.Add("Code", "Code");
        _linesGrid.Columns.Add("Product", "Product");
        var lQtyCol = new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "Qty", ValueType = typeof(decimal) };
        var lRateCol = new DataGridViewTextBoxColumn { Name = "Rate", HeaderText = "Rate", ValueType = typeof(decimal) };
        var lAmtCol = new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Amount", ValueType = typeof(decimal) };
        _linesGrid.Columns.Add(lQtyCol);
        _linesGrid.Columns.Add(lRateCol);
        _linesGrid.Columns.Add(lAmtCol);

        _totalLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            Font = new Font(Theme.Theme.FontFamily, 11f, FontStyle.Bold),
            ForeColor = Theme.Theme.HeadingNavy,
            Text = "Total: Rs 0",
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(0, 8, 0, 0)
        };

        var saveRow = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40 };
        _saveButton = new ThemedButton { Text = "Save Purchase (Ctrl+S)", Width = 210, IconGlyph = Icons.Save };
        _clearButton = new ThemedButton { Text = "Clear (Esc)", Width = 120, Variant = ThemedButtonVariant.Secondary, Margin = new Padding(8, 0, 0, 0), IconGlyph = Icons.Refresh };
        saveRow.Controls.Add(_saveButton);
        saveRow.Controls.Add(_clearButton);

        // WinForms docks in reverse add order: the Fill grid must be added first so it takes only the
        // space left between the top rows (fields, Add Line) and the bottom rows (Total, Save).
        lineCard.ContentArea.Controls.Add(_linesGrid);
        lineCard.ContentArea.Controls.Add(_totalLabel);
        lineCard.ContentArea.Controls.Add(saveRow);
        lineCard.ContentArea.Controls.Add(lineButtonRow);
        lineCard.ContentArea.Controls.Add(lineRow);

        // --- Today's purchases history card ---
        var historyCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Title = "Today's Purchases",
            IconGlyph = Icons.ReportDocument,
            Margin = new Padding(0, 16, 0, 0),
            Height = 260
        };

        _historyGrid = new ThemedGrid { Dock = DockStyle.Fill, Visible = false };
        _historyGrid.Columns.Add("Invoice", "Invoice No");
        _historyGrid.Columns.Add("Date", "Date");
        _historyGrid.Columns.Add("Supplier", "Supplier");
        var hAmtCol = new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Total Amount", ValueType = typeof(decimal) };
        var hBiltiCol = new DataGridViewTextBoxColumn { Name = "Bilti", HeaderText = "Bilti", ValueType = typeof(decimal) };
        _historyGrid.Columns.Add(hAmtCol);
        _historyGrid.Columns.Add(hBiltiCol);

        _historyEmptyState = new EmptyState
        {
            Dock = DockStyle.Fill,
            IconGlyph = Icons.Package,
            Title = "No purchases received today",
            Subtitle = "Receive stock using the form above."
        };

        historyCard.ContentArea.Controls.Add(_historyGrid);
        historyCard.ContentArea.Controls.Add(_historyEmptyState);

        Controls.Add(historyCard);
        Controls.Add(lineCard);
        Controls.Add(headerCard);

        _addLineButton.Click += AddLine;
        _saveButton.Click += async (_, _) => await SavePurchaseAsync();
        _clearButton.Click += (_, _) => ClearAll();

        Load += async (_, _) =>
        {
            await LoadSuppliersAsync();
            await LoadProductsAsync();
            await LoadHistoryAsync();
        };
    }

    public async Task RefreshAsync()
    {
        await LoadSuppliersAsync();
        await LoadProductsAsync();
        await LoadHistoryAsync();
    }

    private async Task LoadSuppliersAsync()
    {
        var suppliers = await _supplierService.SearchAsync(null);
        Shared.Rebind(_supplierCombo, suppliers.Select(s => new { s.Id, Label = $"{s.Code} - {s.Name}" }).ToList());
    }

    private async Task LoadProductsAsync()
    {
        var products = await _productService.GetActiveLookupAsync();
        var items = products.Select(p => new { p.Id, Label = $"{p.Code} - {p.Name}" }).ToList();
        Shared.Rebind(_lineProductCombo, items);
    }

    private void AddLine(object? sender, EventArgs e)
    {
        if (_lineProductCombo.SelectedValue is null)
        {
            Toast.Show(FindForm()!, "Select a product.", ToastKind.Danger);
            return;
        }

        if (_lineQtyBox.Value <= 0 || _lineRateBox.Value <= 0)
        {
            Toast.Show(FindForm()!, "Qty and Rate must be greater than zero.", ToastKind.Danger);
            return;
        }

        var productId = (int)_lineProductCombo.SelectedValue;
        var label = _lineProductCombo.Text;
        var dashIndex = label.IndexOf(" - ", StringComparison.Ordinal);
        var code = dashIndex > 0 ? label[..dashIndex] : label;
        var name = dashIndex > 0 ? label[(dashIndex + 3)..] : label;

        _lines.Add((productId, code, name, _lineQtyBox.Value, _lineRateBox.Value));
        RefreshLinesGrid();

        _lineQtyBox.Value = 0;
        _lineRateBox.Value = 0;
    }

    private void RefreshLinesGrid()
    {
        _linesGrid.Rows.Clear();
        decimal total = 0;
        foreach (var line in _lines)
        {
            var amount = line.Qty * line.Rate;
            total += amount;
            _linesGrid.Rows.Add(line.Code, line.Name, line.Qty, line.Rate, amount);
        }
        _linesGrid.RightAlignNumericColumns();
        _totalLabel.Text = $"Total: Rs {total:N0}";
    }

    private async Task LoadHistoryAsync()
    {
        var rows = await _service.GetTodaysPurchasesAsync();
        _historyGrid.Rows.Clear();
        foreach (var row in rows)
        {
            _historyGrid.Rows.Add(row.InvoiceNo, row.Date.ToString("dd MMM yyyy"), row.SupplierName, row.TotalAmount, row.BiltiCharges);
        }
        _historyGrid.RightAlignNumericColumns();

        _historyGrid.Visible = rows.Count > 0;
        _historyEmptyState.Visible = rows.Count == 0;
    }

    private void ClearAll()
    {
        _invoiceBox.Clear();
        _vehicleBox.Clear();
        _biltiBox.Value = 0;
        _lines.Clear();
        RefreshLinesGrid();
    }

    private async Task SavePurchaseAsync()
    {
        if (_supplierCombo.SelectedValue is null || string.IsNullOrWhiteSpace(_invoiceBox.Text))
        {
            Toast.Show(FindForm()!, "Supplier and Invoice No are required.", ToastKind.Danger);
            return;
        }

        if (_lines.Count == 0)
        {
            Toast.Show(FindForm()!, "Add at least one item line.", ToastKind.Danger);
            return;
        }

        var model = new PurchaseInputModel(
            _invoiceBox.Text.Trim(),
            (int)_supplierCombo.SelectedValue,
            _dateBox.Value.Date,
            _vehicleBox.Text,
            _biltiBox.Value,
            null,
            _lines.Select(l => new PurchaseItemInput(l.ProductId, l.Qty, l.Rate)).ToList());

        await _service.CreateAsync(model, _currentUser);

        Toast.Show(FindForm()!, "Purchase saved and stock updated.");
        ClearAll();
        await LoadHistoryAsync();
    }
}
