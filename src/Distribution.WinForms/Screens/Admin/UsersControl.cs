using Distribution.Domain.Common;
using Distribution.Services.Admin;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Admin;

public class UsersControl : UserControl
{
    private readonly IUserService _service;
    private readonly string _currentUser;

    private readonly TextBox _usernameBox;
    private readonly TextBox _fullNameBox;
    private readonly ComboBox _roleCombo;
    private readonly ThemedButton _saveButton;
    private readonly ThemedButton _clearButton;
    private readonly ThemedButton _resetPasswordButton;
    private readonly ThemedButton _toggleActiveButton;
    private readonly SearchBox _searchBox;
    private readonly ThemedGrid _grid;

    private int? _editingId;
    private bool _editingActive;
    private List<UserListItem> _currentRows = [];

    public UsersControl(IUserService service, string currentUser)
    {
        _service = service;
        _currentUser = currentUser;
        BackColor = Theme.Theme.Background;

        var formCard = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoHeight = true,
            Title = "Add User",
            Subtitle = "Create a login for a staff member",
            IconGlyph = Icons.Add
        };

        var fieldsRow = Shared.FieldRow();
        fieldsRow.Height = 70;

        _usernameBox = new TextBox { MaxLength = 50 };
        _fullNameBox = new TextBox();
        _roleCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _roleCombo.Items.AddRange(Enum.GetNames<UserRole>());
        _roleCombo.SelectedIndex = 0;

        fieldsRow.Controls.Add(Shared.BadgeField("Username", Icons.Contact2, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _usernameBox, "e.g. aslam", 200));
        fieldsRow.Controls.Add(Shared.BadgeField("Full Name", Icons.Contact, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _fullNameBox, "e.g. Aslam Khan", 260));
        fieldsRow.Controls.Add(Shared.BadgeField("Role", Icons.Admin, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _roleCombo, "", 180));

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 16, 0, 0) };
        _saveButton = new ThemedButton { Text = "Save (Ctrl+S)", Width = 140 };
        _clearButton = new ThemedButton { Text = "Clear (Esc)", Width = 110, Variant = ThemedButtonVariant.Secondary, Margin = new Padding(8, 0, 0, 0) };
        _resetPasswordButton = new ThemedButton { Text = "Reset Password", Width = 150, Variant = ThemedButtonVariant.Secondary, Margin = new Padding(8, 0, 0, 0), Enabled = false };
        _toggleActiveButton = new ThemedButton { Text = "Deactivate", Width = 120, Variant = ThemedButtonVariant.Danger, Margin = new Padding(8, 0, 0, 0), Enabled = false };
        buttonRow.Controls.Add(_saveButton);
        buttonRow.Controls.Add(_clearButton);
        buttonRow.Controls.Add(_resetPasswordButton);
        buttonRow.Controls.Add(_toggleActiveButton);

        formCard.ContentArea.Controls.Add(buttonRow);
        formCard.ContentArea.Controls.Add(fieldsRow);

        var listCard = new CardPanel { Dock = DockStyle.Fill, Title = "Users", IconGlyph = Icons.Contact, Margin = new Padding(0, 16, 0, 0) };

        _searchBox = new SearchBox { Dock = DockStyle.Top, Placeholder = "Search users by username or full name..." };
        _grid = new ThemedGrid { Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
        _grid.Columns.Add("Username", "Username");
        _grid.Columns.Add("FullName", "Full Name");
        _grid.Columns.Add("Role", "Role");
        _grid.Columns.Add("Status", "Status");

        listCard.ContentArea.Controls.Add(_grid);
        listCard.ContentArea.Controls.Add(Shared.Spacer());
        listCard.ContentArea.Controls.Add(_searchBox);

        Controls.Add(listCard);
        Controls.Add(formCard);

        _saveButton.Click += async (_, _) => await SaveAsync();
        _clearButton.Click += (_, _) => ClearForm();
        _resetPasswordButton.Click += async (_, _) => await ResetPasswordAsync();
        _toggleActiveButton.Click += async (_, _) => await ToggleActiveAsync();
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
            _grid.Rows.Add(row.Username, row.FullName, row.Role.ToString(), row.IsActive ? "Active" : "Inactive");
        }
    }

    private void LoadIntoForm(int rowIndex)
    {
        var row = _currentRows[rowIndex];
        _editingId = row.Id;
        _editingActive = row.IsActive;
        _usernameBox.Text = row.Username;
        _fullNameBox.Text = row.FullName;
        _roleCombo.SelectedItem = row.Role.ToString();
        _resetPasswordButton.Enabled = true;
        _toggleActiveButton.Enabled = true;
        _toggleActiveButton.Text = row.IsActive ? "Deactivate" : "Activate";
        _saveButton.Text = "Update (Ctrl+S)";
    }

    private void ClearForm()
    {
        _editingId = null;
        _usernameBox.Clear();
        _fullNameBox.Clear();
        _roleCombo.SelectedIndex = 0;
        _resetPasswordButton.Enabled = false;
        _toggleActiveButton.Enabled = false;
        _saveButton.Text = "Save (Ctrl+S)";
        _grid.ClearSelection();
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_usernameBox.Text) || string.IsNullOrWhiteSpace(_fullNameBox.Text))
        {
            Toast.Show(FindForm()!, "Username and Full Name are required.", ToastKind.Danger);
            return;
        }

        var role = Enum.Parse<UserRole>((string)_roleCombo.SelectedItem!);
        var model = new UserEditModel(_editingId ?? 0, _usernameBox.Text, _fullNameBox.Text, role);

        try
        {
            if (_editingId.HasValue)
            {
                await _service.UpdateAsync(model, _currentUser);
                Toast.Show(FindForm()!, "User updated.");
            }
            else
            {
                var password = PromptDialog.Show(FindForm()!, "Set Password", "Enter an initial password for this user:");
                if (string.IsNullOrWhiteSpace(password)) return;

                await _service.CreateAsync(model, password, _currentUser);
                Toast.Show(FindForm()!, "User created.");
            }
        }
        catch (InvalidOperationException ex)
        {
            Toast.Show(FindForm()!, ex.Message, ToastKind.Danger);
            return;
        }

        ClearForm();
        await LoadAsync();
    }

    private async Task ResetPasswordAsync()
    {
        if (!_editingId.HasValue) return;

        var password = PromptDialog.Show(FindForm()!, "Reset Password", "Enter a new password for this user:");
        if (string.IsNullOrWhiteSpace(password)) return;

        await _service.ResetPasswordAsync(_editingId.Value, password, _currentUser);
        Toast.Show(FindForm()!, "Password reset.");
    }

    private async Task ToggleActiveAsync()
    {
        if (!_editingId.HasValue) return;

        var newState = !_editingActive;
        await _service.SetActiveAsync(_editingId.Value, newState, _currentUser);
        Toast.Show(FindForm()!, newState ? "User activated." : "User deactivated.");
        ClearForm();
        await LoadAsync();
    }
}
