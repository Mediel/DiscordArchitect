using Discord;

namespace DiscordArchitect.Services;

/// <summary>
/// Provides methods for managing permission overwrites on Discord category channels, including granting access to the
/// bot, toggling access for the @everyone role, and assigning permissions to specific roles.
/// </summary>
/// <remarks>
/// This class is intended for use with Discord.NET entities to automate permission management within
/// category channels. All methods are asynchronous and modify channel permission overwrites directly. Instances of this
/// class are not intended to be inherited.
/// </remarks>
public sealed class PermissionPlanner
{
    /// <summary>
    /// Ensures that the specified user has the necessary permissions to access and manage the given category channel.
    /// </summary>
    /// <remarks>
    /// Grants the user permission to <c>View Channel</c>, <c>Manage Channel</c>, and <c>Send Messages</c> on the
    /// provided category. If an overwrite for the user already exists on the category, it will be replaced.
    /// Use an <see cref="IUser"/> (not <c>SocketGuildUser</c>) so this method is easy to unit-test.
    /// </remarks>
    /// <param name="category">The category channel to which permissions will be applied. Must not be <see langword="null"/>.</param>
    /// <param name="user">The user for whom access permissions will be granted. Must not be <see langword="null"/>.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task EnsureBotAccessAsync(ICategoryChannel category, IUser user)
    {
        var allow = new OverwritePermissions(
            viewChannel: PermValue.Allow,
            manageChannel: PermValue.Allow,
            sendMessages: PermValue.Allow
        );
        return category.AddPermissionOverwriteAsync(user, allow);
    }

    /// <summary>
    /// Applies or removes the <c>@everyone</c> role's access to the specified category channel in the given guild.
    /// </summary>
    /// <remarks>
    /// If <paramref name="everyoneAccess"/> is <see langword="false"/>, the method creates/updates an overwrite on the
    /// category denying <c>View Channel</c> for the guild's <c>@everyone</c> role. If it is <see langword="true"/>,
    /// the method makes no changes (inheritance from the category's parent/role setup is preserved).
    /// </remarks>
    /// <param name="category">The category channel to which the permission overwrite will be applied.</param>
    /// <param name="guild">The guild that contains the category channel and the <c>@everyone</c> role.</param>
    /// <param name="everyoneAccess">
    /// <see langword="true"/> to allow the <c>@everyone</c> role to view the category channel (no explicit deny written);
    /// <see langword="false"/> to explicitly deny access.
    /// </param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ApplyEveryoneToggleAsync(ICategoryChannel category, IGuild guild, bool everyoneAccess)
    {
        if (everyoneAccess)
            return Task.CompletedTask;

        var deny = new OverwritePermissions(viewChannel: PermValue.Deny);
        return category.AddPermissionOverwriteAsync(guild.EveryoneRole, deny);
    }

    /// <summary>
    /// Grants the specified role permission to view and send messages in the given category channel.
    /// </summary>
    /// <remarks>
    /// Updates the permission overwrites for the specified role on the category channel, allowing members with the role
    /// to view the channel and send messages. Existing role overwrites on the category will be replaced.
    /// </remarks>
    /// <param name="category">The category channel to which the role permissions will be applied.</param>
    /// <param name="role">The role to grant permissions to.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task GrantCategoryRoleAsync(ICategoryChannel category, IRole role)
    {
        var allowRole = new OverwritePermissions(
            viewChannel: PermValue.Allow,
            sendMessages: PermValue.Allow
        );
        return category.AddPermissionOverwriteAsync(role, allowRole);
    }
}
