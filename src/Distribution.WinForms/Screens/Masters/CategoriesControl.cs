using Distribution.Services.Masters;
using Distribution.WinForms.Controls;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Masters;

public class CategoriesControl : UserControl, IRefreshableScreen
{
    private readonly ICategoryService _service;
    private readonly string _currentUser;

    private readonly TextBox _nameBox;
    private readonly TextBox _descriptionBox;
    private readonly ThemedButton _saveButton;
    private readonly ThemedButton _clearButton;
    private readonly ThemedButton _deleteButton;
    private readonly SearchBox _searchBox;
    private readonly ThemedGrid _grid;

    private int? _editingId;

    public CategoriesControl(ICategoryService service, string currentUser)
    {
        _service = service;
        _currentUser = currentUser;
        BackColor = Theme.Theme.Background;

        var formCard = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoHeight = true,
            Title = "Add Category",
            Subtitle = "Enter category details below",
            IconGlyph = Icons.Add
        };

        var fieldsRow = Shared.FieldRow();
        fieldsRow.Height = 70;

        _nameBox = new TextBox();
        _descriptionBox = new TextBox();

        fieldsRow.Controls.Add(Shared.BadgeField("Name", Icons.Tag, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _nameBox, "e.g. Beverages, Snacks, Household", 300));
        fieldsRow.Controls.Add(Shared.BadgeField("Description", Icons.Page, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _descriptionBox, "e.g. Soft drinks and juices", 380));

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 16, 0, 0) };
        _saveButton = new ThemedButton { Text = "Save (Ctrl+S)", Width = 140 };
        _clearButton = new ThemedButton { Text = "Clear (Esc)", Width = 110, Variant = ThemedButtonVariant.Secondary, Margin = new Padding(8, 0, 0, 0) };
        _deleteButton = new ThemedButton { Text = "Delete", Width = 110, Variant = ThemedButtonVariant.Danger, Margin = new Padding(8, 0, 0, 0), Enabled = false };
        buttonRow.Controls.Add(_saveButton);
        buttonRow.Controls.Add(_clearButton);
        buttonRow.Controls.Add(_deleteButton);

        formCard.ContentArea.Controls.Add(buttonRow);
        formCard.ContentArea.Controls.Add(fieldsRow);

        var listCard = new CardPanel { Dock = DockStyle.Fill, Title = "Categories", IconGlyph = Icons.Tag, Margin = new Padding(0, 16, 0, 0) };

        _searchBox = new SearchBox { Dock = DockStyle.Top, Placeholder = "Search categories..." };
        _grid = new ThemedGrid { Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
        _grid.Columns.Add("Name", "Name");
        _grid.Columns.Add("Description", "Description");
        var statusCol = new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", FillWeight = 60 };
        _grid.Columns.Add(statusCol);

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

    private List<CategoryListItem> _currentRows = [];

    public Task RefreshAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _currentRows = await _service.SearchAsync(_searchBox.Text);
        _grid.Rows.Clear();
        foreach (var row in _currentRows)
        {
            _grid.Rows.Add(row.Name, row.Description, row.IsActive ? "Active" : "Inactive");
        }
    }

    private void LoadIntoForm(int rowIndex)
    {
        var row = _currentRows[rowIndex];
        _editingId = row.Id;
        _nameBox.Text = row.Name;
        _descriptionBox.Text = row.Description;
        _deleteButton.Enabled = true;
        _saveButton.Text = "Update (Ctrl+S)";
    }

    private void ClearForm()
    {
        _editingId = null;
        _nameBox.Clear();
        _descriptionBox.Clear();
        _deleteButton.Enabled = false;
        _saveButton.Text = "Save (Ctrl+S)";
        _grid.ClearSelection();
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_nameBox.Text))
        {
            Toast.Show(FindForm()!, "Name is required.", ToastKind.Danger);
            return;
        }

        var model = new CategoryEditModel(_editingId ?? 0, _nameBox.Text, _descriptionBox.Text);

        if (_editingId.HasValue)
        {
            await _service.UpdateAsync(model, _currentUser);
            Toast.Show(FindForm()!, "Category updated.");
        }
        else
        {
            await _service.CreateAsync(model, _currentUser);
            Toast.Show(FindForm()!, "Category saved.");
        }

        ClearForm();
        await LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (!_editingId.HasValue) return;

        var confirm = MessageBox.Show("Delete this category?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
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

        Toast.Show(FindForm()!, "Category deleted.");
        ClearForm();
        await LoadAsync();
    }
}
