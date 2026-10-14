using Distribution.Services;
using Distribution.Services.Auth;
using Distribution.WinForms.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Distribution.WinForms;

static class Program
{
    [STAThread]
    static void Main()
    { 
        ApplicationConfiguration.Initialize();

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ShowUnhandledException(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => ShowUnhandledException(e.ExceptionObject as Exception);

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

        builder.Services.AddSerilog((_, config) => config
            .ReadFrom.Configuration(builder.Configuration)
            .WriteTo.File(
                Path.Combine(AppContext.BaseDirectory, "logs", "distribution-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30));

        var connectionString = builder.Configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
        builder.Services.AddDistributionServices(connectionString);

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
