namespace Distribution.WinForms.Controls;

public static class PromptDialog
{
    public static string? Show(IWin32Window owner, string title, string label)
    {
        using var form = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(360, 150),
            BackColor = Theme.Theme.Background,
            Font = Theme.Theme.BaseFont
        };

        var lbl = new Label { Text = label, Location = new Point(20, 20), AutoSize = true, ForeColor = Theme.Theme.Text };
        var input = new TextBox { Location = new Point(20, 46), Width = 320, Font = Theme.Theme.BaseFont };
        var okButton = new ThemedButton { Text = "OK", Location = new Point(180, 90), Width = 80 };
        var cancelButton = new ThemedButton { Text = "Cancel", Location = new Point(264, 90), Width = 80, Variant = ThemedButtonVariant.Secondary };

        string? result = null;
        okButton.Click += (_, _) => { result = input.Text.Trim(); form.DialogResult = DialogResult.OK; };
        cancelButton.Click += (_, _) => form.DialogResult = DialogResult.Cancel;
        input.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                result = input.Text.Trim();
                form.DialogResult = DialogResult.OK;
            }
        };

        form.Controls.Add(input);
        form.Controls.Add(lbl);
        form.Controls.Add(okButton);
        form.Controls.Add(cancelButton);
        form.AcceptButton = okButton;
        form.CancelButton = cancelButton;

        return form.ShowDialog(owner) == DialogResult.OK ? result : null;
    }
}
