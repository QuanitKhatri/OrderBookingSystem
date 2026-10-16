namespace Distribution.WinForms.Screens.Masters;

internal static class Shared
{
    public static Control LabeledField(string label, Control input, int width = 220)
    {
        var panel = new Panel { Width = width, Height = 52, Margin = new Padding(0, 0, 16, 8) };
        var lbl = new Label
        {
            Text = label,
            Dock = DockStyle.Top,
            Height = 20,
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted
        };
        input.Dock = DockStyle.Top;
        input.Width = width;
        panel.Controls.Add(input);
        panel.Controls.Add(lbl);
        return panel;
    }

    /// <summary>
    /// Binds fresh items to a lookup drop-down, keeping the current choice when it is still in the list
    /// (otherwise the first item is selected). Pass <paramref name="select"/> to choose a specific value instead.
    /// </summary>
    public static void Rebind(ComboBox combo, object items, object? select = null)
    {
        var wanted = select ?? combo.SelectedValue;
        combo.DataSource = items;
        if (wanted is null) return;

        combo.SelectedValue = wanted;
        if (combo.SelectedIndex < 0 && combo.Items.Count > 0) combo.SelectedIndex = 0;
    }

    /// <summary>Vertical gap between Dock=Top siblings (Dock layout ignores Margin).</summary>
    public static Panel Spacer(int height = 10) => new() { Dock = DockStyle.Top, Height = height };

    public static FlowLayoutPanel FieldRow() => new()
    {
        Dock = DockStyle.Top,
        Height = 60,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false,
        AutoSize = false
    };

    public static Control IconField(string label, string? glyph, Control input, int width = 220, int height = 36)
    {
        var outer = new Panel { Width = width, Height = 58, Margin = new Padding(0, 0, 16, 10) };
        var lbl = new Label
        {
            Text = label,
            Dock = DockStyle.Top,
            Height = 20,
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted
        };

        var fieldPanel = new Controls.IconFieldPanel { Dock = DockStyle.Top, Height = height, Glyph = glyph ?? string.Empty };

        switch (input)
        {
            case TextBox tb:
                tb.BorderStyle = BorderStyle.None;
                tb.BackColor = Theme.Theme.InputBackground;
                tb.Font = Theme.Theme.BaseFont;
                tb.Dock = DockStyle.Fill;
                tb.GotFocus += (_, _) => fieldPanel.SetFocused(true);
                tb.LostFocus += (_, _) => fieldPanel.SetFocused(false);
                break;
            case ComboBox cb:
                cb.FlatStyle = FlatStyle.Flat;
                cb.BackColor = Theme.Theme.InputBackground;
                cb.Font = Theme.Theme.BaseFont;
                cb.Dock = DockStyle.Fill;
                cb.GotFocus += (_, _) => fieldPanel.SetFocused(true);
                cb.LostFocus += (_, _) => fieldPanel.SetFocused(false);
                break;
        }

        fieldPanel.Controls.Add(input);

        outer.Controls.Add(fieldPanel);
        outer.Controls.Add(lbl);
        return outer;
    }

    public static Control BadgeField(string label, string glyph, Color badgeColor, Color accentColor, Control input, string placeholder, int width = 220, string? plainText = null)
    {
        const int badgeSize = 40;
        const int gap = 12;

        var outer = new Panel { Width = width, Height = 58, Margin = new Padding(0, 0, 20, 12) };

        var badge = new Controls.IconBadge
        {
            Glyph = glyph,
            PlainText = plainText,
            BadgeColor = badgeColor,
            IconColor = accentColor,
            Size = new Size(badgeSize, badgeSize),
            Location = new Point(0, 9)
        };

        var stack = new Panel { Location = new Point(badgeSize + gap, 0), Width = width - badgeSize - gap, Height = 58 };
        var lbl = new Label
        {
            Text = label,
            Dock = DockStyle.Top,
            Height = 20,
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted
        };

        if (input is TextBox tb)
        {
            tb.PlaceholderText = placeholder;
        }

        var field = WrapInPill(input);
        field.Dock = DockStyle.Top;

        stack.Controls.Add(field);
        stack.Controls.Add(lbl);

        outer.Controls.Add(stack);
        outer.Controls.Add(badge);
        return outer;
    }

    // Every input in a badge field gets the same 36px rounded pill as the numeric steppers, so a row
    // mixing textboxes, dropdowns, dates and steppers lines up instead of showing four different heights.
    private static Control WrapInPill(Control input)
    {
        switch (input)
        {
            case TextBox:
            case ComboBox:
                var pill = new Controls.IconFieldPanel { Height = 36 };
                if (input is TextBox textBox) textBox.BorderStyle = BorderStyle.None;
                if (input is ComboBox comboBox) comboBox.FlatStyle = FlatStyle.Flat;
                input.BackColor = Theme.Theme.InputBackground;
                input.Font = Theme.Theme.BaseFont;
                input.Dock = DockStyle.Fill;
                input.GotFocus += (_, _) => pill.SetFocused(true);
                input.LostFocus += (_, _) => pill.SetFocused(false);
                pill.Controls.Add(input);
                return pill;
            case DateTimePicker picker:
                return new Controls.DatePickerField(picker);
            default:
                input.Font = Theme.Theme.BaseFont;
                return input;
        }
    }

    public static Control BadgeStepperField(string label, string glyph, Color badgeColor, Color accentColor, Controls.NumericStepperField stepper, int width = 170)
    {
        const int badgeSize = 40;
        const int gap = 12;

        var outer = new Panel { Width = width, Height = 58, Margin = new Padding(0, 0, 20, 12) };

        var badge = new Controls.IconBadge
        {
            Glyph = glyph,
            BadgeColor = badgeColor,
            IconColor = accentColor,
            Size = new Size(badgeSize, badgeSize),
            Location = new Point(0, 9)
        };

        var stack = new Panel { Location = new Point(badgeSize + gap, 0), Width = width - badgeSize - gap, Height = 58 };
        var lbl = new Label
        {
            Text = label,
            Dock = DockStyle.Top,
            Height = 20,
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted
        };

        stepper.Glyph = string.Empty;
        stepper.Dock = DockStyle.Top;
        stepper.Width = width - badgeSize - gap;

        stack.Controls.Add(stepper);
        stack.Controls.Add(lbl);

        outer.Controls.Add(stack);
        outer.Controls.Add(badge);
        return outer;
    }

    /// <summary>Badge field with an extra small action button (e.g. "+ add") placed to the right of the input.</summary>
    public static Control BadgeFieldWithAction(string label, string glyph, Color badgeColor, Color accentColor, ComboBox combo, string placeholder, Button actionButton, int width = 220)
    {
        const int badgeSize = 40;
        const int gap = 12;

        var outer = new Panel { Width = width, Height = 58, Margin = new Padding(0, 0, 20, 12) };

        var badge = new Controls.IconBadge
        {
            Glyph = glyph,
            BadgeColor = badgeColor,
            IconColor = accentColor,
            Size = new Size(badgeSize, badgeSize),
            Location = new Point(0, 9)
        };

        var stack = new Panel { Location = new Point(badgeSize + gap, 0), Width = width - badgeSize - gap, Height = 58 };
        var lbl = new Label
        {
            Text = label,
            Dock = DockStyle.Top,
            Height = 20,
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted
        };

        var comboRow = new Panel { Dock = DockStyle.Top, Height = 36 };
        actionButton.Size = new Size(36, 36);
        actionButton.Dock = DockStyle.Right;
        var spacer = new Panel { Dock = DockStyle.Right, Width = 6 };
        var pill = WrapInPill(combo);
        pill.Dock = DockStyle.Fill;

        // Fill first so it is laid out last (WinForms docks in reverse add order).
        comboRow.Controls.Add(pill);
        comboRow.Controls.Add(spacer);
        comboRow.Controls.Add(actionButton);

        stack.Controls.Add(comboRow);
        stack.Controls.Add(lbl);

        outer.Controls.Add(stack);
        outer.Controls.Add(badge);
        return outer;
    }

    public static Control StepperField(string label, string? glyph, Controls.NumericStepperField stepper, int width = 170)
    {
        var outer = new Panel { Width = width, Height = 58, Margin = new Padding(0, 0, 16, 10) };
        var lbl = new Label
        {
            Text = label,
            Dock = DockStyle.Top,
            Height = 20,
            Font = Theme.Theme.MutedFont,
            ForeColor = Theme.Theme.Muted
        };

        stepper.Dock = DockStyle.Top;
        stepper.Width = width;
        if (glyph is not null) stepper.Glyph = glyph;

        outer.Controls.Add(stepper);
        outer.Controls.Add(lbl);
        return outer;
    }
}
