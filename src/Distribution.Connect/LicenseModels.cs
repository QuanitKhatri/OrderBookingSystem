using System.Text.Json;
using System.Text.Json.Serialization;

namespace Distribution.Connect;

/// <summary>What the distributor has decided about one shop's installation.</summary>
public sealed class SiteLicense
{
    /// <summary>True stops the shop using the application until it is set back to false.</summary>
    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    /// <summary>Shown on the lock screen, for example a reason and a phone number.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>For the distributor's own reference; never shown to the shop.</summary>
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>The published list of shops and whether each may use the application.</summary>
public sealed class LicenseFeed
{
    [JsonPropertyName("schema")]
    public int Schema { get; set; } = CurrentSchema;

    [JsonPropertyName("generatedAt")]
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>Locks every installation at once. Use with care.</summary>
    [JsonPropertyName("lockAll")]
    public bool LockAll { get; set; }

    [JsonPropertyName("lockAllMessage")]
    public string? LockAllMessage { get; set; }

    /// <summary>Keyed on the Site Id written into each installation's settings file.</summary>
    [JsonPropertyName("sites")]
    public Dictionary<string, SiteLicense> Sites { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public const int CurrentSchema = 1;

    /// <summary>File name of the list, both as a release asset and on disk.</summary>
    public const string FileName = "licenses.json";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Decides what a given installation may do. A site that is not listed is allowed: a shop must
    /// never be stopped because the distributor forgot to add it.
    /// </summary>
    public (bool Locked, string? Message) Decide(string? siteId)
    {
        if (LockAll) return (true, LockAllMessage);

        if (!string.IsNullOrWhiteSpace(siteId) && Sites.TryGetValue(siteId.Trim(), out var site) && site.Locked)
        {
            return (true, site.Message);
        }

        return (false, null);
    }
}

/// <summary>The lock decision this computer is working to.</summary>
public sealed record LicenseState(bool IsLocked, string? Message, DateTimeOffset? CheckedAt)
{
    public static readonly LicenseState Unlocked = new(false, null, null);

    public const string DefaultMessage =
        "This software has been locked by your supplier.\n\n" +
        "Please contact them to have it switched back on.";

    public string MessageOrDefault =>
        string.IsNullOrWhiteSpace(Message) ? DefaultMessage : Message!;
}
