using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscordArchitect.Services;

/// <summary>
/// Provides methods for managing available forum tags in a Discord channel via the Discord API.
/// </summary>
/// <remarks>This service is intended for use with Discord forum channels that support tag management. Instances
/// of this class are thread-safe for concurrent use if the provided HttpClient instance is thread-safe. For more
/// information about Discord forum tags, refer to the Discord API documentation.</remarks>
public sealed class ForumTagService
{
    private readonly HttpClient _http;

    public ForumTagService(HttpClient http) => _http = http;

    /// <summary>
    /// Updates the set of available tags for the specified channel asynchronously.
    /// </summary>
    /// <remarks>This method sends a PATCH request to the Discord API to update the available tags for the
    /// specified channel. The operation may fail if the channel does not exist, the payload is invalid, or if the
    /// caller lacks sufficient permissions.</remarks>
    /// <param name="channelId">The unique identifier of the channel whose available tags are to be updated.</param>
    /// <param name="tagsPayload">An array containing the tag objects to set as available for the channel. Each object should represent a tag in
    /// the format expected by the Discord API.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the tags were
    /// updated successfully; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> PatchAvailableTagsAsync(ulong channelId, object[] tagsPayload, CancellationToken ct = default)
    {
        var body = new { available_tags = tagsPayload };
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        using var req = new HttpRequestMessage(HttpMethod.Patch, $"https://discord.com/api/v10/channels/{channelId}")
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        var res = await _http.SendAsync(req, ct);
        if (res.IsSuccessStatusCode) return true;

        var text = await SafeReadAsync(res);
        Console.WriteLine($"PATCH tags failed: {res.StatusCode} - {text}");
        return false;
    }

    /// <summary>
    /// Asynchronously reads the response body as a string, returning a fallback value if the content cannot be read.
    /// </summary>
    /// <remarks>If an error occurs while reading the response content, such as if the content stream is
    /// unavailable or an exception is thrown, the method returns the string "<no body>" instead of propagating the
    /// exception.</remarks>
    /// <param name="res">The HTTP response message from which to read the content. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous read operation. The task result contains the response body as a string,
    /// or "<no body>" if the content could not be read.</returns>
    private static async Task<string> SafeReadAsync(HttpResponseMessage res)
    {
        try { return await res.Content.ReadAsStringAsync(); }
        catch { return "<no body>"; }
    }
}
