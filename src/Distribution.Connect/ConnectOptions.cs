namespace Distribution.Connect;

/// <summary>
/// Where this copy of the application looks for updates and broadcast messages.
/// Read from the "Connect" section of appsettings.json.
/// </summary>
public sealed class ConnectOptions
{
    public const string SectionName = "Connect";

    /// <summary>GitHub repository that holds the releases, for example https://github.com/owner/repo.</summary>
    public string? RepositoryUrl { get; set; }

    /// <summary>
    /// Direct URL of the broadcast feed. Left empty, it is derived from <see cref="RepositoryUrl"/>
    /// as the feed asset of the "broadcasts" release.
    /// </summary>
    public string? BroadcastFeedUrl { get; set; }

    /// <summary>
    /// Takes updates from somewhere other than GitHub: a web address, or a shared folder such as
    /// \\server\updates for a site with no internet. Left empty, the repository above is used.
    /// </summary>
    public string? UpdateSource { get; set; }

    /// <summary>Turns the update check off for a site that must stay on a fixed version.</summary>
    public bool UpdatesEnabled { get; set; } = true;

    /// <summary>Turns broadcast messages off.</summary>
    public bool BroadcastsEnabled { get; set; } = true;

    /// <summary>
    /// Identifies this shop's installation to the distributor, for example AL-MADINA-01. Set once
    /// during installation. Without it the shop can never be locked individually.
    /// </summary>
    public string? SiteId { get; set; }

    /// <summary>Direct URL of the licence list. Derived from the repository when empty.</summary>
    public string? LicenseFeedUrl { get; set; }

    /// <summary>Turns the lock check off. Only for a machine the distributor runs themselves.</summary>
    public bool LicenseCheckEnabled { get; set; } = true;

    /// <summary>How long to wait between checks while the application stays open.</summary>
    public int CheckIntervalMinutes { get; set; } = 60;

    /// <summary>Also offer pre-release versions. Normally only used on a test machine.</summary>
    public bool AllowPrerelease { get; set; }

    public bool HasRepository => !string.IsNullOrWhiteSpace(RepositoryUrl);

    /// <summary>True when updates can be looked for at all.</summary>
    public bool HasUpdateSource => !string.IsNullOrWhiteSpace(UpdateSource) || HasRepository;

    /// <summary>The feed URL to poll, or null when no source is configured.</summary>
    public string? ResolveBroadcastFeedUrl()
    {
        if (!string.IsNullOrWhiteSpace(BroadcastFeedUrl)) return BroadcastFeedUrl.Trim();
        return BuildReleaseAssetUrl(BroadcastFeed.FileName);
    }

    /// <summary>The public address of one file published under the broadcasts release.</summary>
    private string? BuildReleaseAssetUrl(string fileName)
    {
        if (!HasRepository) return null;

        var repo = RepositoryUrl!.Trim().TrimEnd('/');
        if (repo.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) repo = repo[..^4];
        return $"{repo}/releases/download/{BroadcastFeed.ReleaseTag}/{fileName}";
    }

    /// <summary>The licence list to read, or null when no source is configured.</summary>
    public string? ResolveLicenseFeedUrl()
    {
        if (!LicenseCheckEnabled) return null;
        if (!string.IsNullOrWhiteSpace(LicenseFeedUrl)) return LicenseFeedUrl.Trim();
        return BuildReleaseAssetUrl(LicenseFeed.FileName);
    }

    public TimeSpan CheckInterval =>
        TimeSpan.FromMinutes(Math.Clamp(CheckIntervalMinutes, 5, 24 * 60));
}
