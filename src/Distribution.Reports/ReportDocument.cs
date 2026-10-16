namespace Distribution.Reports;

public enum ReportColumnKind
{
    Text,
    Date,
    Quantity,
    Money,
    Percent
}

public sealed record ReportColumn(string Header, ReportColumnKind Kind = ReportColumnKind.Text, float Width = 1f)
{
    public bool IsNumeric => Kind is ReportColumnKind.Quantity or ReportColumnKind.Money or ReportColumnKind.Percent;
}

/// <summary>One titled table of a report. Cell values are strings, <see cref="DateTime"/>s or <see cref="decimal"/>s.</summary>
public sealed class ReportTable(string? title, params ReportColumn[] columns)
{
    public string? Title { get; } = title;
    public IReadOnlyList<ReportColumn> Columns { get; } = columns;
    public List<object?[]> Rows { get; } = [];

    /// <summary>Optional bold total row printed under the data.</summary>
    public object?[]? Totals { get; set; }

    /// <summary>Indexes of rows printed in bold, e.g. Gross Profit / Net Profit.</summary>
    public HashSet<int> EmphasisRows { get; } = [];

    public string EmptyMessage { get; set; } = "No records for the selected options.";

    public void AddRow(params object?[] cells) => Rows.Add(cells);
}

/// <summary>A printable/exportable report: business heading, title, filter description and one or more tables.</summary>
public sealed class ReportDocument
{
    public required string Title { get; init; }

    /// <summary>The options the report was produced with, e.g. "Date: 15 Oct 2026 · All companies".</summary>
    public string? Subtitle { get; init; }

    public string BusinessName { get; init; } = "Distribution Management System";
    public string? BusinessAddress { get; init; }
    public string? BusinessPhone { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.Now;
    public bool Landscape { get; init; }
    public List<ReportTable> Tables { get; } = [];

    public bool HasData => Tables.Any(t => t.Rows.Count > 0);
}
