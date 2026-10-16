using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Distribution.Connect;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the update check and the broadcast message feed. Safe to call even when no
    /// repository is configured: the services then report "not configured" instead of failing.
    /// </summary>
    public static IServiceCollection AddDistributionConnect(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new ConnectOptions();
        configuration.GetSection(ConnectOptions.SectionName).Bind(options);
        services.AddSingleton(options);

        services.AddHttpClient<IBroadcastFeedClient, BroadcastFeedClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            // Media and the feed are served by GitHub, which requires a user agent.
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DistributionManagementSystem");
        });

        // Singleton: it remembers the lock decision between checks, so one instance must be shared.
        services.AddHttpClient(LicenseService.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DistributionManagementSystem");
        });
        services.AddSingleton<ILicenseService, LicenseService>();

        services.AddSingleton<BroadcastStateStore>();
        services.AddSingleton<IUpdateService, UpdateService>();

        return services;
    }
}
