using System.Text.Json.Serialization;

namespace Distribution.Connect;

public enum BroadcastKind
{
    /// <summary>General news or information.</summary>
    Info,
    /// <summary>Marketing or a promotional offer.</summary>
    Offer,
    /// <summary>Something the user must act on.</summary>
    Alert
}

/// <summary>
/// One message published by the distributor and shown on every customer's copy of the application.
/// </summary>
public sealed class BroadcastMessage
{
    /// <summary>Stable identifier. Read/unread state on each PC is keyed on this.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Plain text body. Line breaks are kept when the message is displayed.</summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public BroadcastKind Kind { get; set; } = BroadcastKind.Info;

    /// <summary>Poster or picture shown above the text. Any URL the customer's PC can reach.</summary>
    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    /// <summary>Video shown with play controls. MP4 (H.264) plays everywhere.</summary>
    [JsonPropertyName("videoUrl")]
    public string? VideoUrl { get; set; }

    /// <summary>Optional "Learn more" link, opened in the customer's own browser.</summary>
    [JsonPropertyName("linkUrl")]
    public string? LinkUrl { get; set; }

    /// <summary>Text on the link button. Defaults to "Open link".</summary>
    [JsonPropertyName("linkText")]
    public string? LinkText { get; set; }

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>After this moment the message stops being shown. Null means it never expires.</summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; set; }

    public bool IsExpired(DateTimeOffset now) => ExpiresAt is { } expiry && expiry <= now;

    public bool HasMedia => !string.IsNullOrWhiteSpace(ImageUrl) || !string.IsNullOrWhiteSpace(VideoUrl);
}

/// <summary>The published feed: every current message, newest first.</summary>
public sealed class BroadcastFeed
{
    /// <summary>Feed format version, so older clients can refuse a format they do not understand.</summary>
    [JsonPropertyName("schema")]
    public int Schema { get; set; } = 1;

    [JsonPropertyName("generatedAt")]
    public DateTimeOffset GeneratedAt { get; set; }

    [JsonPropertyName("messages")]
    public List<BroadcastMessage> Messages { get; set; } = [];

    public const int CurrentSchema = 1;

    /// <summary>How the feed is read and written, shared by the application and the publisher.</summary>
    public static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    /// <summary>File name of the feed, both as a release asset and on disk.</summary>
    public const string FileName = "notifications.json";

    /// <summary>Release tag that holds the feed and its media. Kept separate from version releases.</summary>
    public const string ReleaseTag = "broadcasts";
}
