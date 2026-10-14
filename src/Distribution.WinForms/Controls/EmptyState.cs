namespace Distribution.WinForms.Controls;

public class EmptyState : Panel
{
    private readonly Label _iconLabel;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Panel _buttonHost;

    public string IconGlyph
    {
        get => _iconLabel.Text;
        set => _iconLabel.Text = value;
    }

    public string Title
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value;
    }

    public string Subtitle
    {
        get => _subtitleLabel.Text;
        set => _subtitleLabel.Text = value;
    }

    public EmptyState()
    {
        BackColor = Color.Transparent;

        _iconLabel = new Label
        {
            Font = new Font(Theme.Icons.FontFamily, 42f),
            ForeColor = Theme.Theme.Border,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _titleLabel = new Label
        {
            Font = new Font(Theme.Theme.FontFamily, 13f, FontStyle.Bold),
            ForeColor = Theme.Theme.HeadingNavy,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _subtitleLabel = new Label
        {
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _buttonHost = new Panel { AutoSize = true, BackColor = Color.Transparent };

        Controls.Add(_buttonHost);
        Controls.Add(_subtitleLabel);
        Controls.Add(_titleLabel);
        Controls.Add(_iconLabel);

        Resize += (_, _) => Reflow();
        _iconLabel.TextChanged += (_, _) => Reflow();
        _titleLabel.TextChanged += (_, _) => Reflow();
        _subtitleLabel.TextChanged += (_, _) => Reflow();
    }

    public void SetAction(Control actionButton)
    {
        _buttonHost.Controls.Clear();
        _buttonHost.Controls.Add(actionButton);
        actionButton.Location = Point.Empty;
        _buttonHost.Size = actionButton.Size;
        Reflow();
    }

    private void Reflow()
    {
        // Centre the whole block and never start above the top edge — a fixed "Height/2 - 100"
        // cut the icon off in short cards (e.g. Party Billing's history card).
        int buttonHeight = _buttonHost.Controls.Count > 0 ? 16 + _buttonHost.Height : 0;
        int blockHeight = _iconLabel.Height + 8 + _titleLabel.Height + 6 + _subtitleLabel.Height + buttonHeight;
        int y = Math.Max(0, (Height - blockHeight) / 2);
        CenterAt(_iconLabel, ref y, 8);
        CenterAt(_titleLabel, ref y, 6);
        CenterAt(_subtitleLabel, ref y, 16);
        CenterAt(_buttonHost, ref y, 0);
    }

    private void CenterAt(Control c, ref int y, int gapAfter)
    {
        c.Location = new Point((Width - c.Width) / 2, y);
        y += c.Height + gapAfter;
    }
}
