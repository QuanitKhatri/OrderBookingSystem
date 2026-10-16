using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Distribution.Connect;

public interface ILicenseService
{
    /// <summary>What this computer last decided, without going to the network.</summary>
    LicenseState Current { get; }

    /// <summary>
    /// Fetches the published list and updates <see cref="Current"/>. Being offline never locks a
    /// shop that was working, and never unlocks one that was already locked.
    /// </summary>
    Task<LicenseState> CheckAsync(CancellationToken ct = default);
}

public sealed class LicenseService : ILicenseService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ConnectOptions _options;
    private readonly ILogger<LicenseService> _logger;
    private readonly string _statePath;
    private readonly object _gate = new();

    private LicenseState _current;

    public const string HttpClientName = "licence";

    public LicenseService(IHttpClientFactory httpFactory, ConnectOptions options, ILogger<LicenseService> logger)
    {
        _httpFactory = httpFactory;
        _options = options;
        _logger = logger;

        _statePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DistributionManagementSystem",
            "license-state.json");

        _current = Load();
    }

    public LicenseState Current
    {
        get { lock (_gate) return _current; }
    }

    public async Task<LicenseState> CheckAsync(CancellationToken ct = default)
    {
        var url = _options.ResolveLicenseFeedUrl();
        if (string.IsNullOrWhiteSpace(url)) return Current;

        try
        {
            var separator = url.Contains('?') ? '&' : '?';
            using var http = _httpFactory.CreateClient(HttpClientName);
            using var response = await http.GetAsync($"{url}{separator}t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Nothing published: every shop is allowed.
                return Apply(LicenseState.Unlocked with { CheckedAt = DateTimeOffset.UtcNow });
            }

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            var feed = await JsonSerializer.DeserializeAsync<LicenseFeed>(stream, LicenseFeed.JsonOptions, ct);
            if (feed is null) return Current;

            if (feed.Schema > LicenseFeed.CurrentSchema)
            {
                _logger.LogWarning("The licence list uses a newer format ({Schema}); leaving the current state alone.", feed.Schema);
                return Current;
            }

            var (locked, message) = feed.Decide(_options.SiteId);
            return Apply(new LicenseState(locked, message, DateTimeOffset.UtcNow));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Offline, or the list could not be read. Keep whatever was decided last, so that a shop
            // in the middle of a sale is never stopped by a network problem.
            _logger.LogDebug(ex, "Could not read the licence list.");
            return Current;
        }
    }

    private LicenseState Apply(LicenseState state)
    {
        lock (_gate)
        {
            var changed = _current.IsLocked != state.IsLocked || _current.Message != state.Message;
            _current = state;
            if (changed) Save(state);
            return state;
        }
    }

    private LicenseState Load()
    {
        try
        {
            if (File.Exists(_statePath))
            {
                var loaded = JsonSerializer.Deserialize<LicenseState>(File.ReadAllText(_statePath), LicenseFeed.JsonOptions);
                if (loaded is not null) return loaded;
            }
        }
        catch (Exception)
        {
            // A damaged file must not stop the application; treat it as unlocked.
        }
        return LicenseState.Unlocked;
    }

    private void Save(LicenseState state)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
            File.WriteAllText(_statePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not save the licence state.");
        }
    }
}
