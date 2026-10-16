using Distribution.Connect;
using Distribution.Data;
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
    private readonly IUpdateService _updateService;
    private readonly IBroadcastFeedClient _broadcastClient;
    private readonly ILicenseService _licenseService;
    private readonly BroadcastStateStore _broadcastState;
    private readonly ConnectOptions _connectOptions;
    private readonly NotificationBell _bell;
    private readonly System.Windows.Forms.Timer _connectTimer = new();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly NotifyIcon _trayIcon = new();
    private IReadOnlyList<BroadcastMessage> _broadcasts = [];
    private bool _updatePromptShown;
    private bool _firstBroadcastLoad = true;
    private bool _lockHandled;
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
    private readonly HashSet<string> _refreshing = [];
    private string? _currentKey;

    public AuthResult? CurrentUser { get; set; }

    public MainForm(
        IServiceScopeFactory scopeFactory,
        IUpdateService updateService,
        IBroadcastFeedClient broadcastClient,
        ILicenseService licenseService,
        BroadcastStateStore broadcastState,
        ConnectOptions connectOptions)
    {
        _scopeFactory = scopeFactory;
        _updateService = updateService;
        _broadcastClient = broadcastClient;
        _licenseService = licenseService;
        _broadcastState = broadcastState;
        _connectOptions = connectOptions;
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

        _bell = new NotificationBell { Visible = false };
        _bell.Click += (_, _) => ShowNotifications();

        SetUpTrayIcon();

        _dateBlock = CreateIconText(Icons.Calendar, DateTime.Now.ToString("dddd, dd MMM yyyy"));
        _userBlock = CreateIconText(Icons.Contact, string.Empty);
        _userLabel = (Label)_userBlock.Controls[1];

        _topBar.Controls.Add(_bell);
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
                    new NavItem("sales.receipt", "Receive Payment", Icons.PaymentCard),
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

            StartConnectChecks();
        };

        FormClosed += (_, _) =>
        {
            _shutdown.Cancel();
            _connectTimer.Stop();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            foreach (var scope in _screenScopes.Values) scope.Dispose();
            _shutdown.Dispose();
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
        _bell.Location = new Point(_dateBlock.Left - _bell.Width - 16, (_topBar.Height - _bell.Height) / 2);

        // Stack title + subtitle from their measured heights and centre the block vertically;
        // the old fixed offsets let the subtitle cover the title's descenders ("Comnanies").
        int titleHeight = _screenTitleLabel.PreferredHeight;
        int subtitleHeight = string.IsNullOrEmpty(_screenSubtitleLabel.Text) ? 0 : _screenSubtitleLabel.PreferredHeight - 3;
        int top = (_topBar.Height - titleHeight - subtitleHeight) / 2;
        _screenBadge.Location = new Point(24, (_topBar.Height - _screenBadge.Height) / 2);
        _screenTitleLabel.Location = new Point(76, top);
        _screenSubtitleLabel.Location = new Point(78, top + titleHeight - 3);
    }

    // ---- Updates and broadcast messages ----

    private void StartConnectChecks()
    {
        _connectTimer.Interval = (int)_connectOptions.CheckInterval.TotalMilliseconds;
        _connectTimer.Tick += async (_, _) => await RunConnectChecksAsync(promptForUpdate: true);
        _connectTimer.Start();

        // Give the main window a moment to finish painting before touching the network.
        var startupDelay = new System.Windows.Forms.Timer { Interval = 4000 };
        startupDelay.Tick += async (s, _) =>
        {
            ((System.Windows.Forms.Timer)s!).Stop();
            ((System.Windows.Forms.Timer)s).Dispose();
            await RunConnectChecksAsync(promptForUpdate: true);
        };
        startupDelay.Start();
    }

    private async Task RunConnectChecksAsync(bool promptForUpdate)
    {
        await CheckLicenceAsync();
        if (_lockHandled) return;

        await RefreshBroadcastsAsync();
        if (promptForUpdate) await CheckForUpdateAsync(announceNoUpdate: false);
    }

    private async Task RefreshBroadcastsAsync()
    {
        if (!_connectOptions.BroadcastsEnabled) return;

        try
        {
            var messages = await _broadcastClient.GetMessagesAsync(_shutdown.Token);
            if (_shutdown.IsCancellationRequested || IsDisposed) return;

            _broadcasts = messages;
            _broadcastState.Prune(messages);
            UpdateBellCount();

            if (_firstBroadcastLoad)
            {
                // Opening the application for the first time should not throw a pile of old
                // messages at the user; the bell count already tells them there is something.
                _firstBroadcastLoad = false;
                _broadcastState.SuppressAnnouncementsFor(messages);
                return;
            }

            var fresh = _broadcastState.TakeUnannounced(messages);
            if (fresh.Count > 0) Announce(fresh);
        }
        catch (OperationCanceledException)
        {
            // The window is closing.
        }
        catch (Exception ex)
        {
            Serilog.Log.Debug(ex, "Could not refresh broadcast messages");
        }
    }

    private void UpdateBellCount()
    {
        _bell.Visible = _broadcasts.Count > 0;
        _bell.UnreadCount = _broadcastState.CountUnread(_broadcasts);
        PositionTopBarLabels();
    }

    private void ShowNotifications()
    {
        using var form = new NotificationsForm(_broadcastState, _broadcasts);
        form.ReadStateChanged += (_, _) => UpdateBellCount();
        form.ShowDialog(this);
        UpdateBellCount();
    }

    /// <summary>
    /// Looks for a newer version. <paramref name="announceNoUpdate"/> is set when the user asked,
    /// so that "you are up to date" is also reported.
    /// </summary>
    private async Task CheckForUpdateAsync(bool announceNoUpdate)
    {
        if (!_connectOptions.UpdatesEnabled) return;
        if (_updatePromptShown && !announceNoUpdate) return;

        UpdateCheckResult result;
        try
        {
            result = await _updateService.CheckAsync(_shutdown.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Serilog.Log.Debug(ex, "Update check failed");
            return;
        }

        if (_shutdown.IsCancellationRequested || IsDisposed) return;

        if (result.HasUpdate)
        {
            _updatePromptShown = true;
            using var form = new UpdateForm(_updateService, result);
            form.ShowDialog(this);
            return;
        }

        if (!announceNoUpdate) return;

        var message = result.Status switch
        {
            UpdateCheckStatus.UpToDate => $"You already have the latest version ({result.CurrentVersion}).",
            UpdateCheckStatus.NotInstalled =>
                "This copy was not installed by the setup program, so it cannot update itself.\n\n" +
                "Ask your supplier for the installer to get automatic updates.",
            UpdateCheckStatus.Disabled => "Updates are switched off for this computer.",
            UpdateCheckStatus.NotConfigured => "No update source is set up for this computer.",
            _ => $"The update check did not work: {result.Error}"
        };

        MessageBox.Show(this, message, "Software Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SetUpTrayIcon()
    {
        try
        {
            _trayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        }
        catch (Exception)
        {
            _trayIcon.Icon = SystemIcons.Application;
        }

        _trayIcon.Text = "Distribution Management System";
        _trayIcon.Visible = true;
        _trayIcon.BalloonTipClicked += (_, _) => BringUpNotifications();
        _trayIcon.Click += (_, _) => { if (_broadcasts.Count > 0) BringUpNotifications(); };
    }

    /// <summary>Tells the user about messages that arrived while they were working.</summary>
    private void Announce(IReadOnlyList<BroadcastMessage> fresh)
    {
        var newest = fresh[0];

        // The Windows notification area, so it is seen even when the window is behind something.
        try
        {
            _trayIcon.BalloonTipTitle = fresh.Count == 1
                ? newest.Title
                : $"{newest.Title}  (+{fresh.Count - 1} more)";
            _trayIcon.BalloonTipText = newest.Body.Replace('\n', ' ').Replace('\r', ' ').Trim();
            _trayIcon.BalloonTipIcon = newest.Kind switch
            {
                BroadcastKind.Alert => ToolTipIcon.Warning,
                _ => ToolTipIcon.Info
            };
            _trayIcon.ShowBalloonTip(10000);
        }
        catch (Exception ex)
        {
            Serilog.Log.Debug(ex, "Could not show the tray notification");
        }

        // And a card inside the application itself, which does not steal focus.
        try
        {
            NotificationPopup.Show(this, fresh, BringUpNotifications);
            Serilog.Log.Information("Announced {Count} new notification(s): {Title}", fresh.Count, newest.Title);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Could not show the notification card");
        }
    }

    private void BringUpNotifications()
    {
        if (IsDisposed) return;
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
        ShowNotifications();
    }

    /// <summary>
    /// Asks the distributor's licence list whether this installation may still be used, and locks
    /// the application if not.
    /// </summary>
    private async Task CheckLicenceAsync()
    {
        if (_lockHandled) return;

        LicenseState state;
        try
        {
            state = await _licenseService.CheckAsync(_shutdown.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            Serilog.Log.Debug(ex, "Licence check failed");
            return;
        }

        if (!state.IsLocked || _shutdown.IsCancellationRequested || IsDisposed) return;

        _lockHandled = true;
        ShowLockAndExit(state);
    }

    private void ShowLockAndExit(LicenseState state)
    {
        _connectTimer.Stop();
        using var form = new LockedForm(_licenseService, state, _connectOptions.SiteId);
        var result = form.ShowDialog(this);

        if (result == DialogResult.OK)
        {
            // The lock was lifted while the screen was open; carry on working.
            _lockHandled = false;
            _connectTimer.Start();
            return;
        }

        Close();
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
        ["sales.receipt"] = new ScreenInfo("Receive Payment", "Record a payment from a customer against their balance", Icons.PaymentCard),
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

        bool firstVisit = false;
        if (!_screenCache.TryGetValue(key, out var screen))
        {
            screen = BuildScreen(key);
            screen.Dock = DockStyle.Fill;
            screen.Visible = false;
            _contentArea.Controls.Add(screen);
            _screenCache[key] = screen;
            firstVisit = true;
        }

        screen.Visible = true;
        screen.BringToFront();
        _currentKey = key;

        // A new screen loads itself in its Load event; a cached one must reload what other screens changed meanwhile.
        if (!firstVisit && screen is IRefreshableScreen refreshable)
        {
            _ = RefreshScreenAsync(key, refreshable);
        }

        var info = ScreenInfos.GetValueOrDefault(key, new ScreenInfo("Coming soon", "", Icons.ViewAll));
        _screenTitleLabel.Text = info.Title;
        _screenBadge.Glyph = info.Icon;
        _screenSubtitleLabel.Text = info.Subtitle;
        _screenSubtitleLabel.Visible = !string.IsNullOrEmpty(info.Subtitle);
        PositionTopBarLabels();
    }

    private async Task RefreshScreenAsync(string key, IRefreshableScreen screen)
    {
        if (!_refreshing.Add(key)) return;
        try
        {
            // Each cached screen keeps one DbContext for the session; forget the rows it tracked earlier so the
            // reload reads current values instead of the copies it already has in memory.
            if (_screenScopes.TryGetValue(key, out var scope))
            {
                scope.ServiceProvider.GetRequiredService<DistributionDbContext>().ChangeTracker.Clear();
            }

            await screen.RefreshAsync();
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Refreshing screen {Screen} failed", key);
            Toast.Show(this, $"Could not refresh this screen: {ex.Message}", ToastKind.Danger);
        }
        finally
        {
            _refreshing.Remove(key);
        }
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
            "sales.receipt" => ActivatorUtilities.CreateInstance<ReceivePaymentControl>(sp, username),
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
        // Zero width or height (minimised window, or mid-layout) has nothing to paint, and a
        // gradient brush cannot be built over an empty rectangle.
        if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0) return;

        using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
            ClientRectangle, Theme.Theme.Surface, Theme.Theme.IconBadgeBackground,
            System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
        e.Graphics.FillRectangle(brush, ClientRectangle);

        var pen = Theme.Theme.GetPen(Theme.Theme.Border);
        e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
    }
}
