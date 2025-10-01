using Discord;
using Discord.WebSocket;
using DiscordArchitect.Options;
using Microsoft.Extensions.Logging;

namespace DiscordArchitect.Services;

/// <summary>
/// Provides post-run verification functionality to ensure created resources exist and permissions are correctly configured.
/// </summary>
public sealed class VerificationService
{
    private readonly ILogger<VerificationService> _log;

    public VerificationService(ILogger<VerificationService> log)
    {
        _log = log;
    }

    /// <summary>
    /// Verifies that all created resources exist and have correct permissions.
    /// </summary>
    /// <param name="server">The Discord server to verify resources in.</param>
    /// <param name="createdResources">The resources that were created during cloning.</param>
    /// <param name="options">The Discord options used during cloning.</param>
    /// <returns>A verification result containing findings and recommendations.</returns>
    public async Task<VerificationResult> VerifyAsync(SocketGuild server, CreatedResources createdResources, DiscordOptions options)
    {
        _log.LogInformation("🔍 Starting post-run verification...");
        
        var findings = new List<VerificationFinding>();
        var recommendations = new List<string>();

        // 1. Verify category exists and is accessible
        await VerifyCategoryAsync(server, createdResources.CategoryId, findings, recommendations);

        // 2. Verify channels exist and have correct permissions
        await VerifyChannelsAsync(server, createdResources.Channels, findings, recommendations);

        // 3. Verify role exists and has correct permissions (if created)
        if (createdResources.RoleId.HasValue)
        {
            await VerifyRoleAsync(server, createdResources.RoleId.Value, createdResources.CategoryId, findings, recommendations);
        }

        // 4. Verify permission inheritance and visibility
        await VerifyPermissionInheritanceAsync(server, createdResources, options, findings, recommendations);

        // 5. Generate summary and recommendations
        var summary = GenerateSummary(findings, recommendations);

        return new VerificationResult(findings, recommendations, summary);
    }

    private Task VerifyCategoryAsync(SocketGuild server, ulong? categoryId, List<VerificationFinding> findings, List<string> recommendations)
    {
        if (!categoryId.HasValue)
        {
            findings.Add(new VerificationFinding(
                VerificationType.Error,
                "Category",
                "Category ID is null",
                "The category was not created successfully."
            ));
            return Task.CompletedTask;
        }

        var category = server.GetCategoryChannel(categoryId.Value);
        if (category == null)
        {
            findings.Add(new VerificationFinding(
                VerificationType.Error,
                "Category",
                $"Category with ID {categoryId.Value} not found",
                "The category may have been deleted or the ID is incorrect."
            ));
            return Task.CompletedTask;
        }

        // Check if category is visible to @everyone
        var everyoneRole = server.EveryoneRole;
        var everyoneOverwrite = category.GetPermissionOverwrite(everyoneRole);
        
        if (everyoneOverwrite?.ViewChannel == PermValue.Deny)
        {
            findings.Add(new VerificationFinding(
                VerificationType.Warning,
                "Category",
                $"Category '{category.Name}' is hidden from @everyone",
                "The category may not be visible to regular users."
            ));
            recommendations.Add("Consider allowing @everyone to view the category if it should be public.");
        }
        else
        {
            findings.Add(new VerificationFinding(
                VerificationType.Success,
                "Category",
                $"Category '{category.Name}' exists and is accessible",
                "The category is properly configured."
            ));
        }
        
        return Task.CompletedTask;
    }

    private Task VerifyChannelsAsync(SocketGuild server, IReadOnlyList<ulong> channelIds, List<VerificationFinding> findings, List<string> recommendations)
    {
        if (!channelIds.Any())
        {
            findings.Add(new VerificationFinding(
                VerificationType.Warning,
                "Channels",
                "No channels were created",
                "The cloning process may not have created any channels."
            ));
            return Task.CompletedTask;
        }

        var verifiedChannels = 0;
        var hiddenChannels = 0;

        foreach (var channelId in channelIds)
        {
            var channel = server.GetChannel(channelId);
            if (channel == null)
            {
                findings.Add(new VerificationFinding(
                    VerificationType.Error,
                    "Channel",
                    $"Channel with ID {channelId} not found",
                    "The channel may have been deleted or the ID is incorrect."
                ));
                continue;
            }

            // Check channel visibility
            var everyoneRole = server.EveryoneRole;
            var everyoneOverwrite = channel.GetPermissionOverwrite(everyoneRole);
            
            if (everyoneOverwrite?.ViewChannel == PermValue.Deny)
            {
                hiddenChannels++;
                findings.Add(new VerificationFinding(
                    VerificationType.Warning,
                    "Channel",
                    $"Channel '{channel.Name}' is hidden from @everyone",
                    "The channel may not be visible to regular users."
                ));
            }
            else
            {
                verifiedChannels++;
                findings.Add(new VerificationFinding(
                    VerificationType.Success,
                    "Channel",
                    $"Channel '{channel.Name}' exists and is accessible",
                    "The channel is properly configured."
                ));
            }
        }

        if (hiddenChannels > 0)
        {
            recommendations.Add($"Consider reviewing permissions for {hiddenChannels} hidden channels.");
        }

        findings.Add(new VerificationFinding(
            VerificationType.Info,
            "Channels",
            $"Verified {verifiedChannels}/{channelIds.Count} channels",
            $"Successfully verified {verifiedChannels} channels, {hiddenChannels} are hidden from @everyone."
        ));
        
        return Task.CompletedTask;
    }

    private Task VerifyRoleAsync(SocketGuild server, ulong roleId, ulong? categoryId, List<VerificationFinding> findings, List<string> recommendations)
    {
        var role = server.GetRole(roleId);
        if (role == null)
        {
            findings.Add(new VerificationFinding(
                VerificationType.Error,
                "Role",
                $"Role with ID {roleId} not found",
                "The role may have been deleted or the ID is incorrect."
            ));
            return Task.CompletedTask;
        }

        findings.Add(new VerificationFinding(
            VerificationType.Success,
            "Role",
            $"Role '{role.Name}' exists",
            "The role was created successfully."
        ));

        // Check if role has permissions on the category
        if (categoryId.HasValue)
        {
            var category = server.GetCategoryChannel(categoryId.Value);
            if (category != null)
            {
                var roleOverwrite = category.GetPermissionOverwrite(role);
                if (roleOverwrite?.ViewChannel == PermValue.Allow)
                {
                    findings.Add(new VerificationFinding(
                        VerificationType.Success,
                        "Role",
                        $"Role '{role.Name}' has view permissions on category",
                        "The role can access the category."
                    ));
                }
                else
                {
                    findings.Add(new VerificationFinding(
                        VerificationType.Warning,
                        "Role",
                        $"Role '{role.Name}' may not have proper permissions on category",
                        "The role may not be able to access the category."
                    ));
                    recommendations.Add($"Ensure role '{role.Name}' has proper permissions on the category.");
                }
            }
        }
        
        return Task.CompletedTask;
    }

    private Task VerifyPermissionInheritanceAsync(SocketGuild server, CreatedResources createdResources, DiscordOptions options, List<VerificationFinding> findings, List<string> recommendations)
    {
        if (!createdResources.CategoryId.HasValue) return Task.CompletedTask;

        var category = server.GetCategoryChannel(createdResources.CategoryId.Value);
        if (category == null) return Task.CompletedTask;

        // Check if channels are synced to category
        if (options.SyncChannelsToCategory)
        {
            var syncedChannels = 0;
            foreach (var channelId in createdResources.Channels)
            {
                var channel = server.GetChannel(channelId);
                if (channel is ICategoryChannel) continue;

                var channelOverwrites = channel?.PermissionOverwrites?.Count ?? 0;
                if (channelOverwrites == 0)
                {
                    syncedChannels++;
                }
            }

            if (syncedChannels == createdResources.Channels.Count)
            {
                findings.Add(new VerificationFinding(
                    VerificationType.Success,
                    "Permissions",
                    "All channels are synced to category",
                    "Channel permissions are properly inherited from the category."
                ));
            }
            else
            {
                findings.Add(new VerificationFinding(
                    VerificationType.Warning,
                    "Permissions",
                    $"Only {syncedChannels}/{createdResources.Channels.Count} channels are synced to category",
                    "Some channels may have custom permission overwrites."
                ));
                recommendations.Add("Review channel permissions to ensure they match the category settings.");
            }
        }

        // Check @everyone access
        if (!options.EveryoneAccessToNewCategory)
        {
            var everyoneRole = server.EveryoneRole;
            var everyoneOverwrite = category.GetPermissionOverwrite(everyoneRole);
            
            if (everyoneOverwrite?.ViewChannel == PermValue.Deny)
            {
                findings.Add(new VerificationFinding(
                    VerificationType.Success,
                    "Permissions",
                    "@everyone access is properly restricted",
                    "The category is hidden from @everyone as configured."
                ));
            }
            else
            {
                findings.Add(new VerificationFinding(
                    VerificationType.Warning,
                    "Permissions",
                    "@everyone may have access to the category",
                    "The category may be visible to @everyone despite configuration."
                ));
                recommendations.Add("Verify that @everyone access is properly restricted if intended.");
            }
        }
        
        return Task.CompletedTask;
    }

    private string GenerateSummary(List<VerificationFinding> findings, List<string> recommendations)
    {
        var errorCount = findings.Count(f => f.Type == VerificationType.Error);
        var warningCount = findings.Count(f => f.Type == VerificationType.Warning);
        var successCount = findings.Count(f => f.Type == VerificationType.Success);
        var infoCount = findings.Count(f => f.Type == VerificationType.Info);

        var summary = $"Verification Summary: {successCount} ✅ Success, {warningCount} ⚠️ Warnings, {errorCount} ❌ Errors, {infoCount} ℹ️ Info";
        
        if (recommendations.Any())
        {
            summary += $"\n\nRecommendations:\n{string.Join("\n", recommendations.Select(r => $"• {r}"))}";
        }

        return summary;
    }
}

/// <summary>
/// Represents the result of a verification operation.
/// </summary>
/// <param name="Findings">List of verification findings.</param>
/// <param name="Recommendations">List of recommendations for improvement.</param>
/// <param name="Summary">Summary of the verification results.</param>
public record VerificationResult(
    IReadOnlyList<VerificationFinding> Findings,
    IReadOnlyList<string> Recommendations,
    string Summary
);

/// <summary>
/// Represents a single verification finding.
/// </summary>
/// <param name="Type">The type of finding (Success, Warning, Error, Info).</param>
/// <param name="Category">The category of the finding (Category, Channel, Role, Permissions).</param>
/// <param name="Message">The finding message.</param>
/// <param name="Description">Detailed description of the finding.</param>
public record VerificationFinding(
    VerificationType Type,
    string Category,
    string Message,
    string Description
);

/// <summary>
/// Represents the type of verification finding.
/// </summary>
public enum VerificationType
{
    Success,
    Warning,
    Error,
    Info
}
