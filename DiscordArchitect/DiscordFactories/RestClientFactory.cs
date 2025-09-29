using System.Net.Http.Headers;

namespace DiscordArchitect.DiscordFactories;

/// <summary>
/// Provides factory methods for creating preconfigured HTTP clients for REST API communication.
/// </summary>
/// <remarks>This class is intended to simplify the creation of HttpClient instances that are preconfigured for
/// use with APIs requiring bot authentication. All methods are static and the class cannot be instantiated.</remarks>
public static class RestClientFactory
{
    /// <summary>
    /// Creates a new instance of the HttpClient class configured for use with a Discord bot token.
    /// </summary>
    /// <remarks>The returned HttpClient includes the required Authorization header for Discord bot
    /// authentication and a default User-Agent header. Callers are responsible for disposing the HttpClient instance
    /// when it is no longer needed.</remarks>
    /// <param name="botToken">The Discord bot token to use for authentication. Cannot be null or empty.</param>
    /// <returns>A new HttpClient instance with the Authorization and User-Agent headers set for Discord bot API requests.</returns>
    public static HttpClient Create(string botToken)
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", botToken);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("GuildBuilderBot/1.0");
        return http;
    }
}
