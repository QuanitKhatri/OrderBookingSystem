using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace Distribution.Connect;

/// <summary>Why an update check produced no update, so the UI can explain it.</summary>
public enum UpdateCheckStatus
{
    UpdateAvailable,
    UpToDate,
    /// <summary>Running from a plain folder or from Visual Studio rather than an installed copy.</summary>
    NotInstalled,
    Disabled,
    NotConfigured,
    Failed
}

public sealed record UpdateCheckResult(
    UpdateCheckStatus Status,
    string? NewVersion = null,
    string? CurrentVersion = null,
    string? ReleaseNotes = null,
    string? Error = null)
{
    public bool HasUpdate => Status == UpdateCheckStatus.UpdateAvailable;
}

public interface IUpdateService
{
    /// <summary>The version running now, for display.</summary>
    string CurrentVersion { get; }

    /// <summary>True when this copy was installed by the setup program and can update itself.</summary>
    bool CanUpdate { get; }

    Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default);

    /// <summary>
    /// Downloads the update found by the last check, reporting progress from 0 to 100.
    /// </summary>
    Task DownloadAsync(IProgress<int>? progress, CancellationToken ct = default);

    /// <summary>
    /// Closes the application and installs the downloaded update, then starts the new version.
    /// Does not return when it succeeds.
    /// </summary>
    void ApplyAndRestart();
}

public sealed class UpdateService : IUpdateService
{
    private readonly ConnectOptions _options;
    private readonly ILogger<UpdateService> _logger;
    private readonly UpdateManager? _manager;
    private UpdateInfo? _pending;

    public UpdateService(ConnectOptions options, ILogger<UpdateService> logger)
    {
        _options = options;
        _logger = logger;

        if (!options.UpdatesEnabled || !options.HasUpdateSource) return;

        try
        {
            var updateOptions = new UpdateOptions { AllowVersionDowngrade = false };

            // A plain folder or web address wins over the repository, so a site with no internet can
            // be pointed at a shared folder instead.
            _manager = string.IsNullOrWhiteSpace(options.UpdateSource)
                ? new UpdateManager(
                    new GithubSource(options.RepositoryUrl!.Trim(), accessToken: null, prerelease: options.AllowPrerelease),
                    updateOptions)
                : new UpdateManager(options.UpdateSource.Trim(), updateOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The update source could not be set up.");
        }
    }

    public string CurrentVersion =>
        _manager?.CurrentVersion?.ToString()
        ?? typeof(UpdateService).Assembly.GetName().Version?.ToString(3)
        ?? "1.0.0";

    public bool CanUpdate => _manager?.IsInstalled == true;

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default)
    {
        if (!_options.UpdatesEnabled) return new UpdateCheckResult(UpdateCheckStatus.Disabled, CurrentVersion: CurrentVersion);
        if (!_options.HasUpdateSource) return new UpdateCheckResult(UpdateCheckStatus.NotConfigured, CurrentVersion: CurrentVersion);
        if (_manager is null) return new UpdateCheckResult(UpdateCheckStatus.Failed, CurrentVersion: CurrentVersion, Error: "The update source is not available.");

        // Velopack can only replace files in a copy made by its own installer. A developer build or a
        // copied folder must be told so, instead of silently reporting "up to date".
        if (!_manager.IsInstalled) return new UpdateCheckResult(UpdateCheckStatus.NotInstalled, CurrentVersion: CurrentVersion);

        try
        {
            var info = await Task.Run(() => _manager.CheckForUpdates(), ct);
            _pending = info;

            if (info is null) return new UpdateCheckResult(UpdateCheckStatus.UpToDate, CurrentVersion: CurrentVersion);

            return new UpdateCheckResult(
                UpdateCheckStatus.UpdateAvailable,
                NewVersion: info.TargetFullRelease.Version.ToString(),
                CurrentVersion: CurrentVersion,
                ReleaseNotes: info.TargetFullRelease.NotesMarkdown);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The update check failed.");
            return new UpdateCheckResult(UpdateCheckStatus.Failed, CurrentVersion: CurrentVersion, Error: ex.Message);
        }
    }

    public async Task DownloadAsync(IProgress<int>? progress, CancellationToken ct = default)
    {
        if (_manager is null || _pending is null)
        {
            throw new InvalidOperationException("Check for updates before downloading.");
        }

        await _manager.DownloadUpdatesAsync(_pending, p => progress?.Report(p), ct);
    }

    public void ApplyAndRestart()
    {
        if (_manager is null || _pending is null)
        {
            throw new InvalidOperationException("There is no downloaded update to install.");
        }

        _manager.ApplyUpdatesAndRestart(_pending.TargetFullRelease);
    }
}
