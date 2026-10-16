namespace Distribution.WinForms.Screens;

/// <summary>
/// A cached screen that reloads its lists and drop-downs whenever the user navigates back to it, so records
/// added on other screens (a new customer, supplier, product…) show up without restarting the application.
/// </summary>
public interface IRefreshableScreen
{
    /// <summary>Reloads data while keeping what the user has typed or selected wherever that still exists.</summary>
    Task RefreshAsync();
}
