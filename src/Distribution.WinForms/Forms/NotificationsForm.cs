using System.Diagnostics;
using System.Net;
using System.Text;
using Distribution.Connect;
using Distribution.WinForms.Controls;
using Microsoft.Web.WebView2.WinForms;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Forms;

/// <summary>
/// Shows the messages sent by the distributor: text with an optional poster or video.
/// </summary>
public class NotificationsForm : Form
{
    private readonly BroadcastStateStore _state;
    private readonly IReadOnlyList<BroadcastMessage> _messages;

    private readonly FlowLayoutPanel _list;
    private readonly Panel _detailHost;
    private readonly EmptyState _emptyState;
    private readonly ThemedButton _markAllButton;
    private WebView2? _webView;
    private Panel? _fallbackPanel;
    private bool _webViewReady;

    private BroadcastListItem? _selectedItem;

    /// <summary>Raised after a message is read, so the bell can refresh its count.</summary>
    public event EventHandler? ReadStateChanged;

    public NotificationsForm(BroadcastStateStore state, IReadOnlyList<BroadcastMessage> messages)
    {
        _state = state;
        _messages = messages;

        Text = "Notifications";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 560);
        ClientSize = new Size(1040, 640);
        BackColor = Theme.Theme.Background;
        Font = Theme.Theme.BaseFont;
        ShowIcon = false;

        var header = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Theme.Theme.Surface };
        var headerBadge = new IconBadge
        {
            Glyph = Icons.Bell,
            Size = new Size(36, 36),
            Location = new Point(20, 14),
            BadgeColor = Theme.Theme.PastelBlue,
            IconColor = Theme.Theme.AccentBlue
        };
        var headerTitle = new Label
        {
            Text = "Notifications",
            Font = Theme.Theme.SectionTitleFont,
            ForeColor = Theme.Theme.HeadingNavy,
            AutoSize = true,
            UseMnemonic = false,
            Location = new Point(66, 22)
        };
        _markAllButton = new ThemedButton
        {
            Text = "Mark all as read",
            Width = 170,
            Variant = ThemedButtonVariant.Secondary,
            Location = new Point(ClientSize.Width - 194, 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _markAllButton.Click += (_, _) => MarkAllRead();
        header.Controls.Add(_markAllButton);
        header.Controls.Add(headerTitle);
        header.Controls.Add(headerBadge);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };

        _list = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 340,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Theme.Theme.Background,
            Padding = new Padding(0, 0, 8, 0)
        };
        _list.ClientSizeChanged += (_, _) => ResizeListItems();

        _detailHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Theme.Surface,
            Padding = new Padding(1),
            Margin = new Padding(16, 0, 0, 0)
        };

        _emptyState = new EmptyState
        {
            Dock = DockStyle.Fill,
            IconGlyph = Icons.Bell,
            Title = "No notifications",
            Subtitle = "Messages from your supplier will appear here."
        };

        body.Controls.Add(_detailHost);
        body.Controls.Add(_list);

        Controls.Add(body);
        Controls.Add(header);

        if (_messages.Count == 0)
        {
            _list.Visible = false;
            _detailHost.Controls.Add(_emptyState);
            _markAllButton.Enabled = false;
        }
        else
        {
            BuildList();
        }

        Load += async (_, _) =>
        {
            if (_messages.Count == 0) return;
            await SetUpViewerAsync();
            SelectItem((BroadcastListItem)_list.Controls[0]);
        };
    }

    private void BuildList()
    {
        _list.SuspendLayout();
        foreach (var message in _messages)
        {
            var item = new BroadcastListItem(message, !_state.IsRead(message.Id));
            item.Click += (s, _) => SelectItem((BroadcastListItem)s!);
            _list.Controls.Add(item);
        }
        _list.ResumeLayout(true);
        ResizeListItems();
    }

    private void ResizeListItems()
    {
        int width = Math.Max(120, _list.ClientSize.Width - _list.Padding.Horizontal);
        foreach (Control c in _list.Controls) c.Width = width;
    }

    private async Task SetUpViewerAsync()
    {
        try
        {
            var webView = new WebView2 { Dock = DockStyle.Fill };
            _detailHost.Controls.Add(webView);
            await webView.EnsureCoreWebView2Async();

            var core = webView.CoreWebView2;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.IsStatusBarEnabled = false;

            // Links must open in the customer's own browser, never inside this window.
            core.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                OpenExternal(e.Uri);
            };
            core.NavigationStarting += (_, e) =>
            {
                if (e.IsUserInitiated && !e.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    e.Cancel = true;
                    OpenExternal(e.Uri);
                }
            };

            _webView = webView;
            _webViewReady = true;
        }
        catch (Exception)
        {
            // WebView2 runtime missing or blocked: show the text, and offer to open media outside.
            _webViewReady = false;
            if (_webView is not null)
            {
                _detailHost.Controls.Remove(_webView);
                _webView.Dispose();
                _webView = null;
            }
        }
    }

    private void SelectItem(BroadcastListItem item)
    {
        if (_selectedItem is not null) _selectedItem.Selected = false;
        _selectedItem = item;
        item.Selected = true;

        if (item.Unread)
        {
            _state.MarkRead(item.Message.Id);
            item.Unread = false;
            ReadStateChanged?.Invoke(this, EventArgs.Empty);
        }

        Render(item.Message);
    }

    private void Render(BroadcastMessage message)
    {
        if (_webViewReady && _webView?.CoreWebView2 is not null)
        {
            _webView.CoreWebView2.NavigateToString(BuildHtml(message));
            return;
        }

        ShowFallback(message);
    }

    private void ShowFallback(BroadcastMessage message)
    {
        if (_fallbackPanel is not null)
        {
            _detailHost.Controls.Remove(_fallbackPanel);
            _fallbackPanel.Dispose();
        }

        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Theme.Surface, Padding = new Padding(24), AutoScroll = true };

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, BackColor = Theme.Theme.Surface };
        foreach (var (url, text, glyph) in new[]
                 {
                     (message.ImageUrl, "Open picture", Icons.Page),
                     (message.VideoUrl, "Play video", Icons.ViewAll),
                     (message.LinkUrl, message.LinkText ?? "Open link", Icons.Import)
                 })
        {
            if (string.IsNullOrWhiteSpace(url)) continue;
            var button = new ThemedButton { Text = text, Width = 160, Variant = ThemedButtonVariant.Secondary, Margin = new Padding(0, 0, 8, 0), IconGlyph = glyph };
            button.Click += (_, _) => OpenExternal(url!);
            buttons.Controls.Add(button);
        }

        var bodyLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = message.Body,
            Font = Theme.Theme.BaseFont,
            ForeColor = Theme.Theme.Text,
            UseMnemonic = false,
            AutoSize = false
        };
        var dateLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = message.PublishedAt.ToLocalTime().ToString("dddd, dd MMM yyyy, hh:mm tt"),
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted,
            UseMnemonic = false
        };
        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Text = message.Title,
            Font = new Font(Theme.Theme.FontFamily, 15f, FontStyle.Bold),
            ForeColor = Theme.Theme.HeadingNavy,
            UseMnemonic = false
        };

        panel.Controls.Add(bodyLabel);
        panel.Controls.Add(dateLabel);
        panel.Controls.Add(titleLabel);
        if (buttons.Controls.Count > 0) panel.Controls.Add(buttons);

        _fallbackPanel = panel;
        _detailHost.Controls.Add(panel);
        panel.BringToFront();
    }

    private static void OpenExternal(string url)
    {
        if (!IsSafeUrl(url)) return;
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // Nothing useful to tell the user if their browser refuses to start.
        }
    }

    /// <summary>Only plain web links are ever opened or embedded, whatever the feed contains.</summary>
    private static bool IsSafeUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    private static string BuildHtml(BroadcastMessage message)
    {
        var accent = message.Kind switch
        {
            BroadcastKind.Offer => "#17824F",
            BroadcastKind.Alert => "#B42318",
            _ => "#1C5FB0"
        };

        var sb = new StringBuilder();
        sb.Append("""
            <!doctype html><html><head><meta charset="utf-8">
            <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src https: http:; media-src https: http:; style-src 'unsafe-inline';">
            <style>
              body{font-family:'Segoe UI',Arial,sans-serif;color:#1C1F24;margin:0;padding:28px 32px;background:#fff;}
              .kind{display:inline-block;font-size:11px;font-weight:700;letter-spacing:.5px;text-transform:uppercase;
                    padding:3px 10px;border-radius:999px;color:#fff;margin-bottom:14px;}
              h1{font-size:22px;margin:0 0 6px;color:#16233D;}
              .date{font-size:12px;color:#5A5F66;margin-bottom:20px;}
              .body{font-size:15px;line-height:1.6;white-space:pre-wrap;margin-bottom:20px;}
              img,video{max-width:100%;border-radius:10px;border:1px solid #DEDDD6;display:block;margin-bottom:18px;}
              a.btn{display:inline-block;background:#0F5C63;color:#fff;text-decoration:none;
                    padding:10px 20px;border-radius:6px;font-size:14px;}
            </style></head><body>
            """);

        sb.Append($"<span class=\"kind\" style=\"background:{accent}\">{Escape(message.Kind.ToString())}</span>");
        sb.Append($"<h1>{Escape(message.Title)}</h1>");
        sb.Append($"<div class=\"date\">{Escape(message.PublishedAt.ToLocalTime().ToString("dddd, dd MMM yyyy, hh:mm tt"))}</div>");

        if (IsSafeUrl(message.ImageUrl))
        {
            sb.Append($"<img src=\"{Escape(message.ImageUrl!)}\" alt=\"\">");
        }

        if (IsSafeUrl(message.VideoUrl))
        {
            sb.Append($"<video src=\"{Escape(message.VideoUrl!)}\" controls preload=\"metadata\"></video>");
        }

        sb.Append($"<div class=\"body\">{Escape(message.Body)}</div>");

        if (IsSafeUrl(message.LinkUrl))
        {
            var text = string.IsNullOrWhiteSpace(message.LinkText) ? "Open link" : message.LinkText!;
            sb.Append($"<a class=\"btn\" href=\"{Escape(message.LinkUrl!)}\" target=\"_blank\">{Escape(text)}</a>");
        }

        sb.Append("</body></html>");
        return sb.ToString();
    }

    // The feed is written by the supplier, but it still arrives over the network, so nothing from it
    // is ever treated as markup.
    private static string Escape(string value) => WebUtility.HtmlEncode(value);

    private void MarkAllRead()
    {
        _state.MarkAllRead(_messages.Select(m => m.Id));
        foreach (Control c in _list.Controls)
        {
            if (c is BroadcastListItem item) item.Unread = false;
        }
        ReadStateChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _webView?.Dispose();
            _fallbackPanel?.Dispose();
        }
        base.Dispose(disposing);
    }
}
