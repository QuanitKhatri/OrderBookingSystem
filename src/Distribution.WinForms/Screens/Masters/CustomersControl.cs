using Distribution.Services.Masters;
using Distribution.WinForms.Controls;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Masters;

public class CustomersControl : UserControl
{
    private readonly ICustomerService _service;
    private readonly IAreaService _areaService;
    private readonly string _currentUser;

    private readonly TextBox _codeBox;
    private readonly TextBox _shopNameBox;
    private readonly TextBox _ownerBox;
    private readonly TextBox _phoneBox;
    private readonly ComboBox _areaCombo;
    private readonly ThemedButton _addAreaButton;
    private readonly TextBox _addressBox;
    private readonly NumericStepperField _openingBalanceBox;
    private readonly NumericStepperField _defaultDiscountBox;
    private readonly ThemedButton _saveButton;
    private readonly ThemedButton _clearButton;
    private readonly ThemedButton _deleteButton;
    private readonly SearchBox _searchBox;
    private readonly ThemedGrid _grid;
    private readonly EmptyState _emptyState;

    private int? _editingId;
    private List<CustomerListItem> _currentRows = [];

    public CustomersControl(ICustomerService service, IAreaService areaService, string currentUser)
    {
        _service = service;
        _areaService = areaService;
        _currentUser = currentUser;
        BackColor = Theme.Theme.Background;

        var formCard = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoHeight = true,
            Title = "Customer",
            IconGlyph = Icons.Add
        };

        var row1 = Shared.FieldRow();
        row1.Height = 70;

        _codeBox = new TextBox { MaxLength = 30 };
        _shopNameBox = new TextBox();
        _ownerBox = new TextBox();
        _phoneBox = new TextBox();

        row1.Controls.Add(Shared.BadgeField("Code", Icons.Scan, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _codeBox, "Enter code...", 160));
        row1.Controls.Add(Shared.BadgeField("Shop Name", Icons.Shop, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _shopNameBox, "Enter shop name...", 220));
        row1.Controls.Add(Shared.BadgeField("Owner", Icons.Contact2, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _ownerBox, "Enter owner name...", 200));
        row1.Controls.Add(Shared.BadgeField("Phone", Icons.Phone, Theme.Theme.PastelOrange, Theme.Theme.AccentOrange, _phoneBox, "Enter phone number...", 200));

        var row2 = Shared.FieldRow();
        row2.Height = 70;

        _areaCombo = new ComboBox { DisplayMember = "Name", ValueMember = "Id", DropDownStyle = ComboBoxStyle.DropDownList };
        _addAreaButton = new ThemedButton { Variant = ThemedButtonVariant.Secondary, IconGlyph = Icons.Add, AccessibleName = "Add area" };

        _addressBox = new TextBox();
        _openingBalanceBox = new NumericStepperField { Maximum = 999999999, DecimalPlaces = 2 };
        _defaultDiscountBox = new NumericStepperField { Maximum = 100, DecimalPlaces = 2 };

        row2.Controls.Add(Shared.BadgeFieldWithAction("Area / Route", Icons.MapPin, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _areaCombo, "Select area / route...", _addAreaButton, 220));
        row2.Controls.Add(Shared.BadgeField("Address", Icons.Home, Theme.Theme.PastelOrange, Theme.Theme.AccentOrange, _addressBox, "Enter address...", 220));
        row2.Controls.Add(Shared.BadgeStepperField("Opening Balance", Icons.Library, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _openingBalanceBox, 210));
        row2.Controls.Add(Shared.BadgeStepperField("Default Discount %", Icons.Tag, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _defaultDiscountBox, 210));

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 16, 0, 0) };
        _saveButton = new ThemedButton { Text = "Save (Ctrl+S)", Width = 140 };
        _clearButton = new ThemedButton { Text = "Clear (Esc)", Width = 110, Variant = ThemedButtonVariant.Secondary, Margin = new Padding(8, 0, 0, 0) };
        _deleteButton = new ThemedButton { Text = "Delete", Width = 110, Variant = ThemedButtonVariant.Danger, Margin = new Padding(8, 0, 0, 0), Enabled = false };
        buttonRow.Controls.Add(_saveButton);
        buttonRow.Controls.Add(_clearButton);
        buttonRow.Controls.Add(_deleteButton);

        formCard.ContentArea.Controls.Add(buttonRow);
        formCard.ContentArea.Controls.Add(row2);
        formCard.ContentArea.Controls.Add(row1);

        var listCard = new CardPanel { Dock = DockStyle.Fill, Title = "Customers", IconGlyph = Icons.ReportDocument, Margin = new Padding(0, 16, 0, 0) };

        _searchBox = new SearchBox { Dock = DockStyle.Top, Placeholder = "Search customers..." };
        _grid = new ThemedGrid { Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0), Visible = false };
        _grid.Columns.Add("Code", "Code");
        _grid.Columns.Add("Shop", "Shop");
        _grid.Columns.Add("Area", "Area");
        var balCol = new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "Balance", ValueType = typeof(decimal) };
        _grid.Columns.Add(balCol);
        _grid.Columns.Add("Status", "Status");

        _emptyState = new EmptyState
        {
            Dock = DockStyle.Fill,
            IconGlyph = Icons.Mail,
            Title = "No customers found",
            Subtitle = "Add a new customer using the form above."
        };

        listCard.ContentArea.Controls.Add(_grid);
        listCard.ContentArea.Controls.Add(_emptyState);
        listCard.ContentArea.Controls.Add(Shared.Spacer());
        listCard.ContentArea.Controls.Add(_searchBox);

        Controls.Add(listCard);
        Controls.Add(formCard);

        _saveButton.Click += async (_, _) => await SaveAsync();
        _clearButton.Click += (_, _) => ClearForm();
        _deleteButton.Click += async (_, _) => await DeleteAsync();
        _addAreaButton.Click += async (_, _) => await AddAreaAsync();
        _searchBox.SearchTextChanged += async (_, _) => await LoadAsync();
        _grid.CellClick += (_, e) => { if (e.RowIndex >= 0) LoadIntoForm(e.RowIndex); };

        Load += async (_, _) =>
        {
            await LoadAreasAsync();
            await LoadAsync();
        };
    }

    private async Task LoadAreasAsync()
    {
        var areas = await _areaService.GetActiveLookupAsync();
        _areaCombo.DataSource = areas.Select(a => new { a.Id, a.Name }).ToList();
    }

    private async Task AddAreaAsync()
    {
        var name = PromptDialog.Show(FindForm()!, "New Area", "Area / route name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        await _areaService.CreateAsync(name, _currentUser);
        await LoadAreasAsync();
        Toast.Show(FindForm()!, "Area added.");
    }

    private async Task LoadAsync()
    {
        _currentRows = await _service.SearchAsync(_searchBox.Text);
        _grid.Rows.Clear();
        foreach (var row in _currentRows)
        {
            _grid.Rows.Add(row.Code, row.ShopName, row.AreaName, row.Balance, row.IsActive ? "Active" : "Inactive");
        }
        _grid.RightAlignNumericColumns();

        _grid.Visible = _currentRows.Count > 0;
        _emptyState.Visible = _currentRows.Count == 0;
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
        _shopNameBox.Text = model.ShopName;
        _ownerBox.Text = model.OwnerName;
        _phoneBox.Text = model.Phone;
        _areaCombo.SelectedValue = model.AreaId;
        _addressBox.Text = model.Address;
        _openingBalanceBox.Value = model.OpeningBalance;
        _defaultDiscountBox.Value = model.DefaultDiscountPct;
    }

    private void ClearForm()
    {
        _editingId = null;
        _codeBox.Clear();
        _shopNameBox.Clear();
        _ownerBox.Clear();
        _phoneBox.Clear();
        if (_areaCombo.Items.Count > 0) _areaCombo.SelectedIndex = 0;
        _addressBox.Clear();
        _openingBalanceBox.Value = 0;
        _defaultDiscountBox.Value = 0;
        _deleteButton.Enabled = false;
        _saveButton.Text = "Save (Ctrl+S)";
        _grid.ClearSelection();
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_codeBox.Text) || string.IsNullOrWhiteSpace(_shopNameBox.Text) || _areaCombo.SelectedValue is null)
        {
            Toast.Show(FindForm()!, "Code, Shop Name and Area are required.", ToastKind.Danger);
            return;
        }

        var model = new CustomerEditModel(
            _editingId ?? 0, _codeBox.Text, _shopNameBox.Text, _ownerBox.Text, _phoneBox.Text,
            (int)_areaCombo.SelectedValue, _addressBox.Text, _openingBalanceBox.Value, _defaultDiscountBox.Value);

        if (_editingId.HasValue)
        {
            await _service.UpdateAsync(model, _currentUser);
            Toast.Show(FindForm()!, "Customer updated.");
        }
        else
        {
            await _service.CreateAsync(model, _currentUser);
            Toast.Show(FindForm()!, "Customer saved.");
        }

        ClearForm();
        await LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (!_editingId.HasValue) return;

        var confirm = MessageBox.Show("Delete this customer?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        await _service.DeleteAsync(_editingId.Value, _currentUser);
        Toast.Show(FindForm()!, "Customer deleted.");
        ClearForm();
        await LoadAsync();
    }
}
