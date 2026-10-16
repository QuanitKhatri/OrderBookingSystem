using System.Diagnostics;
using Distribution.Reports;
using Distribution.Services.Admin;
using Distribution.WinForms.Controls;
using Icons = Distribution.WinForms.Theme.Icons;

namespace Distribution.WinForms.Printing;

/// <summary>Export to Excel / PDF and print (with preview) for the report screens.</summary>
internal static class ReportOutput
{
    /// <summary>Starts a report with the business name, address and phone from Settings as its heading.</summary>
    public static async Task<ReportDocument> CreateAsync(ISettingsService settings, string title, string? subtitle, bool landscape = false)
    {
        var s = await settings.GetAsync();
        return new ReportDocument
        {
            Title = title,
            Subtitle = subtitle,
            Landscape = landscape,
            BusinessName = string.IsNullOrWhiteSpace(s.CompanyName) ? "Distribution Management System" : s.CompanyName,
            BusinessAddress = s.CompanyAddress,
            BusinessPhone = s.CompanyPhone
        };
    }

    /// <summary>
    /// Adds "Export Excel", "Export PDF" and "Print" buttons to a report's button row. <paramref name="build"/>
    /// returns the report for what is currently on screen, or null after telling the user why it cannot.
    /// </summary>
    public static void AddButtons(FlowLayoutPanel buttonRow, Func<Task<ReportDocument?>> build)
    {
        var excel = new ThemedButton { Text = "Export Excel", Width = 150, Variant = ThemedButtonVariant.Secondary, IconGlyph = Icons.Download, Margin = new Padding(24, 0, 0, 0) };
        var pdf = new ThemedButton { Text = "Export PDF", Width = 140, Variant = ThemedButtonVariant.Secondary, IconGlyph = Icons.ReportDocument, Margin = new Padding(8, 0, 0, 0) };
        var print = new ThemedButton { Text = "Print", Width = 110, Variant = ThemedButtonVariant.Secondary, IconGlyph = Icons.Print, Margin = new Padding(8, 0, 0, 0) };

        excel.Click += async (_, _) => await RunAsync(buttonRow, build, ExportExcelAsync);
        pdf.Click += async (_, _) => await RunAsync(buttonRow, build, ExportPdfAsync);
        print.Click += async (_, _) => await RunAsync(buttonRow, build, PrintAsync);

        buttonRow.Controls.Add(excel);
        buttonRow.Controls.Add(pdf);
        buttonRow.Controls.Add(print);
    }

    private static async Task RunAsync(Control host, Func<Task<ReportDocument?>> build, Func<Form, ReportDocument, Task> action)
    {
        var form = host.FindForm();
        if (form is null) return;

        ReportDocument? report;
        try
        {
            report = await build();
        }
        catch (Exception ex)
        {
            Toast.Show(form, $"Could not prepare the report: {ex.Message}", ToastKind.Danger);
            return;
        }

        if (report is null) return;
        if (!report.HasData)
        {
            Toast.Show(form, "There is nothing to export for the selected options.", ToastKind.Warning);
            return;
        }

        await action(form, report);
    }

    private static async Task ExportExcelAsync(Form owner, ReportDocument report)
    {
        var path = AskForFile(owner, report, "Excel Workbook (*.xlsx)|*.xlsx", ".xlsx", "Export to Excel");
        if (path is null) return;

        if (await SaveAsync(owner, () => ExcelReportWriter.Save(report, path)))
        {
            OfferToOpen(owner, path);
        }
    }

    private static async Task ExportPdfAsync(Form owner, ReportDocument report)
    {
        var path = AskForFile(owner, report, "PDF Document (*.pdf)|*.pdf", ".pdf", "Export to PDF");
        if (path is null) return;

        if (await SaveAsync(owner, () => PdfReportWriter.Save(report, path)))
        {
            OfferToOpen(owner, path);
        }
    }

    private static async Task PrintAsync(Form owner, ReportDocument report)
    {
        if (!PageImagePrintDocument.HasPrinter)
        {
            MessageBox.Show(owner, "No printer is installed on this computer.\n\nUse Export PDF to save the report instead.",
                "Print", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        IReadOnlyList<byte[]> pages;
        owner.UseWaitCursor = true;
        try
        {
            pages = await Task.Run(() => PdfReportWriter.RenderPages(report));
        }
        catch (Exception ex)
        {
            Toast.Show(owner, $"Could not prepare the report for printing: {ex.Message}", ToastKind.Danger);
            return;
        }
        finally
        {
            owner.UseWaitCursor = false;
        }

        using var document = new PageImagePrintDocument(pages, report.Title, report.Landscape);
        using var preview = new PrintPreviewDialog
        {
            Document = document,
            Text = $"Print Preview — {report.Title}",
            WindowState = FormWindowState.Maximized,
            UseAntiAlias = true,
            ShowIcon = false
        };

        try
        {
            preview.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, $"Printing failed: {ex.Message}", "Print", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string? AskForFile(Form owner, ReportDocument report, string filter, string extension, string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var baseName = new string($"{report.Title} {DateTime.Now:yyyy-MM-dd}".Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());

        using var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            DefaultExt = extension,
            AddExtension = true,
            FileName = baseName + extension,
            OverwritePrompt = true
        };
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.FileName : null;
    }

    private static async Task<bool> SaveAsync(Form owner, Action write)
    {
        owner.UseWaitCursor = true;
        try
        {
            await Task.Run(write);
            return true;
        }
        catch (IOException ex)
        {
            MessageBox.Show(owner, $"The file could not be saved. If it is open in another program, close it and try again.\n\n{ex.Message}",
                "Export", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, $"Export failed: {ex.Message}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        finally
        {
            owner.UseWaitCursor = false;
        }
    }

    private static void OfferToOpen(Form owner, string path)
    {
        var answer = MessageBox.Show(owner, $"Saved {Path.GetFileName(path)}.\n\nOpen it now?", "Export complete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if (answer != DialogResult.Yes) return;

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, $"The file was saved but could not be opened: {ex.Message}", "Export",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
