using System.Diagnostics;

namespace Distribution.WinForms;

/// <summary>
/// The settings belonging to this computer rather than to the program files: the database
/// connection above all. An update replaces everything in the program folder, so these are kept
/// outside it and layered on top of the shipped defaults.
/// </summary>
internal static class SiteSettings
{
    public const string FileName = "appsettings.json";

    /// <summary>Shared by every Windows user on this PC, next to the log files.</summary>
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "DistributionManagementSystem");

    public static string FilePath => Path.Combine(DataDirectory, FileName);

    /// <summary>
    /// Makes sure the site settings file exists, creating it from the shipped defaults the first
    /// time. Returns its full path.
    /// </summary>
    public static string EnsureExists()
    {
        var path = FilePath;
        try
        {
            if (File.Exists(path)) return path;

            Directory.CreateDirectory(DataDirectory);

            var shipped = Path.Combine(AppContext.BaseDirectory, FileName);
            if (File.Exists(shipped))
            {
                File.Copy(shipped, path);
            }
        }
        catch (Exception)
        {
            // Without write access here the application still runs on the shipped defaults; it just
            // cannot keep settings across an update, which the setup notes warn about.
        }

        return path;
    }

    /// <summary>False while the connection string is still the placeholder shipped with the setup.</summary>
    public static bool IsConfigured(string connectionString) =>
        !connectionString.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase);

    public static void OpenInEditor(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo("notepad.exe", $"\"{path}\"") { UseShellExecute = true });
            }
        }
        catch (Exception)
        {
            // The person can open the file themselves; the path was shown in the message.
        }
    }
}
