using Distribution.Connect;
using Distribution.Services;
using Distribution.Services.Auth;
using Distribution.WinForms.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Velopack;

namespace Distribution.WinForms;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Must run before anything else: on the first start after an update, and during install and
        // uninstall, Velopack handles the run here and exits instead of opening the main window.
        VelopackApp.Build()
            .SetArgs(args)
            .Run();

        ApplicationConfiguration.Initialize();

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ShowUnhandledException(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ShowUnhandledException(e.ExceptionObject as Exception);

        // Settings for this site live outside the program folder, because an update replaces
        // everything inside it. Values here win over the ones shipped with the application.
        var siteSettingsPath = SiteSettings.EnsureExists();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile(siteSettingsPath, optional: true, reloadOnChange: false);

        builder.Services.AddSerilog((_, config) => config
            .ReadFrom.Configuration(builder.Configuration)
            .WriteTo.File(
                Path.Combine(SiteSettings.DataDirectory, "logs", "distribution-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30));

        var connectionString = builder.Configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        if (!SiteSettings.IsConfigured(connectionString))
        {
            MessageBox.Show(
                "This computer has not been set up yet.\n\n" +
                $"Open this file and enter the database details:\n{siteSettingsPath}\n\n" +
                "Your supplier or IT person normally does this once during installation.",
                "Setup needed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            SiteSettings.OpenInEditor(siteSettingsPath);
            return;
        }
        builder.Services.AddDistributionServices(connectionString);
        builder.Services.AddDistributionConnect(builder.Configuration);

        builder.Services.AddTransient<LoginForm>();
        builder.Services.AddTransient<MainForm>();

        using var host = builder.Build();
        host.Start();

        using (var scope = host.Services.CreateScope())
        {
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            authService.EnsureDefaultAdminAsync().GetAwaiter().GetResult();
        }

        Services.Auth.AuthResult? authenticatedUser;
        using (var loginScope = host.Services.CreateScope())
        {
            using var loginForm = loginScope.ServiceProvider.GetRequiredService<LoginForm>();
            authenticatedUser = loginForm.ShowDialog() == DialogResult.OK ? loginForm.AuthenticatedUser : null;
        }

        if (authenticatedUser is not null)
        {
            using var mainForm = host.Services.GetRequiredService<MainForm>();
            mainForm.CurrentUser = authenticatedUser;
            Application.Run(mainForm);
        }

        host.StopAsync().GetAwaiter().GetResult();
    }

    private static void ShowUnhandledException(Exception? ex)
    {
        if (ex is null) return;
        Log.Error(ex, "Unhandled exception");
        MessageBox.Show($"An unexpected error occurred:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
