using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using DiscordArchitect.Options;

namespace DiscordArchitect.Logging;

/// <summary>
/// Provides Serilog configuration for structured logging with support for verbose and JSON output modes.
/// </summary>
public static class SerilogConfiguration
{
    internal const string HumanConsoleTemplate =
        "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    internal const string HumanFileTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Applies minimum levels and suppresses noisy framework namespaces on the console.
    /// </summary>
    public static LoggerConfiguration WithDiscordArchitectLevels(this LoggerConfiguration cfg, DiscordOptions options)
    {
        var minimumLevel = options.Verbose ? LogEventLevel.Debug : LogEventLevel.Information;
        return cfg
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information);
    }

    /// <summary>
    /// Configures Serilog based on the provided options.
    /// </summary>
    /// <param name="options">Discord options containing logging preferences.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>Configured Serilog logger.</returns>
    public static ILogger CreateLogger(DiscordOptions options, IConfiguration configuration)
    {
        var minimumLevel = options.Verbose ? LogEventLevel.Debug : LogEventLevel.Information;

        var loggerConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "DiscordArchitect")
            .Enrich.WithProperty("Version", "1.0.0")
            .WithDiscordArchitectLevels(options);

        // Configure console output
        if (options.JsonOutput)
        {
            // JSON output for structured logging
            loggerConfig.WriteTo.Console(
                new JsonFormatter(renderMessage: true),
                restrictedToMinimumLevel: minimumLevel);
        }
        else
        {
            loggerConfig.WriteTo.Console(
                outputTemplate: HumanConsoleTemplate,
                restrictedToMinimumLevel: minimumLevel);
        }

        // Add file logging
        loggerConfig.WriteTo.File(
            path: "logs/discord-architect-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: options.JsonOutput
                ? "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                : HumanFileTemplate,
            restrictedToMinimumLevel: LogEventLevel.Information);

        return loggerConfig.CreateLogger();
    }
}
