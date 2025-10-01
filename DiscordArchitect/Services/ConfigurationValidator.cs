using Microsoft.Extensions.Configuration;

namespace DiscordArchitect.Services;

/// <summary>
/// Provides validation for Discord application configuration settings to ensure all required values are present and valid.
/// </summary>
/// <remarks>This validator checks that all necessary configuration values for the Discord bot are properly set,
/// including token, server ID, and other required settings. It provides detailed error messages to help users
/// identify and fix configuration issues before the application attempts to connect to Discord.</remarks>
public sealed class ConfigurationValidator
{
    private readonly IConfiguration _configuration;

    public ConfigurationValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Validates the Discord configuration and returns a result indicating whether the configuration is valid.
    /// </summary>
    /// <returns>A validation result containing the validation status and any error messages.</returns>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        // Validate Discord token
        var token = _configuration["Discord:Token"];
        if (string.IsNullOrWhiteSpace(token))
        {
            errors.Add("Discord:Token is required. Set it using: dotnet user-secrets set \"Discord:Token\" \"YOUR_BOT_TOKEN\"");
        }
        else if (token.Length < 50) // Basic token length validation
        {
            errors.Add("Discord:Token appears to be invalid (too short). Please check your bot token.");
        }

        // Validate Server ID
        var serverIdStr = _configuration["Discord:ServerId"];
        if (string.IsNullOrWhiteSpace(serverIdStr))
        {
            errors.Add("Discord:ServerId is required. Set it using: dotnet user-secrets set \"Discord:ServerId\" \"123456789012345678\"");
        }
        else if (!ulong.TryParse(serverIdStr, out var serverId) || serverId == 0)
        {
            errors.Add("Discord:ServerId must be a valid Discord guild ID (18-digit number).");
        }

        // Validate Source Category Name
        var sourceCategoryName = _configuration["Discord:SourceCategoryName"];
        if (string.IsNullOrWhiteSpace(sourceCategoryName))
        {
            errors.Add("Discord:SourceCategoryName is required. Set it in appsettings.json or as a configuration value.");
        }

        // Validate boolean configurations (warn about potential issues)
        var createRolePerCategory = _configuration["Discord:CreateRolePerCategory"];
        var everyoneAccessToNewCategory = _configuration["Discord:EveryoneAccessToNewCategory"];
        var syncChannelsToCategory = _configuration["Discord:SyncChannelsToCategory"];

        if (!string.IsNullOrEmpty(createRolePerCategory) && !bool.TryParse(createRolePerCategory, out _))
        {
            errors.Add("Discord:CreateRolePerCategory must be 'true' or 'false'.");
        }

        if (!string.IsNullOrEmpty(everyoneAccessToNewCategory) && !bool.TryParse(everyoneAccessToNewCategory, out _))
        {
            errors.Add("Discord:EveryoneAccessToNewCategory must be 'true' or 'false'.");
        }

        if (!string.IsNullOrEmpty(syncChannelsToCategory) && !bool.TryParse(syncChannelsToCategory, out _))
        {
            errors.Add("Discord:SyncChannelsToCategory must be 'true' or 'false'.");
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}

/// <summary>
/// Represents the result of a configuration validation operation.
/// </summary>
/// <param name="IsValid">Indicates whether the configuration is valid.</param>
/// <param name="Errors">A list of validation error messages.</param>
public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    /// <summary>
    /// Gets a formatted error message combining all validation errors.
    /// </summary>
    public string ErrorMessage => string.Join(Environment.NewLine, Errors);
}

