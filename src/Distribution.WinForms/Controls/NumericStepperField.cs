namespace Distribution.WinForms.Controls;

public class NumericStepperField : IconFieldPanel
{
    private readonly TextBox _textBox;
    private readonly Label _minusButton;
    private readonly Label _plusButton;
    private decimal _value;

    public decimal Minimum { get; set; } = 0m;
    public decimal Maximum { get; set; } = 9_999_999m;
    public int DecimalPlaces { get; set; } = 2;
    public decimal Increment { get; set; } = 1m;

    public event EventHandler? ValueChanged;

    public decimal Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, Minimum, Maximum);
            _value = clamped;
            _textBox.Text = clamped.ToString("N" + DecimalPlaces);
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public NumericStepperField()
    {
        Height = 36;

        _minusButton = CreateStepButton("−");
        _plusButton = CreateStepButton("+");

        _textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.Theme.BaseFont,
            BackColor = Theme.Theme.InputBackground,
            TextAlign = HorizontalAlignment.Left,
            Dock = DockStyle.Fill
        };
        _textBox.GotFocus += (_, _) => SetFocused(true);
        _textBox.LostFocus += (_, _) => { SetFocused(false); CommitText(); };
        _textBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { CommitText(); e.SuppressKeyPress = true; }
        };

        _minusButton.Click += (_, _) => { CommitText(); Value -= Increment; };
        _plusButton.Click += (_, _) => { CommitText(); Value += Increment; };

        Controls.Add(_textBox);
        Controls.Add(_minusButton);
        Controls.Add(_plusButton);

        Value = 0;
    }

    private Label CreateStepButton(string symbol)
    {
        var btn = new Label
        {
            Text = symbol,
            Width = 24,
            Dock = symbol == "+" ? DockStyle.Right : DockStyle.Left,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Theme.Theme.FontFamily, 10f, FontStyle.Bold),
            ForeColor = Theme.Theme.Muted,
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };
        btn.MouseEnter += (_, _) => btn.ForeColor = Theme.Theme.Primary;
        btn.MouseLeave += (_, _) => btn.ForeColor = Theme.Theme.Muted;
        return btn;
    }

    private void CommitText()
    {
        if (decimal.TryParse(_textBox.Text, out var parsed))
        {
            Value = parsed;
        }
        else
        {
            _textBox.Text = _value.ToString("N" + DecimalPlaces);
        }
    }
}
