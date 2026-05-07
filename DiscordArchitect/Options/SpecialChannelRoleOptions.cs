namespace DiscordArchitect.Options;

/// <summary>Maps a cloned channel name (template/source name) to a per-clone role suffix.</summary>
public sealed class SpecialChannelRoleOptions
{
    /// <summary>Exact channel name to match when cloning (same as Discord channel name).</summary>
    public string ChannelName { get; set; } = string.Empty;

    /// <summary>Appended to the new category name with a space, e.g. <c>Testers</c> → <c>MyCategory Testers</c>.</summary>
    public string RoleSuffix { get; set; } = string.Empty;
}
