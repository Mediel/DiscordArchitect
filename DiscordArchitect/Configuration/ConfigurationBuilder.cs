using Microsoft.Extensions.Configuration;

namespace DiscordArchitect.Configuration;

/// <summary>
/// Provides methods for parsing command-line arguments and configuration values.
/// </summary>
public static class ConfigurationBuilder
{
    /// <summary>
    /// Parses command-line arguments into a configuration object.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Configuration object with parsed values.</returns>
    public static IConfiguration ParseCommandLineArgs(string[] args)
    {
        return new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddCommandLine(args)
            .Build();
    }

    /// <summary>
    /// Parses a boolean value from configuration with multiple possible keys.
    /// </summary>
    /// <param name="config">Configuration object.</param>
    /// <param name="keys">Array of possible keys to check.</param>
    /// <returns>Parsed boolean value or false if none found.</returns>
    public static bool ParseBoolean(IConfiguration config, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (bool.TryParse(config[key], out var result) && result)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Parses a boolean value from configuration with multiple possible keys, including environment variable.
    /// </summary>
    /// <param name="config">Configuration object.</param>
    /// <param name="envVar">Environment variable name.</param>
    /// <param name="keys">Array of possible keys to check.</param>
    /// <returns>Parsed boolean value or false if none found.</returns>
    public static bool ParseBooleanWithEnv(IConfiguration config, string envVar, params string[] keys)
    {
        // Check environment variable first
        if (Environment.GetEnvironmentVariable(envVar) == "true")
        {
            return true;
        }

        // Then check configuration keys
        return ParseBoolean(config, keys);
    }

    /// <summary>
    /// Parses a string value from configuration with multiple possible keys.
    /// </summary>
    /// <param name="config">Configuration object.</param>
    /// <param name="defaultValue">Default value if none found.</param>
    /// <param name="keys">Array of possible keys to check.</param>
    /// <returns>Parsed string value or default if none found.</returns>
    public static string ParseString(IConfiguration config, string defaultValue, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = config[key];
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Parses a ulong value from configuration with multiple possible keys.
    /// </summary>
    /// <param name="config">Configuration object.</param>
    /// <param name="keys">Array of possible keys to check.</param>
    /// <returns>Parsed ulong value or null if none found.</returns>
    public static ulong? ParseUlong(IConfiguration config, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (ulong.TryParse(config[key], out var result))
            {
                return result;
            }
        }
        return null;
    }
}
