using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace DiscordArchitect.Services;

/// <summary>
/// Provides functionality to clean up Discord resources created during test mode operations.
/// </summary>
/// <remarks>This service is designed to safely remove categories, channels, and roles that were created
/// during test mode, ensuring no residual data remains on the Discord server. It handles cleanup in the
/// correct order to avoid permission issues and provides detailed logging of cleanup operations.</remarks>
public sealed class CleanupService
{
    private readonly ILogger<CleanupService> _log;

    public CleanupService(ILogger<CleanupService> log)
    {
        _log = log;
    }

    /// <summary>
    /// Asynchronously cleans up all resources created during a test mode operation.
    /// </summary>
    /// <param name="server">The Discord server containing the resources to clean up.</param>
    /// <param name="createdResources">The resources that were created and need to be cleaned up.</param>
    /// <returns>A task that represents the asynchronous cleanup operation.</returns>
    public async Task CleanupAsync(SocketGuild server, CreatedResources createdResources)
    {
        _log.LogInformation("🧹 Starting cleanup of test mode resources...");

        try
        {
            // 1. Delete channels first (they must be deleted before the category)
            if (createdResources.Channels.Any())
            {
                _log.LogInformation("🗑️  Deleting {Count} channels...", createdResources.Channels.Count);
                foreach (var channelId in createdResources.Channels)
                {
                    var channel = server.GetChannel(channelId);
                    if (channel != null)
                    {
                        try
                        {
                            await channel.DeleteAsync();
                            _log.LogInformation("   ✅ Deleted channel: {Name} (ID: {Id})", channel.Name, channelId);
                        }
                        catch (Exception ex)
                        {
                            _log.LogWarning("   ⚠️  Failed to delete channel {Id}: {Error}", channelId, ex.Message);
                        }
                    }
                }
            }

            // 2. Delete the category
            if (createdResources.CategoryId.HasValue)
            {
                var category = server.GetCategoryChannel(createdResources.CategoryId.Value);
                if (category != null)
                {
                    try
                    {
                        await category.DeleteAsync();
                        _log.LogInformation("✅ Deleted category: {Name} (ID: {Id})", category.Name, createdResources.CategoryId.Value);
                    }
                    catch (Exception ex)
                    {
                        _log.LogWarning("⚠️  Failed to delete category {Id}: {Error}", createdResources.CategoryId.Value, ex.Message);
                    }
                }
            }

            // 3. Delete the role (if created)
            if (createdResources.RoleId.HasValue)
            {
                var role = server.GetRole(createdResources.RoleId.Value);
                if (role != null)
                {
                    try
                    {
                        await role.DeleteAsync();
                        _log.LogInformation("✅ Deleted role: {Name} (ID: {Id})", role.Name, createdResources.RoleId.Value);
                    }
                    catch (Exception ex)
                    {
                        _log.LogWarning("⚠️  Failed to delete role {Id}: {Error}", createdResources.RoleId.Value, ex.Message);
                    }
                }
            }

            _log.LogInformation("🎉 Cleanup completed successfully!");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "❌ Error during cleanup: {Error}", ex.Message);
            throw;
        }
    }
}

/// <summary>
/// Represents resources created during a test mode operation that need to be cleaned up.
/// </summary>
/// <param name="CategoryId">The ID of the created category, if any.</param>
/// <param name="Channels">The IDs of created channels.</param>
/// <param name="RoleId">The ID of the created role, if any.</param>
public record CreatedResources(ulong? CategoryId, IReadOnlyList<ulong> Channels, ulong? RoleId);
