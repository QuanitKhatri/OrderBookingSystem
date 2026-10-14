using Distribution.Domain.Common;
using Distribution.Services.Masters;
using Distribution.Services.Stock;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Stock;

public class DamageEntryControl : UserControl
{
    private readonly IDamageService _service;
    private readonly IDamageReasonService _reasonService;
    private readonly IProductService _productService;
    private readonly string _currentUser;

    private readonly DateTimePicker _dateBox;
    private readonly ComboBox _productCombo;
    private readonly NumericStepperField _qtyBox;
    private readonly ComboBox _reasonCombo;
    private readonly ThemedButton _addReasonButton;
    private readonly ComboBox _foundAtCombo;
    private readonly TextBox _remarksBox;
    private readonly ThemedButton _saveButton;
    private readonly ThemedGrid _grid;
    private readonly EmptyState _emptyState;

    private static readonly (FoundAt Value, string Label)[] FoundAtOptions =
    [
        (FoundAt.Godown, "Godown"),
        (FoundAt.InTransit, "In Transit"),
        (FoundAt.ReturnedByParty, "Returned by Party")
    ];

    public DamageEntryControl(IDamageService service, IDamageReasonService reasonService, IProductService productService, string currentUser)
    {
        _service = service;
        _reasonService = reasonService;
        _productService = productService;
        _currentUser = currentUser;
        BackColor = Theme.Theme.Background;

        var formCard = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoHeight = true,
            Title = "Damage Entry",
            Subtitle = "Record damaged or lost stock",
            IconGlyph = Icons.Admin
        };

        var row1 = Shared.FieldRow();
        row1.Height = 70;

        _dateBox = new DateTimePicker { Format = DateTimePickerFormat.Short };
        _productCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Label", ValueMember = "Id" };
        _qtyBox = new NumericStepperField { Maximum = 9_999_999, DecimalPlaces = 3, Increment = 1 };

        row1.Controls.Add(Shared.BadgeField("Date", Icons.Calendar, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _dateBox, "", 190));
        row1.Controls.Add(Shared.BadgeField("Product", Icons.Package, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _productCombo, "", 260));
        row1.Controls.Add(Shared.BadgeStepperField("Qty", Icons.Package, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _qtyBox, 190));

        var row2 = Shared.FieldRow();
        row2.Height = 70;

        _reasonCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id" };
        _addReasonButton = new ThemedButton { Variant = ThemedButtonVariant.Secondary, IconGlyph = Icons.Add, AccessibleName = "Add damage reason" };
        _foundAtCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Label", ValueMember = "Value" };
        _remarksBox = new TextBox();

        row2.Controls.Add(Shared.BadgeFieldWithAction("Reason", Icons.Tag, Theme.Theme.PastelOrange, Theme.Theme.AccentOrange, _reasonCombo, "", _addReasonButton, 220));
        row2.Controls.Add(Shared.BadgeField("Found At", Icons.MapPin, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _foundAtCombo, "", 200));
        row2.Controls.Add(Shared.BadgeField("Remarks", Icons.Page, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _remarksBox, "Optional remarks...", 260));

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 16, 0, 0) };
        _saveButton = new ThemedButton { Text = "Save (Ctrl+S)", Width = 150, IconGlyph = Icons.Save };
        buttonRow.Controls.Add(_saveButton);

        formCard.ContentArea.Controls.Add(buttonRow);
        formCard.ContentArea.Controls.Add(row2);
        formCard.ContentArea.Controls.Add(row1);

        var listCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Title = "Damage Register",
            IconGlyph = Icons.ReportDocument,
            Margin = new Padding(0, 16, 0, 0)
        };

        _grid = new ThemedGrid { Dock = DockStyle.Fill, Visible = false };
        _grid.Columns.Add("RefNo", "Ref No");
        _grid.Columns.Add("Date", "Date");
        _grid.Columns.Add("Product", "Product");
        var qtyCol = new DataGridViewTextBoxColumn { Name = "Qty", HeaderText = "Qty", ValueType = typeof(decimal) };
        _grid.Columns.Add(qtyCol);
        _grid.Columns.Add("Reason", "Reason");
        _grid.Columns.Add("FoundAt", "Found At");
        var lossCol = new DataGridViewTextBoxColumn { Name = "Loss", HeaderText = "Loss Amount", ValueType = typeof(decimal) };
        _grid.Columns.Add(lossCol);

        _emptyState = new EmptyState
        {
            Dock = DockStyle.Fill,
            IconGlyph = Icons.Admin,
            Title = "No damage entries recorded",
            Subtitle = "Record damaged or lost stock using the form above."
        };

        listCard.ContentArea.Controls.Add(_grid);
        listCard.ContentArea.Controls.Add(_emptyState);

        Controls.Add(listCard);
        Controls.Add(formCard);

        _saveButton.Click += async (_, _) => await SaveAsync();
        _addReasonButton.Click += async (_, _) => await AddReasonAsync();

        Load += async (_, _) =>
        {
            await LoadProductsAsync();
            await LoadReasonsAsync();
            _foundAtCombo.DataSource = FoundAtOptions.Select(o => new { o.Value, o.Label }).ToList();
            await LoadAsync();
        };
    }

    private async Task LoadProductsAsync()
    {
        var products = await _productService.GetActiveLookupAsync();
        _productCombo.DataSource = products.Select(p => new { p.Id, Label = $"{p.Code} - {p.Name}" }).ToList();
    }

    private async Task LoadReasonsAsync()
    {
        var reasons = await _reasonService.GetActiveLookupAsync();
        _reasonCombo.DataSource = reasons.Select(r => new { r.Id, r.Name }).ToList();
    }

    private async Task AddReasonAsync()
    {
        var name = PromptDialog.Show(FindForm()!, "New Damage Reason", "Reason name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        await _reasonService.CreateAsync(name, _currentUser);
        await LoadReasonsAsync();
        Toast.Show(FindForm()!, "Reason added.");
    }

    private async Task LoadAsync()
    {
        var rows = await _service.GetRecentAsync();
        _grid.Rows.Clear();
        foreach (var row in rows)
        {
            _grid.Rows.Add(row.RefNo, row.Date.ToString("dd MMM yyyy"), $"{row.ProductCode} - {row.ProductName}", row.Qty, row.Reason, FoundAtLabel(row.FoundAt), row.LossAmount);
        }
        _grid.RightAlignNumericColumns();

        _grid.Visible = rows.Count > 0;
        _emptyState.Visible = rows.Count == 0;
    }

    private static string FoundAtLabel(string raw)
    {
        return Enum.TryParse<FoundAt>(raw, out var value)
            ? FoundAtOptions.FirstOrDefault(o => o.Value == value).Label ?? raw
            : raw;
    }

    private async Task SaveAsync()
    {
        if (_productCombo.SelectedValue is null || _reasonCombo.SelectedValue is null || _foundAtCombo.SelectedValue is null)
        {
            Toast.Show(FindForm()!, "Product, Reason and Found At are required.", ToastKind.Danger);
            return;
        }

        if (_qtyBox.Value <= 0)
        {
            Toast.Show(FindForm()!, "Quantity must be greater than zero.", ToastKind.Danger);
            return;
        }

        var model = new DamageEntryModel(
            (int)_productCombo.SelectedValue, _dateBox.Value.Date, _qtyBox.Value,
            (int)_reasonCombo.SelectedValue, (FoundAt)_foundAtCombo.SelectedValue, _remarksBox.Text);

        try
        {
            await _service.RecordAsync(model, _currentUser);
            Toast.Show(FindForm()!, "Damage entry saved.");
            _qtyBox.Value = 0;
            _remarksBox.Clear();
            await LoadAsync();
        }
        catch (InsufficientStockException ex)
        {
            Toast.Show(FindForm()!, ex.Message, ToastKind.Danger);
        }
    }
}
