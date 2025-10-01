using DiscordArchitect.Options;
using Microsoft.Extensions.Configuration;

namespace DiscordArchitect.Configuration;

/// <summary>
/// Provides methods for building DiscordOptions from configuration.
/// </summary>
public static class OptionsBuilder
{
    /// <summary>
    /// Builds DiscordOptions from configuration with support for command-line arguments and environment variables.
    /// </summary>
    /// <param name="config">Configuration object.</param>
    /// <param name="verbose">Whether verbose logging is enabled.</param>
    /// <returns>Configured DiscordOptions instance.</returns>
    public static DiscordOptions BuildDiscordOptions(IConfiguration config, bool verbose)
    {
        // Parse auto cleanup with environment variable support
        var autoCleanup = ConfigurationBuilder.ParseBooleanWithEnv(
            config, 
            "DISCORD_ARCHITECT_AUTO_CLEANUP",
            "Discord:AutoCleanup",
            "auto-cleanup",
            "--auto-cleanup",
            "AutoCleanup"
        );

        // Parse test mode
        var testMode = ConfigurationBuilder.ParseBoolean(
            config,
            "Discord:TestMode",
            "test-mode",
            "TestMode"
        );

        // FOR TESTING: Force test mode and auto cleanup when running from assistant
        if (Environment.GetEnvironmentVariable("DISCORD_ARCHITECT_AUTO_CLEANUP") == "true")
        {
            testMode = true;
            autoCleanup = true;
        }

        // Debug logging (only in verbose mode)
        if (verbose)
        {
            Console.WriteLine($"🔍 Debug: TestMode={testMode}, AutoCleanup={autoCleanup}");
        }

        // Parse other options
        var token = ConfigurationBuilder.ParseString(config, string.Empty, "Discord:Token");
        var sourceCategoryName = ConfigurationBuilder.ParseString(config, "Template", "Discord:SourceCategoryName");
        var createRolePerCategory = ConfigurationBuilder.ParseBoolean(config, "Discord:CreateRolePerCategory");
        var everyoneAccessToNewCategory = ConfigurationBuilder.ParseBoolean(config, "Discord:EveryoneAccessToNewCategory");
        var syncChannelsToCategory = ConfigurationBuilder.ParseBoolean(config, "Discord:SyncChannelsToCategory");
        var verboseOption = ConfigurationBuilder.ParseBoolean(config, "Discord:Verbose", "verbose", "--verbose");
        var jsonOutput = ConfigurationBuilder.ParseBoolean(config, "Discord:JsonOutput", "json", "--json");
        var serverId = ConfigurationBuilder.ParseUlong(config, "Discord:ServerId");

        return new DiscordOptions
        {
            Token = token,
            ServerId = serverId ?? 0,
            SourceCategoryName = sourceCategoryName,
            CreateRolePerCategory = createRolePerCategory,
            EveryoneAccessToNewCategory = everyoneAccessToNewCategory,
            SyncChannelsToCategory = syncChannelsToCategory,
            TestMode = testMode,
            Verbose = verboseOption,
            JsonOutput = jsonOutput,
            AutoCleanup = autoCleanup
        };
    }

    /// <summary>
    /// Builds early DiscordOptions for Serilog initialization.
    /// </summary>
    /// <param name="config">Configuration object.</param>
    /// <returns>Configured DiscordOptions instance for early logging.</returns>
    public static DiscordOptions BuildEarlyDiscordOptions(IConfiguration config)
    {
        return new DiscordOptions
        {
            Verbose = ConfigurationBuilder.ParseBoolean(config, "Discord:Verbose", "verbose", "--verbose"),
            JsonOutput = ConfigurationBuilder.ParseBoolean(config, "Discord:JsonOutput", "json", "--json")
        };
    }
}
