using Distribution.Domain.Common;
using Distribution.Domain.System;
using Distribution.Services.Masters;
using Distribution.Services.Sales;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Screens.Masters;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Screens.Sales;

public class PartyBillingControl : UserControl, IRefreshableScreen
{
    private readonly ISaleService _service;
    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;
    private readonly bool _isAdmin;
    private readonly string _currentUser;

    private readonly ComboBox _customerCombo;
    private readonly Label _prevBalanceLabel;
    private readonly DateTimePicker _dateBox;

    private readonly ComboBox _lineProductCombo;
    private readonly NumericStepperField _lineQtyBox;
    private readonly NumericStepperField _lineRateBox;
    private readonly NumericStepperField _lineDiscountPctBox;
    private readonly ThemedButton _addLineButton;
    private readonly ThemedGrid _linesGrid;

    private readonly NumericStepperField _billDiscountBox;
    private readonly NumericStepperField _cashReceivedBox;
    private readonly ComboBox _paidByCombo;
    private readonly Label _totalsLabel;
    private readonly ThemedButton _saveButton;
    private readonly ThemedButton _clearButton;

    private readonly ThemedGrid _historyGrid;
    private readonly EmptyState _historyEmptyState;

    private List<ProductListItem> _products = [];
    private readonly List<(int ProductId, string Code, string Name, decimal Qty, decimal Rate, decimal DiscountPct)> _lines = [];

    public PartyBillingControl(ISaleService service, ICustomerService customerService, IProductService productService, Distribution.Domain.Common.UserRole role, string currentUser)
    {
        _service = service;
        _customerService = customerService;
        _productService = productService;
        _isAdmin = role == Distribution.Domain.Common.UserRole.Admin;
        _currentUser = currentUser;
        BackColor = Theme.Theme.Background;
        AutoScroll = true;

        var headerCard = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, Title = "Party Billing", Subtitle = "Create a new sale bill", IconGlyph = Icons.Page };
        var headerRow = Shared.FieldRow();
        headerRow.Height = 70;

        _customerCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Label", ValueMember = "Id" };
        _dateBox = new DateTimePicker { Format = DateTimePickerFormat.Short };

        headerRow.Controls.Add(Shared.BadgeField("Customer", Icons.Contact2, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _customerCombo, "", 280));
        headerRow.Controls.Add(Shared.BadgeField("Date", Icons.Calendar, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _dateBox, "", 180));

        _prevBalanceLabel = new Label { Font = new Font(Theme.Theme.FontFamily, 10f, FontStyle.Bold), ForeColor = Theme.Theme.Warning, AutoSize = true, Location = new Point(500, 41), Text = "Previous Balance: Rs 0" };
        headerCard.ContentArea.Controls.Add(_prevBalanceLabel);
        headerCard.ContentArea.Controls.Add(headerRow);

        var lineCard = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, FillMinHeight = 150, Margin = new Padding(0, 16, 0, 0), Title = "Items", Subtitle = "Add each product to the bill", IconGlyph = Icons.Add };
        var lineRow = Shared.FieldRow();
        lineRow.Height = 70;

        _lineProductCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Label", ValueMember = "Id" };
        _lineQtyBox = new NumericStepperField { Maximum = 9_999_999, DecimalPlaces = 3, Increment = 1 };
        _lineRateBox = new NumericStepperField { Maximum = 9_999_999, DecimalPlaces = 2, Increment = 1 };
        _lineDiscountPctBox = new NumericStepperField { Maximum = 100, DecimalPlaces = 2, Increment = 1 };

        lineRow.Controls.Add(Shared.BadgeField("Product", Icons.Package, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _lineProductCombo, "", 260));
        lineRow.Controls.Add(Shared.BadgeStepperField("Qty", Icons.Package, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _lineQtyBox, 190));
        lineRow.Controls.Add(Shared.BadgeStepperField("Rate", Icons.Library, Theme.Theme.PastelOrange, Theme.Theme.AccentOrange, _lineRateBox, 210));
        lineRow.Controls.Add(Shared.BadgeStepperField("Discount %", Icons.Tag, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _lineDiscountPctBox, 190));

        var lineBtnRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Margin = new Padding(0, 4, 0, 4) };
        _addLineButton = new ThemedButton { Text = "Add Line", Width = 140, Variant = ThemedButtonVariant.Secondary, IconGlyph = Icons.Add };
        lineBtnRow.Controls.Add(_addLineButton);

        _linesGrid = new ThemedGrid { Dock = DockStyle.Fill, Height = 130 };
        _linesGrid.Columns.Add("Code", "Code");
        _linesGrid.Columns.Add("Product", "Product");
        foreach (var (name, header) in new[] { ("Qty", "Qty"), ("Rate", "Rate"), ("Disc", "Disc %"), ("Net", "Net Amount") })
        {
            _linesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = header, ValueType = typeof(decimal) });
        }

        lineCard.ContentArea.Controls.Add(_linesGrid);
        lineCard.ContentArea.Controls.Add(lineBtnRow);
        lineCard.ContentArea.Controls.Add(lineRow);

        var totalsCard = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, Margin = new Padding(0, 16, 0, 0), Title = "Totals & Payment", IconGlyph = Icons.PaymentCard };
        var totalsRow = Shared.FieldRow();
        totalsRow.Height = 70;

        _billDiscountBox = new NumericStepperField { Maximum = 9_999_999, DecimalPlaces = 2, Increment = 1 };
        _cashReceivedBox = new NumericStepperField { Maximum = 9_999_999, DecimalPlaces = 2, Increment = 1 };
        _paidByCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Label", ValueMember = "Value" };

        totalsRow.Controls.Add(Shared.BadgeStepperField("Bill Discount", Icons.Tag, Theme.Theme.PastelPurple, Theme.Theme.AccentPurple, _billDiscountBox, 200));
        totalsRow.Controls.Add(Shared.BadgeStepperField("Cash Received", Icons.PaymentCard, Theme.Theme.PastelGreen, Theme.Theme.AccentGreen, _cashReceivedBox, 200));
        totalsRow.Controls.Add(Shared.BadgeField("Received In", Icons.Library, Theme.Theme.PastelBlue, Theme.Theme.AccentBlue, _paidByCombo, "", 180));

        _totalsLabel = new Label { Dock = DockStyle.Top, Height = 28, Font = new Font(Theme.Theme.FontFamily, 11f, FontStyle.Bold), ForeColor = Theme.Theme.HeadingNavy, Text = "Net Bill: Rs 0   |   New Balance: Rs 0" };

        var saveRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Margin = new Padding(0, 8, 0, 0) };
        _saveButton = new ThemedButton { Text = "Save Bill (Ctrl+S)", Width = 180, IconGlyph = Icons.Save };
        _clearButton = new ThemedButton { Text = "Clear (Esc)", Width = 120, Variant = ThemedButtonVariant.Secondary, Margin = new Padding(8, 0, 0, 0), IconGlyph = Icons.Refresh };
        saveRow.Controls.Add(_saveButton);
        saveRow.Controls.Add(_clearButton);

        totalsCard.ContentArea.Controls.Add(saveRow);
        totalsCard.ContentArea.Controls.Add(_totalsLabel);
        totalsCard.ContentArea.Controls.Add(totalsRow);

        // Top + AutoHeight (not Fill) so the page scrolls instead of squeezing this card to its header.
        var historyCard = new CardPanel { Dock = DockStyle.Top, AutoHeight = true, FillMinHeight = 220, Title = "Today's Bills", IconGlyph = Icons.ReportDocument, Margin = new Padding(0, 16, 0, 0) };
        _historyGrid = new ThemedGrid { Dock = DockStyle.Fill, Visible = false };
        _historyGrid.Columns.Add("BillNo", "Bill No");
        _historyGrid.Columns.Add("Customer", "Customer");
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Net", HeaderText = "Net Amount", ValueType = typeof(decimal) });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cash", HeaderText = "Cash Received", ValueType = typeof(decimal) });

        _historyEmptyState = new EmptyState { Dock = DockStyle.Fill, IconGlyph = Icons.Page, Title = "No bills yet today", Subtitle = "Create a new bill using the form above." };
        historyCard.ContentArea.Controls.Add(_historyGrid);
        historyCard.ContentArea.Controls.Add(_historyEmptyState);

        Controls.Add(historyCard);
        Controls.Add(totalsCard);
        Controls.Add(lineCard);
        Controls.Add(headerCard);

        _customerCombo.SelectedIndexChanged += (_, _) => UpdatePreviousBalance();
        _lineProductCombo.SelectedIndexChanged += (_, _) => AutoFillRate();
        _addLineButton.Click += AddLine;
        _billDiscountBox.ValueChanged += (_, _) => RefreshTotals();
        _cashReceivedBox.ValueChanged += (_, _) => RefreshTotals();
        _saveButton.Click += async (_, _) => await SaveBillAsync();
        _clearButton.Click += (_, _) => ClearAll();

        Load += async (_, _) =>
        {
            _paidByCombo.DataSource = Enum.GetValues<PaidBy>().Select(v => new { Value = v, Label = v.ToString() }).ToList();
            await LoadCustomersAsync();
            await LoadProductsAsync();
            await LoadHistoryAsync();
        };
    }

    public async Task RefreshAsync()
    {
        await LoadCustomersAsync();
        await LoadProductsAsync();
        await LoadHistoryAsync();
    }

    private async Task LoadCustomersAsync()
    {
        var customers = await _customerService.SearchAsync(null);
        Shared.Rebind(_customerCombo, customers.Select(c => new { c.Id, Label = $"{c.Code} - {c.ShopName}", c.Balance }).ToList());
        UpdatePreviousBalance();
    }

    private void UpdatePreviousBalance()
    {
        if (_customerCombo.SelectedItem is null) return;
        dynamic item = _customerCombo.SelectedItem;
        decimal balance = item.Balance;
        _prevBalanceLabel.Text = $"Previous Balance: Rs {balance:N0}";
    }

    private async Task LoadProductsAsync()
    {
        _products = await _productService.SearchAsync(null, null, null);
        // Keep the chosen product across a refresh, but do not re-trigger AutoFillRate over a rate the user edited.
        var keepRate = _lineRateBox.Value;
        Shared.Rebind(_lineProductCombo, _products.Select(p => new { p.Id, Label = $"{p.Code} - {p.Name}" }).ToList());
        if (keepRate > 0) _lineRateBox.Value = keepRate;
    }

    private void AutoFillRate()
    {
        if (_lineProductCombo.SelectedValue is not int productId) return;
        var product = _products.FirstOrDefault(p => p.Id == productId);
        if (product is not null) _lineRateBox.Value = product.SaleRate;
    }

    private void AddLine(object? sender, EventArgs e)
    {
        if (_lineProductCombo.SelectedValue is not int productId)
        {
            Toast.Show(FindForm()!, "Select a product.", ToastKind.Danger);
            return;
        }
        if (_lineQtyBox.Value <= 0 || _lineRateBox.Value <= 0)
        {
            Toast.Show(FindForm()!, "Qty and Rate must be greater than zero.", ToastKind.Danger);
            return;
        }

        var product = _products.First(p => p.Id == productId);
        _lines.Add((productId, product.Code, product.Name, _lineQtyBox.Value, _lineRateBox.Value, _lineDiscountPctBox.Value));
        RefreshLinesGrid();

        _lineQtyBox.Value = 0;
        _lineDiscountPctBox.Value = 0;
    }

    private void RefreshLinesGrid()
    {
        _linesGrid.Rows.Clear();
        foreach (var l in _lines)
        {
            var amount = l.Qty * l.Rate;
            var discountAmt = Math.Round(amount * l.DiscountPct / 100, 0, MidpointRounding.AwayFromZero);
            var net = amount - discountAmt;
            _linesGrid.Rows.Add(l.Code, l.Name, l.Qty, l.Rate, l.DiscountPct, net);
        }
        _linesGrid.RightAlignNumericColumns();
        RefreshTotals();
    }

    private void RefreshTotals()
    {
        decimal gross = 0, itemDiscount = 0;
        foreach (var l in _lines)
        {
            var amount = l.Qty * l.Rate;
            var discountAmt = Math.Round(amount * l.DiscountPct / 100, 0, MidpointRounding.AwayFromZero);
            gross += amount;
            itemDiscount += discountAmt;
        }

        var netBill = gross - itemDiscount - _billDiscountBox.Value;
        decimal prevBalance = 0;
        if (_customerCombo.SelectedItem is not null)
        {
            dynamic item = _customerCombo.SelectedItem;
            prevBalance = item.Balance;
        }
        var newBalance = prevBalance + netBill - _cashReceivedBox.Value;

        _totalsLabel.Text = $"Net Bill: Rs {netBill:N0}   |   New Balance: Rs {newBalance:N0}";
    }

    private async Task LoadHistoryAsync()
    {
        var rows = await _service.GetTodaysSalesAsync();
        _historyGrid.Rows.Clear();
        foreach (var r in rows) _historyGrid.Rows.Add(r.BillNo, r.CustomerName, r.NetAmount, r.CashReceived);
        _historyGrid.RightAlignNumericColumns();
        _historyGrid.Visible = rows.Count > 0;
        _historyEmptyState.Visible = rows.Count == 0;
    }

    private void ClearAll()
    {
        _lines.Clear();
        _billDiscountBox.Value = 0;
        _cashReceivedBox.Value = 0;
        RefreshLinesGrid();
    }

    private async Task SaveBillAsync()
    {
        if (_customerCombo.SelectedValue is not int customerId)
        {
            Toast.Show(FindForm()!, "Select a customer.", ToastKind.Danger);
            return;
        }
        if (_lines.Count == 0)
        {
            Toast.Show(FindForm()!, "Add at least one item.", ToastKind.Danger);
            return;
        }

        var model = new SaleInputModel(
            customerId, _dateBox.Value.Date,
            _lines.Select(l => new SaleLineInput(l.ProductId, l.Qty, l.Rate, l.DiscountPct)).ToList(),
            _billDiscountBox.Value, _cashReceivedBox.Value,
            _paidByCombo.SelectedValue is PaidBy paidBy ? paidBy : PaidBy.Cash);

        try
        {
            var result = await _service.CreateAsync(model, _isAdmin, _currentUser);
            var credit = result.NetAmount - model.CashReceived;
            Toast.Show(FindForm()!, credit > 0
                ? $"Bill {result.BillNo} saved. Rs {credit:N0} on credit — new balance Rs {result.NewBalance:N0}."
                : $"Bill {result.BillNo} saved. Fully paid.");
            ClearAll();
            await LoadCustomersAsync();
            await LoadHistoryAsync();
        }
        catch (Exception ex)
        {
            Toast.Show(FindForm()!, ex.Message, ToastKind.Danger);
        }
    }
}
