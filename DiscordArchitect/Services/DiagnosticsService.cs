using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace DiscordArchitect.Services;

/// <summary>
/// Provides diagnostic utilities for inspecting and logging guild permissions and role hierarchy information.
/// </summary>
/// <remarks>This service is intended for use in debugging and support scenarios where detailed information about
/// the bot's permissions and role stack within a Discord guild is required. It logs relevant details using the provided
/// logger. This class is not intended for general application logic and should be used with care to avoid exposing
/// sensitive information in production environments.</remarks>
public sealed class DiagnosticsService
{
    private readonly ILogger<DiagnosticsService> _log;

    public DiagnosticsService(ILogger<DiagnosticsService> log) => _log = log;

    /// <summary>
    /// Logs diagnostic information about the current user's permissions and role hierarchy within the specified guild.
    /// </summary>
    /// <remarks>This method outputs information to the application's log, including the current user's key
    /// permissions and a list of all roles in the guild ordered from highest to lowest. If the current user only has
    /// managed roles, a warning is logged to indicate that additional configuration may be required for proper
    /// permission management.</remarks>
    /// <param name="server">The guild whose permissions and role stack will be inspected and logged. Cannot be null.</param>
    public void PrintGuildPermsAndRoleStack(SocketGuild server)
    {
        var me = server.CurrentUser;
        _log.LogInformation("ℹ️  [DIAG] Admin:{Admin} ManageRoles:{ManageRoles} ManageChannels:{ManageChannels} ManageThreads:{ManageThreads}",
            me.GuildPermissions.Administrator, me.GuildPermissions.ManageRoles, me.GuildPermissions.ManageChannels, me.GuildPermissions.ManageThreads);

        _log.LogInformation("ℹ️  [DIAG] My roles (top→bottom):");
        foreach (var r in server.Roles.OrderByDescending(r => r.Position))
        {
            var mine = me.Roles.Any(rr => rr.Id == r.Id) ? "*" : " ";
            _log.LogInformation("{Mine} pos={Pos} name={Name} managed={Managed} perms={Perms}", mine, r.Position, r.Name, r.IsManaged, r.Permissions.RawValue);
        }

        if (me.Roles.All(r => r.IsManaged))
            _log.LogWarning("⚠️  The bot only has MANAGED roles. Add a normal role with Manage Roles and place it above others.");
    }
}
