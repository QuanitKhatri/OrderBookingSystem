using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Distribution.Connect;
using Octokit;

namespace Distribution.Publisher;

/// <summary>
/// Publishes broadcast messages to a GitHub release. The feed and all its media live under one
/// fixed tag, kept apart from the version releases the updater reads.
/// </summary>
public sealed class BroadcastPublisher(PublisherSettings settings)
{
    private static readonly JsonSerializerOptions FeedJson = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private GitHubClient CreateClient()
    {
        var token = settings.GetToken()
            ?? throw new InvalidOperationException("No GitHub token is saved. Open Settings and add one.");

        return new GitHubClient(new ProductHeaderValue("DistributionPublisher"))
        {
            Credentials = new Credentials(token)
        };
    }

    /// <summary>Confirms the token works and can write to the repository.</summary>
    public async Task<string> TestConnectionAsync()
    {
        var client = CreateClient();
        var user = await client.User.Current();
        var repo = await client.Repository.Get(settings.RepositoryOwner, settings.RepositoryName);

        if (!repo.Permissions.Push)
        {
            throw new InvalidOperationException(
                $"The account '{user.Login}' can read {repo.FullName} but cannot publish to it. " +
                "Use a token from an account with write access.");
        }

        return $"Connected as {user.Login} — can publish to {repo.FullName}.";
    }

    public async Task<BroadcastFeed> GetFeedAsync()
    {
        var client = CreateClient();
        var release = await FindBroadcastReleaseAsync(client);
        if (release is null) return new BroadcastFeed();

        var asset = release.Assets.FirstOrDefault(a => a.Name == BroadcastFeed.FileName);
        if (asset is null) return new BroadcastFeed();

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("DistributionPublisher");
        var json = await http.GetStringAsync($"{asset.BrowserDownloadUrl}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
        return JsonSerializer.Deserialize<BroadcastFeed>(json, BroadcastFeed.JsonOptions) ?? new BroadcastFeed();
    }

    /// <summary>
    /// Adds a message to the feed and publishes it. Any picture or video is uploaded first and the
    /// message points at its public address.
    /// </summary>
    public async Task<BroadcastMessage> PublishAsync(
        BroadcastMessage message,
        string? imagePath,
        string? videoPath,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var client = CreateClient();
        progress?.Report("Preparing the release…");
        var release = await EnsureBroadcastReleaseAsync(client);

        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            progress?.Report("Uploading the picture…");
            message.ImageUrl = await UploadAssetAsync(client, release, imagePath!, message.Id, ct);
        }

        if (!string.IsNullOrWhiteSpace(videoPath))
        {
            progress?.Report("Uploading the video… this can take a while.");
            message.VideoUrl = await UploadAssetAsync(client, release, videoPath!, message.Id, ct);
        }

        progress?.Report("Updating the message list…");
        var feed = await GetFeedAsync();
        feed.Messages.RemoveAll(m => m.Id == message.Id);
        feed.Messages.Insert(0, message);
        feed.Schema = BroadcastFeed.CurrentSchema;
        feed.GeneratedAt = DateTimeOffset.UtcNow;

        await UploadFeedAsync(client, release, feed, ct);
        progress?.Report("Sent.");
        return message;
    }

    /// <summary>Removes a message so customers stop seeing it. Its media files are left in place.</summary>
    public async Task DeleteAsync(string messageId, CancellationToken ct = default)
    {
        var client = CreateClient();
        var release = await FindBroadcastReleaseAsync(client)
            ?? throw new InvalidOperationException("Nothing has been published yet.");

        var feed = await GetFeedAsync();
        if (feed.Messages.RemoveAll(m => m.Id == messageId) == 0) return;

        feed.GeneratedAt = DateTimeOffset.UtcNow;
        await UploadFeedAsync(client, release, feed, ct);
    }

    private async Task UploadFeedAsync(GitHubClient client, Release release, BroadcastFeed feed, CancellationToken ct)
    {
        // A release asset cannot be overwritten in place, so the old one is removed first.
        var existing = release.Assets.FirstOrDefault(a => a.Name == BroadcastFeed.FileName);
        if (existing is not null)
        {
            await client.Repository.Release.DeleteAsset(settings.RepositoryOwner, settings.RepositoryName, existing.Id);
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(feed, FeedJson);
        using var stream = new MemoryStream(bytes);
        await client.Repository.Release.UploadAsset(release, new ReleaseAssetUpload
        {
            FileName = BroadcastFeed.FileName,
            ContentType = "application/json",
            RawData = stream
        }, ct);
    }

    private async Task<string> UploadAssetAsync(GitHubClient client, Release release, string path, string messageId, CancellationToken ct)
    {
        var file = new FileInfo(path);
        if (!file.Exists) throw new FileNotFoundException("The file could not be found.", path);

        // GitHub refuses release assets over 2 GB; keep well under that for a sane download too.
        const long maxBytes = 200L * 1024 * 1024;
        if (file.Length > maxBytes)
        {
            throw new InvalidOperationException(
                $"'{file.Name}' is {file.Length / 1024 / 1024} MB. Keep media under 200 MB so customers can load it.");
        }

        // Prefix with the message id so two messages can use files of the same name.
        var assetName = $"{messageId}-{SafeName(file.Name)}";

        var existing = release.Assets.FirstOrDefault(a => a.Name == assetName);
        if (existing is not null)
        {
            await client.Repository.Release.DeleteAsset(settings.RepositoryOwner, settings.RepositoryName, existing.Id);
        }

        await using var stream = file.OpenRead();
        var uploaded = await client.Repository.Release.UploadAsset(release, new ReleaseAssetUpload
        {
            FileName = assetName,
            ContentType = ContentTypeFor(file.Extension),
            RawData = stream
        }, ct);

        return uploaded.BrowserDownloadUrl;
    }

    private static string SafeName(string name)
    {
        var cleaned = new string(name.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '-').ToArray());
        return cleaned.Length > 80 ? cleaned[^80..] : cleaned;
    }

    private static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".mp4" => "video/mp4",
        ".webm" => "video/webm",
        _ => "application/octet-stream"
    };

    private async Task<Release?> FindBroadcastReleaseAsync(GitHubClient client)
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

    private async Task<Release> EnsureBroadcastReleaseAsync(GitHubClient client)
    {
        var existing = await FindBroadcastReleaseAsync(client);
        if (existing is not null) return existing;

        return await client.Repository.Release.Create(settings.RepositoryOwner, settings.RepositoryName, new NewRelease(BroadcastFeed.ReleaseTag)
        {
            Name = "Customer notifications",
            Body = "Holds the notification list and its pictures and videos. Do not delete.",
            Prerelease = false,
            MakeLatest = MakeLatestQualifier.False
        });
    }

    /// <summary>A short, readable id, for example 20261015-2214-a1b2.</summary>
    public static string NewMessageId() =>
        $"{DateTime.UtcNow:yyyyMMdd-HHmm}-{Guid.NewGuid().ToString("N")[..4]}";
}
