using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Distribution.Publisher;

/// <summary>
/// Where to publish and the token to publish with. Kept in this PC's AppData; the token is
/// encrypted with the Windows account key so another account on the same machine cannot read it.
/// </summary>
public sealed class PublisherSettings
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DistributionPublisher",
        "settings.json");

    public string RepositoryOwner { get; set; } = string.Empty;
    public string RepositoryName { get; set; } = string.Empty;

    /// <summary>DPAPI-protected GitHub token. Never written out in plain text.</summary>
    [JsonPropertyName("protectedToken")]
    public string? ProtectedToken { get; set; }

    [JsonIgnore]
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(RepositoryOwner) &&
        !string.IsNullOrWhiteSpace(RepositoryName) &&
        !string.IsNullOrWhiteSpace(ProtectedToken);

    [JsonIgnore]
    public string RepositoryUrl => $"https://github.com/{RepositoryOwner}/{RepositoryName}";

    public void SetToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            ProtectedToken = null;
            return;
        }

        var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(token.Trim()), null, DataProtectionScope.CurrentUser);
        ProtectedToken = Convert.ToBase64String(bytes);
    }

    public string? GetToken()
    {
        if (string.IsNullOrWhiteSpace(ProtectedToken)) return null;
        try
        {
            var bytes = ProtectedData.Unprotect(Convert.FromBase64String(ProtectedToken), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception)
        {
            // Copied from another PC or another Windows account: the token cannot be read here.
            return null;
        }
    }

    public static PublisherSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<PublisherSettings>(File.ReadAllText(FilePath)) ?? new PublisherSettings();
            }
        }
        catch (Exception)
        {
            // Fall through to defaults; the user can enter the details again.
        }
        return new PublisherSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
    }
}
