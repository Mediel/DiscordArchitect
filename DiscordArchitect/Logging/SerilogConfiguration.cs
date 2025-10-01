using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Configuration;
using DiscordArchitect.Options;

namespace DiscordArchitect.Logging;

/// <summary>
/// Provides Serilog configuration for structured logging with support for verbose and JSON output modes.
/// </summary>
public static class SerilogConfiguration
{
    /// <summary>
    /// Configures Serilog based on the provided options.
    /// </summary>
    /// <param name="options">Discord options containing logging preferences.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>Configured Serilog logger.</returns>
    public static ILogger CreateLogger(DiscordOptions options, IConfiguration configuration)
    {
        var loggerConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "DiscordArchitect")
            .Enrich.WithProperty("Version", "1.0.0");

        // Set minimum level based on verbose mode
        var minimumLevel = options.Verbose ? LogEventLevel.Debug : LogEventLevel.Information;
        loggerConfig.MinimumLevel.Is(minimumLevel);

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
            // Human-readable output with emojis and colors
            loggerConfig.WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: minimumLevel);
        }

        // Add file logging
        loggerConfig.WriteTo.File(
            path: "logs/discord-architect-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: options.JsonOutput 
                ? "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                : "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            restrictedToMinimumLevel: LogEventLevel.Information);

        return loggerConfig.CreateLogger();
    }

    /// <summary>
    /// Configures Serilog for the host builder.
    /// </summary>
    /// <param name="hostBuilder">The host builder to configure.</param>
    /// <returns>Configured host builder.</returns>
    public static IHostBuilder UseSerilog(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "DiscordArchitect")
                .Enrich.WithProperty("Version", "1.0.0");

            // Set minimum level
            configuration.MinimumLevel.Is(LogEventLevel.Information);

            // Configure console output
            configuration.WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information);

            // Add file logging
            configuration.WriteTo.File(
                path: "logs/discord-architect-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information);
        });
    }
}
