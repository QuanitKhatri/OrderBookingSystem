using System.Text.Json;
using Distribution.Connect;
using Octokit;

namespace Distribution.Publisher;

/// <summary>
/// Publishes which shops may use the application. The list lives beside the notifications, under
/// the same fixed release tag.
/// </summary>
public sealed class LicensePublisher(PublisherSettings settings)
{
    private static readonly JsonSerializerOptions FeedJson = new() { WriteIndented = true };

    private GitHubClient CreateClient()
    {
        var token = settings.GetToken()
            ?? throw new InvalidOperationException("No GitHub token is saved. Open Settings and add one.");

        return new GitHubClient(new ProductHeaderValue("DistributionPublisher"))
        {
            Credentials = new Credentials(token)
        };
    }

    public async Task<LicenseFeed> GetFeedAsync()
    {
        var client = CreateClient();
        var release = await FindReleaseAsync(client);
        var asset = release?.Assets.FirstOrDefault(a => a.Name == LicenseFeed.FileName);
        if (asset is null) return new LicenseFeed();

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("DistributionPublisher");
        var json = await http.GetStringAsync($"{asset.BrowserDownloadUrl}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
        return JsonSerializer.Deserialize<LicenseFeed>(json, LicenseFeed.JsonOptions) ?? new LicenseFeed();
    }

    /// <summary>Locks or unlocks one shop and publishes the change.</summary>
    public async Task SetSiteAsync(string siteId, bool locked, string? message, string? label, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(siteId))
        {
            throw new InvalidOperationException("Enter the Site Id exactly as it is in that shop's settings file.");
        }

        var feed = await GetFeedAsync();
        feed.Sites[siteId.Trim()] = new SiteLicense
        {
            Locked = locked,
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await PublishAsync(feed, ct);
    }

    public async Task RemoveSiteAsync(string siteId, CancellationToken ct = default)
    {
        var feed = await GetFeedAsync();
        if (!feed.Sites.Remove(siteId)) return;
        await PublishAsync(feed, ct);
    }

    /// <summary>Locks or unlocks every installation at once.</summary>
    public async Task SetLockAllAsync(bool lockAll, string? message, CancellationToken ct = default)
    {
        var feed = await GetFeedAsync();
        feed.LockAll = lockAll;
        feed.LockAllMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        await PublishAsync(feed, ct);
    }

    private async Task PublishAsync(LicenseFeed feed, CancellationToken ct)
    {
        var client = CreateClient();
        var release = await EnsureReleaseAsync(client);

        feed.Schema = LicenseFeed.CurrentSchema;
        feed.GeneratedAt = DateTimeOffset.UtcNow;

        // A release asset cannot be replaced in place, so the old one goes first.
        var existing = release.Assets.FirstOrDefault(a => a.Name == LicenseFeed.FileName);
        if (existing is not null)
        {
            await client.Repository.Release.DeleteAsset(settings.RepositoryOwner, settings.RepositoryName, existing.Id);
        }

        using var stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(feed, FeedJson));
        await client.Repository.Release.UploadAsset(release, new ReleaseAssetUpload
        {
            FileName = LicenseFeed.FileName,
            ContentType = "application/json",
            RawData = stream
        }, ct);
    }

    private async Task<Release?> FindReleaseAsync(GitHubClient client)
    {
        try
        {
            return await client.Repository.Release.Get(settings.RepositoryOwner, settings.RepositoryName, BroadcastFeed.ReleaseTag);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private async Task<Release> EnsureReleaseAsync(GitHubClient client)
    {
        var existing = await FindReleaseAsync(client);
        if (existing is not null) return existing;

        return await client.Repository.Release.Create(settings.RepositoryOwner, settings.RepositoryName, new NewRelease(BroadcastFeed.ReleaseTag)
        {
            Name = "Customer notifications",
            Body = "Holds the notification list, its media, and the list of shops that may use the application. Do not delete.",
            Prerelease = false,
            MakeLatest = MakeLatestQualifier.False
        });
    }
}
