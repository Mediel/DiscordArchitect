namespace DiscordArchitect.Options;

/// <summary>
/// Represents configuration options for connecting to and managing a Discord server integration.
/// </summary>
/// <remarks>Use this class to specify authentication credentials and behavior for Discord-related features, such
/// as server selection, category management, and channel synchronization. All properties must be set appropriately
/// before initializing Discord functionality.</remarks>
public sealed class DiscordOptions
{
    /// <summary>
    /// Gets or sets the authentication token used to authorize requests.
    /// </summary>
    public string Token { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the unique identifier of the Discord server (guild) to connect to.
    /// </summary>
    public ulong ServerId { get; set; }
    /// <summary>
    /// Gets or sets the name of the source category associated with this instance.
    /// </summary>
    public string SourceCategoryName { get; set; } = "Template";

    /// <summary>
    /// Gets or sets a value indicating whether a separate role is created for each category.
    /// </summary>
    public bool CreateRolePerCategory { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether all users are granted access to newly created categories by default.
    /// </summary>
    public bool EveryoneAccessToNewCategory { get; set; } = false;
    /// <summary>
    /// Gets or sets a value indicating whether channels should be synchronized with their parent category.
    /// </summary>
    public bool SyncChannelsToCategory { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the application should run in test mode, allowing for cleanup of created resources.
    /// </summary>
    public bool TestMode { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether verbose logging should be enabled.
    /// </summary>
    public bool Verbose { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether logs should be output in JSON format.
    /// </summary>
    public bool JsonOutput { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to automatically clean up resources in test mode.
    /// </summary>
    public bool AutoCleanup { get; set; } = false;
}
