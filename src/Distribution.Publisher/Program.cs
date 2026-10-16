namespace Distribution.Publisher;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Report(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Report(e.ExceptionObject as Exception);

        Application.Run(new PublisherForm());
    }

    private static void Report(Exception? ex)
    {
        if (ex is null) return;
        MessageBox.Show($"Something went wrong:\n\n{ex.Message}", "Distribution Publisher",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
