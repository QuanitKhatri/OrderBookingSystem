using System.Globalization;

namespace Distribution.Reports;

internal static class ReportFormat
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static string Text(object? value, ReportColumnKind kind) => value switch
    {
        null => string.Empty,
        string s => s,
        DateTime d => d.ToString("dd MMM yyyy", Culture),
        decimal m => kind switch
        {
            ReportColumnKind.Quantity => m.ToString("#,##0.###", Culture),
            ReportColumnKind.Percent => m.ToString("0.0", Culture) + "%",
            _ => m.ToString("#,##0.##", Culture)
        },
        IFormattable f => f.ToString(null, Culture),
        _ => value.ToString() ?? string.Empty
    };

    public static string GeneratedLine(ReportDocument report) =>
        "Generated on " + report.GeneratedAt.ToString("dd MMM yyyy, hh:mm tt", Culture);
}
