using Distribution.Services.Masters;
using Distribution.WinForms.Controls;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Masters;

public class CompaniesControl : UserControl
{
    private readonly ICompanyService _service;
    private readonly string _currentUser;

    private readonly TextBox _codeBox;
    private readonly TextBox _nameBox;
    private readonly TextBox _contactBox;
    private readonly TextBox _phoneBox;
    private readonly ThemedButton _saveButton;
    private readonly ThemedButton _clearButton;
    private readonly ThemedButton _deleteButton;
    private readonly SearchBox _searchBox;
    private readonly ThemedGrid _grid;

    private int? _editingId;
    private List<CompanyListItem> _currentRows = [];

    public CompaniesControl(ICompanyService service, string currentUser)
    {
        _service = service;
        _currentUser = currentUser;
        BackColor = Theme.Theme.Background;

        var formCard = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoHeight = true,
            Title = "Add Company",
            Subtitle = "Enter company details below",
            IconGlyph = Icons.Add
        };

        var fieldsRow = Shared.FieldRow();
        fieldsRow.Height = 70;

        _codeBox = new TextBox { MaxLength = 20 };
        _nameBox = new TextBox();
        _contactBox = new TextBox();
        _phoneBox = new TextBox();

        fieldsRow.Controls.Add(Shared.BadgeField("Short Code", "", Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _codeBox, "e.g. UNI, P&G", 170, plainText: "Ab"));
        fieldsRow.Controls.Add(Shared.BadgeField("Name", Icons.Shop, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _nameBox, "e.g. Unilever Pakistan", 280));
        fieldsRow.Controls.Add(Shared.BadgeField("Contact Person", Icons.Contact2, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _contactBox, "e.g. Ahmed Khan", 230));
        fieldsRow.Controls.Add(Shared.BadgeField("Phone", Icons.Phone, Theme.Theme.PastelOrange, Theme.Theme.AccentOrange, _phoneBox, "e.g. 0300-1234567", 200));

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 16, 0, 0) };
        _saveButton = new ThemedButton { Text = "Save (Ctrl+S)", Width = 140 };
        _clearButton = new ThemedButton { Text = "Clear (Esc)", Width = 110, Variant = ThemedButtonVariant.Secondary, Margin = new Padding(8, 0, 0, 0) };
        _deleteButton = new ThemedButton { Text = "Delete", Width = 110, Variant = ThemedButtonVariant.Danger, Margin = new Padding(8, 0, 0, 0), Enabled = false };
        buttonRow.Controls.Add(_saveButton);
        buttonRow.Controls.Add(_clearButton);
        buttonRow.Controls.Add(_deleteButton);

        formCard.ContentArea.Controls.Add(buttonRow);
        formCard.ContentArea.Controls.Add(fieldsRow);

        var listCard = new CardPanel { Dock = DockStyle.Fill, Title = "Companies", IconGlyph = Icons.Shop, Margin = new Padding(0, 16, 0, 0) };

        _searchBox = new SearchBox { Dock = DockStyle.Top, Placeholder = "Search companies by code, name, contact person or phone..." };
        _grid = new ThemedGrid { Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
        _grid.Columns.Add("Code", "Code");
        _grid.Columns.Add("Name", "Company");
        _grid.Columns.Add("Contact", "Contact Person");
        _grid.Columns.Add("Phone", "Phone");
        _grid.Columns.Add("Status", "Status");

        listCard.ContentArea.Controls.Add(_grid);
        listCard.ContentArea.Controls.Add(Shared.Spacer());
        listCard.ContentArea.Controls.Add(_searchBox);

        Controls.Add(listCard);
        Controls.Add(formCard);

        _saveButton.Click += async (_, _) => await SaveAsync();
        _clearButton.Click += (_, _) => ClearForm();
        _deleteButton.Click += async (_, _) => await DeleteAsync();
        _searchBox.SearchTextChanged += async (_, _) => await LoadAsync();
        _grid.CellClick += (_, e) => { if (e.RowIndex >= 0) LoadIntoForm(e.RowIndex); };

        Load += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _currentRows = await _service.SearchAsync(_searchBox.Text);
        _grid.Rows.Clear();
        foreach (var row in _currentRows)
        {
            _grid.Rows.Add(row.Code, row.Name, row.ContactPerson, row.Phone, row.IsActive ? "Active" : "Inactive");
        }
    }

    private void LoadIntoForm(int rowIndex)
    {
        var row = _currentRows[rowIndex];
        _editingId = row.Id;
        _codeBox.Text = row.Code;
        _nameBox.Text = row.Name;
        _contactBox.Text = row.ContactPerson;
        _phoneBox.Text = row.Phone;
        _deleteButton.Enabled = true;
        _saveButton.Text = "Update (Ctrl+S)";
    }

    private void ClearForm()
    {
        _editingId = null;
        _codeBox.Clear();
        _nameBox.Clear();
        _contactBox.Clear();
        _phoneBox.Clear();
        _deleteButton.Enabled = false;
        _saveButton.Text = "Save (Ctrl+S)";
        _grid.ClearSelection();
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_codeBox.Text) || string.IsNullOrWhiteSpace(_nameBox.Text))
        {
            Toast.Show(FindForm()!, "Code and Name are required.", ToastKind.Danger);
            return;
        }

        var model = new CompanyEditModel(_editingId ?? 0, _codeBox.Text, _nameBox.Text, _contactBox.Text, _phoneBox.Text);

        if (_editingId.HasValue)
        {
            await _service.UpdateAsync(model, _currentUser);
            Toast.Show(FindForm()!, "Company updated.");
        }
        else
        {
            await _service.CreateAsync(model, _currentUser);
            Toast.Show(FindForm()!, "Company saved.");
        }

        ClearForm();
        await LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (!_editingId.HasValue) return;

        var confirm = MessageBox.Show("Delete this company?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        try
        {
            await _service.DeleteAsync(_editingId.Value, _currentUser);
        }
        catch (InvalidOperationException ex)
        {
            Toast.Show(FindForm()!, ex.Message, ToastKind.Danger);
            return;
        }

        Toast.Show(FindForm()!, "Company deleted.");
        ClearForm();
        await LoadAsync();
    }
}
