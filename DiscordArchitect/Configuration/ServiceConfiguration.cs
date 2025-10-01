using DiscordArchitect.DiscordFactories;
using DiscordArchitect.Hosting;
using DiscordArchitect.Options;
using DiscordArchitect.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiscordArchitect.Configuration;

/// <summary>
/// Provides methods for configuring services in the dependency injection container.
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Configures all services in the dependency injection container.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="config">Configuration object.</param>
    /// <param name="options">Discord options.</param>
    public static void ConfigureServices(IServiceCollection services, IConfiguration config, DiscordOptions options)
    {
        // Add options
        services.AddSingleton(options);
        services.AddOptions<DiscordOptions>().Configure(o =>
        {
            o.Token = options.Token;
            o.ServerId = options.ServerId;
            o.SourceCategoryName = options.SourceCategoryName;
            o.CreateRolePerCategory = options.CreateRolePerCategory;
            o.EveryoneAccessToNewCategory = options.EveryoneAccessToNewCategory;
            o.SyncChannelsToCategory = options.SyncChannelsToCategory;
            o.TestMode = options.TestMode;
            o.Verbose = options.Verbose;
            o.JsonOutput = options.JsonOutput;
            o.AutoCleanup = options.AutoCleanup;
        });

        // Discord client
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DiscordClient");
            return DiscordClientFactory.Create(logger);
        });

        // HttpClient for REST tags
        services.AddSingleton(sp =>
        {
            var o = sp.GetRequiredService<DiscordOptions>();
            return RestClientFactory.Create(o.Token);
        });

        // Application services
        services.AddSingleton<Prompt>();
        services.AddSingleton<DiagnosticsService>();
        services.AddSingleton<PermissionPlanner>();
        services.AddSingleton<ForumTagService>();
        services.AddSingleton<CleanupService>();
        services.AddSingleton<VerificationService>();
        services.AddSingleton<CategoryCloner>();

        // Hosted service
        services.AddHostedService<DiscordHostedService>();
    }
}
