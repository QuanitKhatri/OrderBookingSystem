using Distribution.Services.Masters;
using Distribution.WinForms.Controls;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Masters;

public class SuppliersControl : UserControl
{
    private readonly ISupplierService _service;
    private readonly ICompanyService _companyService;
    private readonly string _currentUser;

    private readonly TextBox _codeBox;
    private readonly TextBox _nameBox;
    private readonly ComboBox _companyCombo;
    private readonly TextBox _contactBox;
    private readonly TextBox _phoneBox;
    private readonly TextBox _addressBox;
    private readonly NumericStepperField _openingBalanceBox;
    private readonly ThemedButton _saveButton;
    private readonly ThemedButton _clearButton;
    private readonly ThemedButton _deleteButton;
    private readonly SearchBox _searchBox;
    private readonly ThemedGrid _grid;
    private readonly EmptyState _emptyState;

    private int? _editingId;
    private List<SupplierListItem> _currentRows = [];

    public SuppliersControl(ISupplierService service, ICompanyService companyService, string currentUser)
    {
        _service = service;
        _companyService = companyService;
        _currentUser = currentUser;
        BackColor = Theme.Theme.Background;

        var formCard = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoHeight = true,
            Title = "Supplier",
            IconGlyph = Icons.Page
        };

        var row1 = Shared.FieldRow();
        row1.Height = 70;

        _codeBox = new TextBox { MaxLength = 30 };
        _nameBox = new TextBox();
        _companyCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id" };
        _contactBox = new TextBox();

        row1.Controls.Add(Shared.BadgeField("Code", Icons.Scan, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _codeBox, "Enter code...", 160));
        row1.Controls.Add(Shared.BadgeField("Name", Icons.Contact, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _nameBox, "Enter name...", 220));
        row1.Controls.Add(Shared.BadgeField("Company", Icons.Shop, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _companyCombo, "", 230));
        row1.Controls.Add(Shared.BadgeField("Contact Person", Icons.Contact2, Theme.Theme.PastelOrange, Theme.Theme.AccentOrange, _contactBox, "Enter contact person...", 220));

        var row2 = Shared.FieldRow();
        row2.Height = 70;

        _phoneBox = new TextBox();
        _addressBox = new TextBox();
        _openingBalanceBox = new NumericStepperField { Maximum = 999999999, DecimalPlaces = 2 };

        row2.Controls.Add(Shared.BadgeField("Phone", Icons.Phone, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _phoneBox, "Enter phone number...", 200));
        row2.Controls.Add(Shared.BadgeField("Address", Icons.MapPin, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _addressBox, "Enter address...", 260));
        row2.Controls.Add(Shared.BadgeStepperField("Opening Balance", Icons.Library, Theme.Theme.PastelOrange, Theme.Theme.AccentOrange, _openingBalanceBox, 210));

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

        var listCard = new CardPanel { Dock = DockStyle.Fill, Title = "Suppliers", IconGlyph = Icons.ReportDocument, Margin = new Padding(0, 16, 0, 0) };

        _searchBox = new SearchBox { Dock = DockStyle.Top, Placeholder = "Search suppliers..." };
        _grid = new ThemedGrid { Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0), Visible = false };
        _grid.Columns.Add("Code", "Code");
        _grid.Columns.Add("Name", "Supplier");
        _grid.Columns.Add("Company", "Company");
        var balCol = new DataGridViewTextBoxColumn { Name = "Balance", HeaderText = "Payable Balance", ValueType = typeof(decimal) };
        _grid.Columns.Add(balCol);
        _grid.Columns.Add("Status", "Status");

        _emptyState = new EmptyState
        {
            Dock = DockStyle.Fill,
            IconGlyph = Icons.Mail,
            Title = "No suppliers found",
            Subtitle = "Add a new supplier using the form above."
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
        _searchBox.SearchTextChanged += async (_, _) => await LoadAsync();
        _grid.CellClick += (_, e) => { if (e.RowIndex >= 0) LoadIntoForm(e.RowIndex); };

        Load += async (_, _) =>
        {
            await LoadCompaniesAsync();
            await LoadAsync();
        };
    }

    private async Task LoadCompaniesAsync()
    {
        var companies = await _companyService.GetActiveLookupAsync();
        _companyCombo.DataSource = companies.Select(c => new { c.Id, c.Name }).ToList();
    }

    private async Task LoadAsync()
    {
        _currentRows = await _service.SearchAsync(_searchBox.Text);
        _grid.Rows.Clear();
        foreach (var row in _currentRows)
        {
            _grid.Rows.Add(row.Code, row.Name, row.CompanyName, row.OpeningBalance, row.IsActive ? "Active" : "Inactive");
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
        _nameBox.Text = model.Name;
        _companyCombo.SelectedValue = model.CompanyId;
        _contactBox.Text = model.ContactPerson;
        _phoneBox.Text = model.Phone;
        _addressBox.Text = model.Address;
        _openingBalanceBox.Value = model.OpeningBalance;
    }

    private void ClearForm()
    {
        _editingId = null;
        _codeBox.Clear();
        _nameBox.Clear();
        if (_companyCombo.Items.Count > 0) _companyCombo.SelectedIndex = 0;
        _contactBox.Clear();
        _phoneBox.Clear();
        _addressBox.Clear();
        _openingBalanceBox.Value = 0;
        _deleteButton.Enabled = false;
        _saveButton.Text = "Save (Ctrl+S)";
        _grid.ClearSelection();
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_codeBox.Text) || string.IsNullOrWhiteSpace(_nameBox.Text) || _companyCombo.SelectedValue is null)
        {
            Toast.Show(FindForm()!, "Code, Name and Company are required.", ToastKind.Danger);
            return;
        }

        var model = new SupplierEditModel(
            _editingId ?? 0, _codeBox.Text, _nameBox.Text, (int)_companyCombo.SelectedValue,
            _contactBox.Text, _phoneBox.Text, _addressBox.Text, _openingBalanceBox.Value);

        if (_editingId.HasValue)
        {
            await _service.UpdateAsync(model, _currentUser);
            Toast.Show(FindForm()!, "Supplier updated.");
        }
        else
        {
            await _service.CreateAsync(model, _currentUser);
            Toast.Show(FindForm()!, "Supplier saved.");
        }

        ClearForm();
        await LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (!_editingId.HasValue) return;

        var confirm = MessageBox.Show("Delete this supplier?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        await _service.DeleteAsync(_editingId.Value, _currentUser);
        Toast.Show(FindForm()!, "Supplier deleted.");
        ClearForm();
        await LoadAsync();
    }
}
