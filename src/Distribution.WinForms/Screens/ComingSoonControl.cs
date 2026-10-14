namespace Distribution.WinForms.Screens;

public class ComingSoonControl : UserControl
{
    public ComingSoonControl(string screenKey)
    {
        BackColor = Theme.Theme.Background;

        var label = new Label
        {
            Text = $"\"{screenKey}\" is not built yet in this phase.",
            Font = Theme.Theme.BaseFont,
            ForeColor = Theme.Theme.Muted,
            AutoSize = true,
            Anchor = AnchorStyles.None
        };

        Controls.Add(label);
        Resize += (_, _) =>
        {
            label.Location = new Point((Width - label.Width) / 2, (Height - label.Height) / 2);
        };
    }
}
