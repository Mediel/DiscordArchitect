using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace DiscordArchitect.DiscordFactories;

/// <summary>
/// Provides factory methods for creating configured instances of DiscordSocketClient.
/// </summary>
/// <remarks>This class simplifies the creation of DiscordSocketClient instances with predefined configuration and
/// logging integration. All members are static and thread-safe.</remarks>
public static class DiscordClientFactory
{
    /// <summary>
    /// Creates a new instance of the DiscordSocketClient configured with basic guild intents and logging.
    /// </summary>
    /// <remarks>The returned client is configured with GatewayIntents.Guilds and will forward log messages to
    /// the provided logger at the Information level. Additional configuration may be required before connecting to
    /// Discord.</remarks>
    /// <param name="logger">The logger used to record informational messages from the Discord client. Cannot be null.</param>
    /// <returns>A new DiscordSocketClient instance with logging integrated using the specified logger.</returns>
    public static DiscordSocketClient Create(ILogger logger)
    {
        var client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds
        });
        client.Log += msg =>
        {
            logger.LogInformation("{DiscordLog}", msg.ToString());
            return Task.CompletedTask;
        };
        return client;
    }
}
