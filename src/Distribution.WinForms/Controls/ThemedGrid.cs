namespace Distribution.WinForms.Controls;

public class ThemedGrid : DataGridView
{
    public ThemedGrid()
    {
        BorderStyle = BorderStyle.None;
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        GridColor = Theme.Theme.Border;
        BackgroundColor = Theme.Theme.Surface;
        RowHeadersVisible = false;
        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToResizeRows = false;
        ReadOnly = true;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        MultiSelect = false;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        ColumnHeadersHeight = 40;
        RowTemplate.Height = 36;
        EnableHeadersVisualStyles = false;
        Font = Theme.Theme.BaseFont;

        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Theme.Background,
            ForeColor = Theme.Theme.Muted,
            Font = new Font(Theme.Theme.FontFamily, 9f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            SelectionBackColor = Theme.Theme.Background,
            SelectionForeColor = Theme.Theme.Muted,
            Padding = new Padding(8, 0, 8, 0)
        };

        DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Theme.Surface,
            ForeColor = Theme.Theme.Text,
            SelectionBackColor = Theme.Theme.Primary,
            SelectionForeColor = Color.White,
            Padding = new Padding(8, 0, 8, 0)
        };

        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Theme.Background
        };
    }

    private bool _clearQueued;

    // DataGridView auto-selects the first row whenever rows are (re)loaded, which looks like a row
    // the user picked even though nothing was loaded into the edit form. Only a real click selects.
    protected override void OnRowsAdded(DataGridViewRowsAddedEventArgs e)
    {
        base.OnRowsAdded(e);
        QueueClearSelection();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        QueueClearSelection();
    }

    private void QueueClearSelection()
    {
        if (_clearQueued || !IsHandleCreated || Focused) return;
        _clearQueued = true;
        BeginInvoke(() =>
        {
            _clearQueued = false;
            if (Focused || IsDisposed) return;
            ClearSelection();
            CurrentCell = null;
        });
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        QueueClearSelection();
    }

    public void RightAlignNumericColumns()
    {
        foreach (DataGridViewColumn col in Columns)
        {
            if (col.ValueType == typeof(decimal) || col.ValueType == typeof(int) ||
                col.ValueType == typeof(double) || col.ValueType == typeof(long))
            {
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                col.DefaultCellStyle.Format = "N0";
            }
        }
    }
}
