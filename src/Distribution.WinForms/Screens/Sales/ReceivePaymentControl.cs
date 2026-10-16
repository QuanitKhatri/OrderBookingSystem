using Distribution.Domain.Common;
using Distribution.Services.Masters;
using Distribution.Services.Sales;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Sales;

public class ReceivePaymentControl : UserControl, IRefreshableScreen
{
    private readonly ICustomerReceiptService _service;
    private readonly ICustomerService _customerService;
    private readonly string _currentUser;

    private readonly ComboBox _customerCombo;
    private readonly DateTimePicker _dateBox;
    private readonly NumericStepperField _amountBox;
    private readonly ComboBox _paidByCombo;
    private readonly TextBox _remarksBox;
    private readonly Label _balanceLabel;
    private readonly ThemedButton _saveButton;
    private readonly ThemedButton _clearButton;
    private readonly ThemedGrid _grid;
    private readonly EmptyState _emptyState;

    private List<CustomerListItem> _customers = [];

    public ReceivePaymentControl(ICustomerReceiptService service, ICustomerService customerService, string currentUser)
    {
        _service = service;
        _customerService = customerService;
        _currentUser = currentUser;
        BackColor = Theme.Theme.Background;

        var formCard = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoHeight = true,
            Title = "Receive Payment",
            Subtitle = "Record cash or bank payment received from a customer",
            IconGlyph = Icons.PaymentCard
        };

        var row1 = Shared.FieldRow();
        row1.Height = 70;

        _customerCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Label", ValueMember = "Id" };
        _dateBox = new DateTimePicker { Format = DateTimePickerFormat.Short };
        _amountBox = new NumericStepperField { Maximum = 999_999_999, DecimalPlaces = 2, Increment = 1 };

        row1.Controls.Add(Shared.BadgeField("Customer", Icons.Contact2, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _customerCombo, "", 280));
        row1.Controls.Add(Shared.BadgeField("Date", Icons.Calendar, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _dateBox, "", 180));
        row1.Controls.Add(Shared.BadgeStepperField("Amount Received", Icons.PaymentCard, Theme.Theme.PastelOrange, Theme.Theme.AccentOrange, _amountBox, 220));

        var row2 = Shared.FieldRow();
        row2.Height = 70;

        _paidByCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Label", ValueMember = "Value" };
        _remarksBox = new TextBox();

        row2.Controls.Add(Shared.BadgeField("Received In", Icons.Library, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _paidByCombo, "", 190));
        row2.Controls.Add(Shared.BadgeField("Remarks", Icons.Page, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _remarksBox, "Optional remarks...", 320));

        _balanceLabel = new Label
        {
            Font = new Font(Theme.Theme.FontFamily, 10f, FontStyle.Bold),
            ForeColor = Theme.Theme.Warning,
            AutoSize = true,
            UseMnemonic = false,
            // Clear of the three fields in row 1 (280 + 180 + 220 wide, each with a 20px right margin).
            Location = new Point(760, 41),
            Text = "Current Balance: Rs 0"
        };

        var buttonRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 16, 0, 0) };
        _saveButton = new ThemedButton { Text = "Save Receipt (Ctrl+S)", Width = 200, IconGlyph = Icons.Save };
        _clearButton = new ThemedButton { Text = "Clear (Esc)", Width = 120, Variant = ThemedButtonVariant.Secondary, Margin = new Padding(8, 0, 0, 0), IconGlyph = Icons.Refresh };
        buttonRow.Controls.Add(_saveButton);
        buttonRow.Controls.Add(_clearButton);

        formCard.ContentArea.Controls.Add(buttonRow);
        formCard.ContentArea.Controls.Add(row2);
        formCard.ContentArea.Controls.Add(_balanceLabel);
        formCard.ContentArea.Controls.Add(row1);

        var listCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Title = "Recent Receipts",
            Subtitle = "Includes cash taken on bills",
            IconGlyph = Icons.ReportDocument,
            Margin = new Padding(0, 16, 0, 0)
        };

        _grid = new ThemedGrid { Dock = DockStyle.Fill, Visible = false };
        _grid.Columns.Add("ReceiptNo", "Receipt No");
        _grid.Columns.Add("Date", "Date");
        _grid.Columns.Add("Customer", "Customer");
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Amount", ValueType = typeof(decimal) });
        _grid.Columns.Add("PaidBy", "Received In");
        _grid.Columns.Add("Against", "Against Bill");
        _grid.Columns.Add("Remarks", "Remarks");

        _emptyState = new EmptyState
        {
            Dock = DockStyle.Fill,
            IconGlyph = Icons.PaymentCard,
            Title = "No payments recorded yet",
            Subtitle = "Record a payment using the form above."
        };

        listCard.ContentArea.Controls.Add(_grid);
        listCard.ContentArea.Controls.Add(_emptyState);

        Controls.Add(listCard);
        Controls.Add(formCard);

        _customerCombo.SelectedIndexChanged += (_, _) => UpdateBalanceLabel();
        _saveButton.Click += async (_, _) => await SaveAsync();
        _clearButton.Click += (_, _) => ClearForm();

        Load += async (_, _) =>
        {
            _paidByCombo.DataSource = Enum.GetValues<PaidBy>().Select(v => new { Value = v, Label = v.ToString() }).ToList();
            await LoadCustomersAsync();
            await LoadReceiptsAsync();
        };
    }

    public async Task RefreshAsync()
    {
        await LoadCustomersAsync();
        await LoadReceiptsAsync();
    }

    private async Task LoadCustomersAsync()
    {
        _customers = await _customerService.SearchAsync(null);
        Shared.Rebind(_customerCombo, _customers.Select(c => new { c.Id, Label = $"{c.Code} - {c.ShopName}" }).ToList());
        UpdateBalanceLabel();
    }

    private void UpdateBalanceLabel()
    {
        var balance = _customerCombo.SelectedValue is int id
            ? _customers.FirstOrDefault(c => c.Id == id)?.Balance ?? 0
            : 0;
        _balanceLabel.Text = $"Current Balance: Rs {balance:N0}";
    }

    private async Task LoadReceiptsAsync()
    {
        var rows = await _service.GetRecentAsync();
        _grid.Rows.Clear();
        foreach (var r in rows)
        {
            _grid.Rows.Add(r.ReceiptNo, r.Date.ToString("dd MMM yyyy"), $"{r.CustomerCode} - {r.CustomerName}",
                r.Amount, r.PaidBy, r.BillNo ?? "—", r.Remarks ?? string.Empty);
        }
        _grid.RightAlignNumericColumns();

        _grid.Visible = rows.Count > 0;
        _emptyState.Visible = rows.Count == 0;
    }

    private void ClearForm()
    {
        _amountBox.Value = 0;
        _remarksBox.Clear();
    }

    private async Task SaveAsync()
    {
        if (_customerCombo.SelectedValue is not int customerId)
        {
            Toast.Show(FindForm()!, "Select a customer.", ToastKind.Danger);
            return;
        }

        if (_amountBox.Value <= 0)
        {
            Toast.Show(FindForm()!, "Amount must be greater than zero.", ToastKind.Danger);
            return;
        }

        var model = new ReceiptInputModel(customerId, _dateBox.Value.Date, _amountBox.Value,
            (PaidBy)_paidByCombo.SelectedValue!, _remarksBox.Text);

        _saveButton.Enabled = false;
        try
        {
            var result = await _service.CreateAsync(model, _currentUser);
            Toast.Show(FindForm()!, $"Receipt {result.ReceiptNo} saved. New balance: Rs {result.NewBalance:N0}");
            ClearForm();
            await LoadCustomersAsync();
            await LoadReceiptsAsync();
        }
        catch (Exception ex)
        {
            Toast.Show(FindForm()!, ex.Message, ToastKind.Danger);
        }
        finally
        {
            _saveButton.Enabled = true;
        }
    }
}
