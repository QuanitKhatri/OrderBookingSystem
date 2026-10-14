namespace Distribution.WinForms.Controls;

/// <summary>
/// Hosts a DateTimePicker inside the standard rounded input pill. The picker always paints its own
/// white box and border (it ignores BackColor under visual styles), so it sits inside a clip panel
/// offset by its border width — only the date text and calendar button remain visible.
/// </summary>
public sealed class DatePickerField : IconFieldPanel
{
    private const int PickerBorder = 2;
    private readonly Panel _clip;
    private readonly DateTimePicker _picker;

    public DatePickerField(DateTimePicker picker)
    {
        _picker = picker;
        Height = 36;
        FillColor = SystemColors.Window;
        BackColor = SystemColors.Window;
        // The picker already has its own inner text margin, so the pill needs less than the default 10px.
        Padding = new Padding(4, 0, 2, 0);

        picker.Dock = DockStyle.None;
        picker.Font = Theme.Theme.BaseFont;
        picker.GotFocus += (_, _) => SetFocused(true);
        picker.LostFocus += (_, _) => SetFocused(false);

        _clip = new Panel { BackColor = SystemColors.Window };
        _clip.Controls.Add(picker);
        Controls.Add(_clip);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        // The base constructor sets Padding, which raises Layout before our fields are assigned.
        if (_picker is null || _clip is null) return;
        int innerHeight = _picker.Height - PickerBorder * 2;
        int innerWidth = Math.Max(10, ClientSize.Width - Padding.Horizontal - 2);
        _clip.SetBounds(Padding.Left, (ClientSize.Height - innerHeight) / 2, innerWidth, innerHeight);
        _picker.SetBounds(-PickerBorder, -PickerBorder, innerWidth + PickerBorder * 2, _picker.Height);
    }
}
