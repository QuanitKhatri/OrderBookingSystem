using System.Drawing.Printing;

namespace Distribution.WinForms.Printing;

/// <summary>Prints pre-rendered report pages (see PdfReportWriter.RenderPages), so paper output matches the PDF.</summary>
internal sealed class PageImagePrintDocument : PrintDocument
{
    private readonly IReadOnlyList<byte[]> _pages;
    private int _pageIndex;
    private bool _printerChosen;

    public PageImagePrintDocument(IReadOnlyList<byte[]> pages, string title, bool landscape)
    {
        _pages = pages;
        DocumentName = title;
        DefaultPageSettings.Landscape = landscape;
        UseA4Paper();
    }

    public static bool HasPrinter => PrinterSettings.InstalledPrinters.Count > 0;

    protected override void OnBeginPrint(PrintEventArgs e)
    {
        // The preview window's Print button prints straight to the default printer; let the user pick one first.
        if (e.PrintAction == PrintAction.PrintToPrinter && !_printerChosen)
        {
            using var dialog = new PrintDialog { Document = this, UseEXDialog = true, AllowSomePages = false };
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                e.Cancel = true;
                return;
            }

            _printerChosen = true;
            UseA4Paper();
        }

        _pageIndex = 0;
        base.OnBeginPrint(e);
    }

    protected override void OnPrintPage(PrintPageEventArgs e)
    {
        base.OnPrintPage(e);
        if (_pageIndex >= _pages.Count || e.Graphics is null)
        {
            e.HasMorePages = false;
            return;
        }

        using (var stream = new MemoryStream(_pages[_pageIndex]))
        using (var image = Image.FromStream(stream))
        {
            // The page image already contains the report's own margins, so draw it over the whole sheet.
            // On paper the drawing origin sits at the printer's hard margin, hence the offset; preview has none.
            bool preview = PrintController?.IsPreview == true;
            float offsetX = preview ? 0 : e.PageSettings.HardMarginX;
            float offsetY = preview ? 0 : e.PageSettings.HardMarginY;

            var sheet = e.PageBounds;
            float scale = Math.Min(sheet.Width / (image.Width / image.HorizontalResolution * 100f),
                                   sheet.Height / (image.Height / image.VerticalResolution * 100f));
            float width = image.Width / image.HorizontalResolution * 100f * scale;
            float height = image.Height / image.VerticalResolution * 100f * scale;

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(image, new RectangleF(
                (sheet.Width - width) / 2f - offsetX,
                (sheet.Height - height) / 2f - offsetY,
                width, height));
        }

        _pageIndex++;
        e.HasMorePages = _pageIndex < _pages.Count;
    }

    private void UseA4Paper()
    {
        try
        {
            var a4 = PrinterSettings.PaperSizes.Cast<PaperSize>().FirstOrDefault(p => p.Kind == PaperKind.A4);
            if (a4 is not null) DefaultPageSettings.PaperSize = a4;
        }
        catch (InvalidPrinterException)
        {
            // No usable printer: keep the defaults; printing itself reports the problem.
        }
    }
}
