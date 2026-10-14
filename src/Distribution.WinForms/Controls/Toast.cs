namespace Distribution.WinForms.Controls;

public enum ToastKind
{
    Success,
    Warning,
    Danger
}

public class Toast : Form
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 2200 };

    private Toast(string message, ToastKind kind)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = kind switch
        {
            ToastKind.Success => Theme.Theme.Primary,
            ToastKind.Warning => Theme.Theme.Warning,
            ToastKind.Danger => Theme.Theme.Danger,
            _ => Theme.Theme.Primary
        };

        var label = new Label
        {
            Text = message,
            ForeColor = Color.White,
            Font = Theme.Theme.BaseFont,
            AutoSize = true,
            Padding = new Padding(16, 10, 16, 10)
        };
        Controls.Add(label);

        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        _timer.Tick += (_, _) => { _timer.Stop(); Close(); };
    }

    public static void Show(Form owner, string message, ToastKind kind = ToastKind.Success)
    {
        var toast = new Toast(message, kind);
        toast.Load += (_, _) =>
        {
            var ownerBounds = owner.Bounds;
            toast.Location = new Point(
                ownerBounds.Right - toast.Width - 24,
                ownerBounds.Bottom - toast.Height - 24);
        };
        toast.Show(owner);
        toast._timer.Start();
    }
}
