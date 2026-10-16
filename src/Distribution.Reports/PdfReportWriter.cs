using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Distribution.Reports;

/// <summary>Lays out a <see cref="ReportDocument"/> once and produces either a PDF file or page images for printing.</summary>
public static class PdfReportWriter
{
    private const string Primary = "#0F5C63";
    private const string HeaderFill = "#E8F1F0";
    private const string Zebra = "#F7F6F2";
    private const string Muted = "#5A5F66";
    private const string RuleColor = "#DEDDD6";

    static PdfReportWriter()
    {
        // QuestPDF's free Community licence covers individuals and businesses below USD 1M annual gross revenue.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static void Save(ReportDocument report, string path) => Build(report).GeneratePdf(path);

    public static byte[] Generate(ReportDocument report) => Build(report).GeneratePdf();

    /// <summary>Renders each page as a PNG so printing and preview match the PDF layout exactly.</summary>
    public static IReadOnlyList<byte[]> RenderPages(ReportDocument report, int dpi = 150) =>
        Build(report).GenerateImages(new ImageGenerationSettings { ImageFormat = ImageFormat.Png, RasterDpi = dpi }).ToList();

    private static IDocument Build(ReportDocument report) => Document.Create(container =>
        container.Page(page =>
        {
            page.Size(report.Landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
            page.Margin(28);
            page.DefaultTextStyle(style => style.FontSize(9).FontColor("#1C1F24"));

            page.Header().Element(c => ComposeHeader(c, report));

            page.Content().PaddingTop(10).Column(column =>
            {
                column.Spacing(16);
                foreach (var table in report.Tables)
                {
                    column.Item().Element(c => ComposeTable(c, table));
                }
            });

            page.Footer().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(ReportFormat.GeneratedLine(report)).FontSize(7.5f).FontColor(Muted);
                row.AutoItem().Text(text =>
                {
                    text.Span("Page ").FontSize(7.5f).FontColor(Muted);
                    text.CurrentPageNumber().FontSize(7.5f).FontColor(Muted);
                    text.Span(" of ").FontSize(7.5f).FontColor(Muted);
                    text.TotalPages().FontSize(7.5f).FontColor(Muted);
                });
            });
        }));

    private static void ComposeHeader(IContainer container, ReportDocument report)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(report.BusinessName).FontSize(14).Bold().FontColor(Primary);
                    if (!string.IsNullOrWhiteSpace(report.BusinessAddress))
                        left.Item().Text(report.BusinessAddress!).FontSize(8.5f).FontColor(Muted);
                    if (!string.IsNullOrWhiteSpace(report.BusinessPhone))
                        left.Item().Text("Phone: " + report.BusinessPhone).FontSize(8.5f).FontColor(Muted);
                });

                row.RelativeItem().Column(right =>
                {
                    right.Item().AlignRight().Text(report.Title).FontSize(14).Bold();
                    if (!string.IsNullOrWhiteSpace(report.Subtitle))
                        right.Item().AlignRight().Text(report.Subtitle!).FontSize(8.5f).FontColor(Muted);
                });
            });

            column.Item().PaddingTop(6).LineHorizontal(1.2f).LineColor(Primary);
        });
    }

    private static void ComposeTable(IContainer container, ReportTable table)
    {
        container.Column(column =>
        {
            if (!string.IsNullOrWhiteSpace(table.Title))
            {
                column.Item().PaddingBottom(4).Text(table.Title!).FontSize(11).SemiBold().FontColor(Primary);
            }

            column.Item().Table(grid =>
            {
                grid.ColumnsDefinition(columns =>
                {
                    foreach (var c in table.Columns) columns.RelativeColumn(c.Width);
                });

                grid.Header(header =>
                {
                    foreach (var c in table.Columns)
                    {
                        header.Cell().Background(HeaderFill).BorderBottom(1).BorderColor(Primary)
                            .PaddingVertical(4).PaddingHorizontal(4)
                            .Element(e => Align(e, c))
                            .Text(c.Header).SemiBold().FontSize(8.5f);
                    }
                });

                for (int r = 0; r < table.Rows.Count; r++)
                {
                    var values = table.Rows[r];
                    bool emphasis = table.EmphasisRows.Contains(r);
                    string fill = r % 2 == 1 ? Zebra : "#FFFFFF";

                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        var c = table.Columns[i];
                        var text = grid.Cell().Background(fill).BorderBottom(0.5f).BorderColor(RuleColor)
                            .PaddingVertical(3).PaddingHorizontal(4)
                            .Element(e => Align(e, c))
                            .Text(ReportFormat.Text(i < values.Length ? values[i] : null, c.Kind));
                        if (emphasis) text.Bold();
                    }
                }

                if (table.Totals is { } totals)
                {
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        var c = table.Columns[i];
                        grid.Cell().BorderTop(1).BorderColor(Primary).PaddingVertical(4).PaddingHorizontal(4)
                            .Element(e => Align(e, c))
                            .Text(ReportFormat.Text(i < totals.Length ? totals[i] : null, c.Kind)).Bold();
                    }
                }
            });

            if (table.Rows.Count == 0)
            {
                column.Item().PaddingTop(6).Text(table.EmptyMessage).Italic().FontColor(Muted);
            }
        });
    }

    private static IContainer Align(IContainer container, ReportColumn column) =>
        column.IsNumeric ? container.AlignRight() : container.AlignLeft();
}
