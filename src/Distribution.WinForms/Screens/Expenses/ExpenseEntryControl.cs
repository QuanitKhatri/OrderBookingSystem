using Distribution.Domain.Common;
using Distribution.Services.Expenses;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Expenses;

public class ExpenseEntryControl : UserControl
{
    private readonly IExpenseService _service;
    private readonly IExpenseCategoryService _categoryService;
    private readonly string _currentUser;

    private readonly DateTimePicker _dateBox;
    private readonly ComboBox _categoryCombo;
    private readonly ThemedButton _addCategoryButton;
    private readonly NumericStepperField _amountBox;
    private readonly ComboBox _paidFromCombo;
    private readonly TextBox _descriptionBox;
    private readonly ThemedButton _saveButton;
    private readonly ThemedGrid _grid;
    private readonly EmptyState _emptyState;

    public ExpenseEntryControl(IExpenseService service, IExpenseCategoryService categoryService, string currentUser)
    {
        _service = service;
        _categoryService = categoryService;
        _currentUser = currentUser;
        BackColor = Theme.Theme.Background;

        var formCard = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, Title = "Expense Entry", Subtitle = "Record a business or personal expense", IconGlyph = Icons.PaymentCard };

        var row1 = Shared.FieldRow();
        row1.Height = 70;
        _dateBox = new DateTimePicker { Format = DateTimePickerFormat.Short };
        _categoryCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id" };
        _addCategoryButton = new ThemedButton { Variant = ThemedButtonVariant.Secondary, IconGlyph = Icons.Add, AccessibleName = "Add expense category" };
        _amountBox = new NumericStepperField { Maximum = 9_999_999, DecimalPlaces = 2, Increment = 1 };

        row1.Controls.Add(Shared.BadgeField("Date", Icons.Calendar, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _dateBox, "", 190));
        row1.Controls.Add(Shared.BadgeFieldWithAction("Category", Icons.Tag, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _categoryCombo, "", _addCategoryButton, 230));
        row1.Controls.Add(Shared.BadgeStepperField("Amount", Icons.PaymentCard, Theme.Theme.PastelOrange, Theme.Theme.AccentOrange, _amountBox, 190));

        var row2 = Shared.FieldRow();
        row2.Height = 70;
        _paidFromCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Label", ValueMember = "Value" };
        _descriptionBox = new TextBox();

        row2.Controls.Add(Shared.BadgeField("Paid From", Icons.PaymentCard, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _paidFromCombo, "", 190));
        row2.Controls.Add(Shared.BadgeField("Description", Icons.Page, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _descriptionBox, "Optional description...", 300));

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 16, 0, 0) };
        _saveButton = new ThemedButton { Text = "Save (Ctrl+S)", Width = 150, IconGlyph = Icons.Save };
        buttonRow.Controls.Add(_saveButton);

        formCard.ContentArea.Controls.Add(buttonRow);
        formCard.ContentArea.Controls.Add(row2);
        formCard.ContentArea.Controls.Add(row1);

        var listCard = new CardPanel { Dock = DockStyle.Fill, Title = "Today's Expenses", IconGlyph = Icons.ReportDocument, Margin = new Padding(0, 16, 0, 0) };
        _grid = new ThemedGrid { Dock = DockStyle.Fill, Visible = false };
        _grid.Columns.Add("Voucher", "Voucher No");
        _grid.Columns.Add("Category", "Category");
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Amount", ValueType = typeof(decimal) });
        _grid.Columns.Add("PaidFrom", "Paid From");
        _grid.Columns.Add("Description", "Description");

        _emptyState = new EmptyState { Dock = DockStyle.Fill, IconGlyph = Icons.PaymentCard, Title = "No expenses today", Subtitle = "Record an expense using the form above." };
        listCard.ContentArea.Controls.Add(_grid);
        listCard.ContentArea.Controls.Add(_emptyState);

        Controls.Add(listCard);
        Controls.Add(formCard);

        _addCategoryButton.Click += async (_, _) => await AddCategoryAsync();
        _saveButton.Click += async (_, _) => await SaveAsync();

        Load += async (_, _) =>
        {
            _paidFromCombo.DataSource = Enum.GetValues<PaidBy>().Select(v => new { Value = v, Label = v.ToString() }).ToList();
            await LoadCategoriesAsync();
            await LoadAsync();
        };
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _categoryService.GetActiveLookupAsync();
        _categoryCombo.DataSource = categories.Select(c => new { c.Id, c.Name }).ToList();
    }

    private async Task AddCategoryAsync()
    {
        var name = PromptDialog.Show(FindForm()!, "New Expense Category", "Category name:");
        if (string.IsNullOrWhiteSpace(name)) return;

        await _categoryService.CreateAsync(name, ExpenseType.Business, _currentUser);
        await LoadCategoriesAsync();
        Toast.Show(FindForm()!, "Category added.");
    }

    private async Task LoadAsync()
    {
        var rows = await _service.GetTodaysExpensesAsync();
        _grid.Rows.Clear();
        foreach (var r in rows) _grid.Rows.Add(r.VoucherNo, r.CategoryName, r.Amount, r.PaidFrom, r.Description);
        _grid.RightAlignNumericColumns();
        _grid.Visible = rows.Count > 0;
        _emptyState.Visible = rows.Count == 0;
    }

    private async Task SaveAsync()
    {
        if (_categoryCombo.SelectedValue is not int categoryId || _amountBox.Value <= 0)
        {
            Toast.Show(FindForm()!, "Category and Amount are required.", ToastKind.Danger);
            return;
        }

        var model = new ExpenseInputModel(_dateBox.Value.Date, categoryId, _amountBox.Value, (PaidBy)_paidFromCombo.SelectedValue!, _descriptionBox.Text);
        await _service.RecordAsync(model, _currentUser);

        Toast.Show(FindForm()!, "Expense saved.");
        _amountBox.Value = 0;
        _descriptionBox.Clear();
        await LoadAsync();
    }
}
