using ClosedXML.Excel;

namespace Distribution.Reports;

public static class ExcelReportWriter
{
    private static readonly XLColor HeaderFill = XLColor.FromHtml("#E8F1F0");
    private static readonly XLColor Muted = XLColor.FromHtml("#5A5F66");

    public static void Save(ReportDocument report, string path)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName(report.Title));
        int row = 1;

        WriteLine(sheet, row++, report.BusinessName, 14, bold: true);
        if (!string.IsNullOrWhiteSpace(report.BusinessAddress)) WriteLine(sheet, row++, report.BusinessAddress!, 10);
        if (!string.IsNullOrWhiteSpace(report.BusinessPhone)) WriteLine(sheet, row++, "Phone: " + report.BusinessPhone, 10);
        row++;
        WriteLine(sheet, row++, report.Title, 13, bold: true);
        if (!string.IsNullOrWhiteSpace(report.Subtitle)) WriteLine(sheet, row++, report.Subtitle!, 10);
        WriteLine(sheet, row++, ReportFormat.GeneratedLine(report), 9, muted: true);
        row++;

        int firstTableRow = row;
        foreach (var table in report.Tables)
        {
            if (!string.IsNullOrWhiteSpace(table.Title)) WriteLine(sheet, row++, table.Title!, 11, bold: true);

            for (int i = 0; i < table.Columns.Count; i++)
            {
                var cell = sheet.Cell(row, i + 1);
                cell.Value = table.Columns[i].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = HeaderFill;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                if (table.Columns[i].IsNumeric) cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }
            row++;

            for (int r = 0; r < table.Rows.Count; r++)
            {
                WriteCells(sheet, row, table.Columns, table.Rows[r]);
                if (table.EmphasisRows.Contains(r)) sheet.Range(row, 1, row, table.Columns.Count).Style.Font.Bold = true;
                row++;
            }

            if (table.Rows.Count == 0) WriteLine(sheet, row++, table.EmptyMessage, 10, muted: true);

            if (table.Totals is { } totals)
            {
                WriteCells(sheet, row, table.Columns, totals);
                var range = sheet.Range(row, 1, row, table.Columns.Count);
                range.Style.Font.Bold = true;
                range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                row++;
            }

            row++;
        }

        // Size columns from the tables only; the long heading lines above them would otherwise widen column A.
        int columnCount = Math.Max(1, report.Tables.Select(t => t.Columns.Count).DefaultIfEmpty(1).Max());
        for (int c = 1; c <= columnCount; c++)
        {
            var column = sheet.Column(c);
            column.AdjustToContents(firstTableRow, Math.Max(firstTableRow, row));
            column.Width = Math.Clamp(column.Width + 2, 10, 60);
        }

        sheet.PageSetup.PageOrientation = report.Landscape ? XLPageOrientation.Landscape : XLPageOrientation.Portrait;
        sheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
        sheet.PageSetup.FitToPages(1, 0);

        workbook.SaveAs(path);
    }

    private static void WriteLine(IXLWorksheet sheet, int row, string text, double fontSize, bool bold = false, bool muted = false)
    {
        var cell = sheet.Cell(row, 1);
        cell.Value = text;
        cell.Style.Font.FontSize = fontSize;
        cell.Style.Font.Bold = bold;
        if (muted) cell.Style.Font.FontColor = Muted;
    }

    private static void WriteCells(IXLWorksheet sheet, int row, IReadOnlyList<ReportColumn> columns, object?[] values)
    {
        for (int i = 0; i < columns.Count && i < values.Length; i++)
        {
            var cell = sheet.Cell(row, i + 1);
            var column = columns[i];
            switch (values[i])
            {
                case null:
                    break;
                case decimal number:
                    // Store the exact number so Excel can total it; only the display format is rounded.
                    cell.Value = (double)number;
                    cell.Style.NumberFormat.Format = NumberFormat(column.Kind, number);
                    break;
                case DateTime date:
                    cell.Value = date;
                    cell.Style.DateFormat.Format = "dd mmm yyyy";
                    break;
                default:
                    cell.Value = values[i]!.ToString();
                    break;
            }

            if (column.IsNumeric) cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }
    }

    // A format such as "#,##0.##" leaves a stray decimal point on whole numbers in Excel, so pick per value.
    private static string NumberFormat(ReportColumnKind kind, decimal value)
    {
        bool whole = value == decimal.Truncate(value);
        return kind switch
        {
            ReportColumnKind.Percent => "0.0\"%\"",
            ReportColumnKind.Quantity => whole ? "#,##0" : "#,##0.###",
            _ => whole ? "#,##0" : "#,##0.00"
        };
    }

    private static string SheetName(string title)
    {
        var cleaned = new string(title.Where(ch => ch is not ('[' or ']' or ':' or '*' or '?' or '/' or '\\')).ToArray()).Trim();
        if (cleaned.Length == 0) cleaned = "Report";
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }
}
