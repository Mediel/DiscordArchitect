using Discord;
using Discord.WebSocket;

namespace DiscordArchitect.Services;

/// <summary>
/// Provides methods for managing permission overwrites on Discord category channels, including granting access to the
/// bot, toggling access for the @everyone role, and assigning permissions to specific roles.
/// </summary>
/// <remarks>This class is intended for use with Discord.NET entities to automate permission management within
/// category channels. All methods are asynchronous and modify channel permission overwrites directly. Instances of this
/// class are not intended to be inherited.</remarks>
public sealed class PermissionPlanner
{
    /// <summary>
    /// Ensures that the specified user has the necessary permissions to access and manage the given category channel.
    /// </summary>
    /// <remarks>This method grants the user permission to view, manage, and send messages in the specified
    /// category channel. Existing permission overwrites for the user on the category may be replaced.</remarks>
    /// <param name="category">The category channel to which permissions will be applied. Cannot be null.</param>
    /// <param name="me">The user for whom access permissions will be granted. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task EnsureBotAccessAsync(ICategoryChannel category, SocketGuildUser me)
    {
        var allow = new OverwritePermissions(
            viewChannel: PermValue.Allow,
            manageChannel: PermValue.Allow,
            sendMessages: PermValue.Allow
        );
        await category.AddPermissionOverwriteAsync(me, allow);
    }

    /// <summary>
    /// Applies or removes the 'Everyone' role's access to the specified category channel in the given server.
    /// </summary>
    /// <remarks>If everyoneAccess is set to false, the method denies the 'View Channel' permission for the
    /// 'Everyone' role on the specified category channel. If everyoneAccess is true, no changes are made.</remarks>
    /// <param name="category">The category channel to which the permission overwrite will be applied.</param>
    /// <param name="server">The server that contains the category channel and the 'Everyone' role.</param>
    /// <param name="everyoneAccess">true to allow the 'Everyone' role to view the category channel; false to deny access.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ApplyEveryoneToggleAsync(ICategoryChannel category, SocketGuild server, bool everyoneAccess)
    {
        if (!everyoneAccess)
        {
            var denyEveryone = new OverwritePermissions(viewChannel: PermValue.Deny);
            await category.AddPermissionOverwriteAsync(server.EveryoneRole, denyEveryone);
        }
    }

    /// <summary>
    /// Grants the specified role permission to view and send messages in the given category channel asynchronously.
    /// </summary>
    /// <remarks>This method updates the permission overwrites for the specified role on the given category
    /// channel, allowing members with the role to view the channel and send messages. Existing permissions for the role
    /// on the category may be overwritten.</remarks>
    /// <param name="category">The category channel to which the role permissions will be applied. Cannot be null.</param>
    /// <param name="role">The role to grant view and send message permissions within the category channel. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task GrantCategoryRoleAsync(ICategoryChannel category, IRole role)
    {
        var allowRole = new OverwritePermissions(
            viewChannel: PermValue.Allow,
            sendMessages: PermValue.Allow
        );
        await category.AddPermissionOverwriteAsync(role, allowRole);
    }
}
