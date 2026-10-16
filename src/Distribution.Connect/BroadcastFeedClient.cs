using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Distribution.Connect;

public interface IBroadcastFeedClient
{
    /// <summary>
    /// Fetches the current messages, newest first, with expired ones removed. Returns an empty list
    /// when broadcasts are switched off, nothing has been published, or the PC is offline.
    /// </summary>
    Task<IReadOnlyList<BroadcastMessage>> GetMessagesAsync(CancellationToken ct = default);
}

public sealed class BroadcastFeedClient(HttpClient http, ConnectOptions options, ILogger<BroadcastFeedClient> logger)
    : IBroadcastFeedClient
{
    public async Task<IReadOnlyList<BroadcastMessage>> GetMessagesAsync(CancellationToken ct = default)
    {
        if (!options.BroadcastsEnabled) return [];

        var url = options.ResolveBroadcastFeedUrl();
        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogDebug("No broadcast feed is configured.");
            return [];
        }

        try
        {
            // A cache-busting value: GitHub serves release assets through a CDN that would otherwise
            // keep handing back the previous feed for several minutes after a new message is sent.
            var separator = url.Contains('?') ? '&' : '?';
            var requestUrl = $"{url}{separator}t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            using var response = await http.GetAsync(requestUrl, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogDebug("No broadcast feed has been published yet.");
                return [];
            }

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var feed = await JsonSerializer.DeserializeAsync<BroadcastFeed>(stream, BroadcastFeed.JsonOptions, ct);
            if (feed is null) return [];

            if (feed.Schema > BroadcastFeed.CurrentSchema)
            {
                logger.LogWarning(
                    "The broadcast feed uses format {Schema}, newer than this version understands ({Supported}). Update the application.",
                    feed.Schema, BroadcastFeed.CurrentSchema);
                return [];
            }

            var now = DateTimeOffset.UtcNow;
            return feed.Messages
                .Where(m => !string.IsNullOrWhiteSpace(m.Id) && !m.IsExpired(now))
                .OrderByDescending(m => m.PublishedAt)
                .ToList();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Being offline is normal and must never interrupt the user's work.
            logger.LogDebug(ex, "Could not read the broadcast feed.");
            return [];
        }
    }
}
