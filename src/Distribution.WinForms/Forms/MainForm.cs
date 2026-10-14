using Distribution.Services.Auth;
using Distribution.WinForms.Controls;
using Distribution.WinForms.Screens;
using Distribution.WinForms.Screens.Expenses;
using Distribution.WinForms.Screens.Masters;
using Distribution.WinForms.Screens.Reports;
using Distribution.WinForms.Screens.Sales;
using Distribution.WinForms.Screens.Stock;
using Distribution.WinForms.Screens.Admin;
using Microsoft.Extensions.DependencyInjection;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Forms;

public class MainForm : Form
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SideNav _sideNav;
    private readonly Panel _contentArea;
    private readonly Panel _userBlock;
    private readonly Panel _dateBlock;
    private readonly Label _userLabel;
    private readonly IconBadge _screenBadge;
    private readonly Label _screenTitleLabel;
    private readonly Label _screenSubtitleLabel;
    private readonly TopBarPanel _topBar;

    private readonly Dictionary<string, IServiceScope> _screenScopes = new();
    private readonly Dictionary<string, Control> _screenCache = new();
    private string? _currentKey;

    public AuthResult? CurrentUser { get; set; }

    public MainForm(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        Text = "Distribution Management System";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1366, 768);
        WindowState = FormWindowState.Maximized;
        BackColor = Theme.Theme.Background;
        Font = Theme.Theme.BaseFont;

        _sideNav = new SideNav();
        _sideNav.ItemSelected += (_, key) => NavigateTo(key);
        _topBar = new TopBarPanel { Dock = DockStyle.Top, Height = 72 };

        _screenBadge = new IconBadge { Glyph = Icons.ViewAll, Size = new Size(40, 40), Location = new Point(24, 16) };
        _screenTitleLabel = new Label
        {
            Text = "Dashboard",
            Font = Theme.Theme.HeadingFont,
            ForeColor = Theme.Theme.HeadingNavy,
            AutoSize = true,
            UseMnemonic = false,
            BackColor = Color.Transparent,
            Location = new Point(76, 14)
        };
        _screenSubtitleLabel = new Label
        {
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted,
            AutoSize = true,
            UseMnemonic = false,
            BackColor = Color.Transparent,
            Location = new Point(76, 42),
            Visible = false
        };

        _dateBlock = CreateIconText(Icons.Calendar, DateTime.Now.ToString("dddd, dd MMM yyyy"));
        _userBlock = CreateIconText(Icons.Contact, string.Empty);
        _userLabel = (Label)_userBlock.Controls[1];

        _topBar.Controls.Add(_dateBlock);
        _topBar.Controls.Add(_userBlock);
        _topBar.Controls.Add(_screenSubtitleLabel);
        _topBar.Controls.Add(_screenTitleLabel);
        _topBar.Controls.Add(_screenBadge);
        _topBar.Resize += (_, _) => PositionTopBarLabels();

        _contentArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Theme.Background,
            Padding = new Padding(24)
        };

        Controls.Add(_contentArea);
        Controls.Add(_topBar);
        Controls.Add(_sideNav);

        Load += (_, _) =>
        {
            SuspendLayout();
            _userLabel.Text = CurrentUser is not null ? $"{CurrentUser.FullName} ({CurrentUser.Role})" : string.Empty;

            var isAdmin = CurrentUser?.Role == Distribution.Domain.Common.UserRole.Admin;
            List<NavItem> systemItems = isAdmin
                ? [new NavItem("system.users", "Users", Icons.Contact), new NavItem("settings", "Settings", Icons.Admin)]
                : [new NavItem("settings", "Settings", Icons.Admin)];

            _sideNav.SetGroups(
            [
                new NavGroup("Overview", [new NavItem("dashboard", "Dashboard", Icons.Home)]),
                new NavGroup("Masters",
                [
                    new NavItem("masters.companies", "Companies", Icons.Shop),
                    new NavItem("masters.categories", "Categories", Icons.Tag),
                    new NavItem("masters.products", "Products", Icons.Package),
                    new NavItem("masters.customers", "Customers", Icons.Contact),
                    new NavItem("masters.suppliers", "Suppliers", Icons.Car)
                ]),
                new NavGroup("Stock",
                [
                    new NavItem("stock.opening", "Opening Stock", Icons.Scan),
                    new NavItem("stock.received", "Stock Received", Icons.Page),
                    new NavItem("stock.damage", "Damage Entry", Icons.Admin)
                ]),
                new NavGroup("Sales",
                [
                    new NavItem("sales.billing", "Party Billing", Icons.Page),
                    new NavItem("sales.ledger", "Party Ledger", Icons.ReportDocument),
                    new NavItem("sales.credit", "Market Credit", Icons.PaymentCard)
                ]),
                new NavGroup("Expenses",
                [
                    new NavItem("expenses.entry", "Expense Entry", Icons.Page),
                    new NavItem("expenses.report", "Expense Report", Icons.ReportDocument)
                ]),
                new NavGroup("Reports",
                [
                    new NavItem("reports.closingstock", "Daily Closing Stock", Icons.ReportDocument),
                    new NavItem("reports.dsr", "Daily Sale Report", Icons.ReportDocument),
                    new NavItem("reports.pl", "Profit & Loss", Icons.Up),
                    new NavItem("reports.damage", "Damage Report", Icons.Admin)
                ]),
                new NavGroup("System", systemItems)
            ]);

            PositionTopBarLabels();
            _sideNav.SetActive("dashboard", raiseEvent: false);
            NavigateTo("dashboard");
            ResumeLayout(true);
        };

        FormClosed += (_, _) =>
        {
            foreach (var scope in _screenScopes.Values) scope.Dispose();
        };
    }

    private static Panel CreateIconText(string glyph, string text)
    {
        // GrowAndShrink: the default GrowOnly keeps the panel at its initial 100px height, which threw
        // off vertical centring in the 72px top bar.
        var panel = new Panel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Color.Transparent };
        var iconLabel = new Label
        {
            Text = glyph,
            Font = Icons.GetFont(10f),
            ForeColor = Theme.Theme.Muted,
            AutoSize = true,
            Location = new Point(0, 2)
        };
        var textLabel = new Label
        {
            Text = text,
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted,
            AutoSize = true,
            Location = new Point(20, 2)
        };
        panel.Controls.Add(iconLabel);
        panel.Controls.Add(textLabel);
        return panel;
    }

    private void PositionTopBarLabels()
    {
        _userBlock.Location = new Point(_topBar.Width - _userBlock.Width - 24, (_topBar.Height - _userBlock.Height) / 2);
        _dateBlock.Location = new Point(_userBlock.Left - _dateBlock.Width - 32, (_topBar.Height - _dateBlock.Height) / 2);

        // Stack title + subtitle from their measured heights and centre the block vertically;
        // the old fixed offsets let the subtitle cover the title's descenders ("Comnanies").
        int titleHeight = _screenTitleLabel.PreferredHeight;
        int subtitleHeight = string.IsNullOrEmpty(_screenSubtitleLabel.Text) ? 0 : _screenSubtitleLabel.PreferredHeight - 3;
        int top = (_topBar.Height - titleHeight - subtitleHeight) / 2;
        _screenBadge.Location = new Point(24, (_topBar.Height - _screenBadge.Height) / 2);
        _screenTitleLabel.Location = new Point(76, top);
        _screenSubtitleLabel.Location = new Point(78, top + titleHeight - 3);
    }

    private record ScreenInfo(string Title, string Subtitle, string Icon);

    private static readonly Dictionary<string, ScreenInfo> ScreenInfos = new()
    {
        ["dashboard"] = new ScreenInfo("Dashboard", "", Icons.ViewAll),
        ["masters.companies"] = new ScreenInfo("Companies", "Manage your company records easily", Icons.Shop),
        ["masters.categories"] = new ScreenInfo("Categories", "Manage product categories for your inventory", Icons.Tag),
        ["masters.products"] = new ScreenInfo("Products", "Add, update and manage your products", Icons.Package),
        ["masters.customers"] = new ScreenInfo("Customers", "Manage your customer accounts", Icons.Contact),
        ["masters.suppliers"] = new ScreenInfo("Suppliers", "Manage your supplier records easily", Icons.Car),
        ["stock.opening"] = new ScreenInfo("Opening Stock", "One-time setup of starting stock", Icons.Scan),
        ["stock.received"] = new ScreenInfo("Stock Received", "Receive stock from your suppliers", Icons.Package),
        ["stock.damage"] = new ScreenInfo("Damage Entry", "Record damaged or lost stock", Icons.Admin),
        ["sales.billing"] = new ScreenInfo("Party Billing", "Create a new sale bill", Icons.Page),
        ["sales.ledger"] = new ScreenInfo("Party Ledger", "View a customer's transaction history", Icons.ReportDocument),
        ["sales.credit"] = new ScreenInfo("Market Credit", "Customer-wise outstanding balances", Icons.PaymentCard),
        ["expenses.entry"] = new ScreenInfo("Expense Entry", "Record a business or personal expense", Icons.PaymentCard),
        ["expenses.report"] = new ScreenInfo("Expense Report", "Category-wise expense breakdown", Icons.ReportDocument),
        ["reports.closingstock"] = new ScreenInfo("Daily Closing Stock", "Opening, received, sold and closing per product", Icons.Package),
        ["reports.dsr"] = new ScreenInfo("Daily Sale Report", "Bill-wise, product-wise and company-wise sales", Icons.ReportDocument),
        ["reports.pl"] = new ScreenInfo("Profit & Loss", "Net sale, cost, expenses and net profit", Icons.Up),
        ["reports.damage"] = new ScreenInfo("Damage Report", "Grouped loss summary for any date range", Icons.Admin),
        ["system.users"] = new ScreenInfo("Users", "Manage staff logins and roles", Icons.Contact),
        ["settings"] = new ScreenInfo("Settings", "Company info, invoicing and stock preferences", Icons.Admin)
    };

    private void NavigateTo(string key)
    {
        if (_currentKey == key) return;

        if (_currentKey is not null && _screenCache.TryGetValue(_currentKey, out var previous))
        {
            previous.Visible = false;
        }

        if (!_screenCache.TryGetValue(key, out var screen))
        {
            screen = BuildScreen(key);
            screen.Dock = DockStyle.Fill;
            screen.Visible = false;
            _contentArea.Controls.Add(screen);
            _screenCache[key] = screen;
        }

        screen.Visible = true;
        screen.BringToFront();
        _currentKey = key;

        var info = ScreenInfos.GetValueOrDefault(key, new ScreenInfo("Coming soon", "", Icons.ViewAll));
        _screenTitleLabel.Text = info.Title;
        _screenBadge.Glyph = info.Icon;
        _screenSubtitleLabel.Text = info.Subtitle;
        _screenSubtitleLabel.Visible = !string.IsNullOrEmpty(info.Subtitle);
        PositionTopBarLabels();
    }

    private Control BuildScreen(string key)
    {
        if (key == "dashboard")
        {
            var dashboard = new DashboardControl();
            dashboard.NavigateRequested += (_, targetKey) =>
            {
                _sideNav.SetActive(targetKey, raiseEvent: false);
                NavigateTo(targetKey);
            };
            return dashboard;
        }

        var username = CurrentUser?.Username ?? string.Empty;
        var scope = _scopeFactory.CreateScope();
        _screenScopes[key] = scope;
        var sp = scope.ServiceProvider;

        return key switch
        {
            "masters.companies" => ActivatorUtilities.CreateInstance<CompaniesControl>(sp, username),
            "masters.categories" => ActivatorUtilities.CreateInstance<CategoriesControl>(sp, username),
            "masters.products" => ActivatorUtilities.CreateInstance<ProductsControl>(sp, username),
            "masters.customers" => ActivatorUtilities.CreateInstance<CustomersControl>(sp, username),
            "masters.suppliers" => ActivatorUtilities.CreateInstance<SuppliersControl>(sp, username),
            "stock.opening" => ActivatorUtilities.CreateInstance<OpeningStockControl>(sp, username),
            "stock.received" => ActivatorUtilities.CreateInstance<StockReceivedControl>(sp, username),
            "stock.damage" => ActivatorUtilities.CreateInstance<DamageEntryControl>(sp, username),
            "sales.billing" => ActivatorUtilities.CreateInstance<PartyBillingControl>(sp, CurrentUser!.Role, username),
            "sales.ledger" => ActivatorUtilities.CreateInstance<PartyLedgerControl>(sp),
            "sales.credit" => ActivatorUtilities.CreateInstance<MarketCreditControl>(sp),
            "expenses.entry" => ActivatorUtilities.CreateInstance<ExpenseEntryControl>(sp, username),
            "expenses.report" => ActivatorUtilities.CreateInstance<ExpenseReportControl>(sp),
            "reports.closingstock" => ActivatorUtilities.CreateInstance<DailyClosingStockControl>(sp),
            "reports.dsr" => ActivatorUtilities.CreateInstance<DailySaleReportControl>(sp),
            "reports.pl" => ActivatorUtilities.CreateInstance<ProfitLossControl>(sp),
            "reports.damage" => ActivatorUtilities.CreateInstance<DamageReportControl>(sp),
            "system.users" => ActivatorUtilities.CreateInstance<UsersControl>(sp, username),
            "settings" => ActivatorUtilities.CreateInstance<SettingsControl>(sp, username),
            _ => new ComingSoonControl(key)
        };
    }
}

internal class TopBarPanel : Panel
{
    public TopBarPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Theme.Surface;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
            ClientRectangle, Theme.Theme.Surface, Theme.Theme.IconBadgeBackground,
            System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
        e.Graphics.FillRectangle(brush, ClientRectangle);

        var pen = Theme.Theme.GetPen(Theme.Theme.Border);
        e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
    }
}
