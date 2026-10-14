namespace Distribution.WinForms.Controls;

public class SearchBox : IconFieldPanel
{
    private readonly TextBox _textBox;
    private readonly System.Windows.Forms.Timer _debounceTimer = new() { Interval = 300 };

    public event EventHandler? SearchTextChanged;

    public string Placeholder
    {
        get => _textBox.PlaceholderText;
        set => _textBox.PlaceholderText = value;
    }

    public new string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value;
    }

    public SearchBox()
    {
        Height = 36;
        Glyph = Theme.Icons.Search;

        _textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.Theme.BaseFont,
            BackColor = Theme.Theme.InputBackground,
            Dock = DockStyle.Fill
        };
        _textBox.GotFocus += (_, _) => SetFocused(true);
        _textBox.LostFocus += (_, _) => SetFocused(false);

        Controls.Add(_textBox);

        _debounceTimer.Tick += (_, _) =>
        {
            _debounceTimer.Stop();
            SearchTextChanged?.Invoke(this, EventArgs.Empty);
        };

        _textBox.TextChanged += (_, _) =>
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _debounceTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
