using DiscordArchitect.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DiscordArchitect.Tests.UnitTests;

/// <summary>
/// Tests for <see cref="OptionsBuilder"/>. Saves and restores <c>DISCORD_ARCHITECT_AUTO_CLEANUP</c> around runs.
/// </summary>
public sealed class OptionsBuilderTests : IDisposable
{
    private readonly string? _previousAutoCleanup;

    public OptionsBuilderTests()
    {
        _previousAutoCleanup = Environment.GetEnvironmentVariable("DISCORD_ARCHITECT_AUTO_CLEANUP");
        Environment.SetEnvironmentVariable("DISCORD_ARCHITECT_AUTO_CLEANUP", null);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("DISCORD_ARCHITECT_AUTO_CLEANUP", _previousAutoCleanup);
    }

    private static IConfiguration Config(Dictionary<string, string?> data) =>
        new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(data!)
            .Build();

    [Fact]
    public void BuildEarlyDiscordOptions_ReadsVerboseAndJsonFromConfig()
    {
        var config = Config(new Dictionary<string, string?>
        {
            ["Discord:Verbose"] = "true",
            ["Discord:JsonOutput"] = "true",
        });

        var early = OptionsBuilder.BuildEarlyDiscordOptions(config);

        early.Verbose.Should().BeTrue();
        early.JsonOutput.Should().BeTrue();
    }

    [Fact]
    public void BuildDiscordOptions_ReadsTokenServerAndDefaults()
    {
        var config = Config(new Dictionary<string, string?>
        {
            ["Discord:Token"] = "fake-discord-token-for-testing-only-123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890",
            ["Discord:ServerId"] = "987654321098765432",
            ["Discord:SourceCategoryName"] = "SrcCat",
            ["Discord:TestMode"] = "true",
            ["Discord:Verbose"] = "false",
            ["Discord:JsonOutput"] = "false",
            ["Discord:AutoCleanup"] = "true",
        });

        var options = OptionsBuilder.BuildDiscordOptions(config, verbose: false);

        options.Token.Should().Contain("fake-discord-token");
        options.ServerId.Should().Be(987654321098765432UL);
        options.SourceCategoryName.Should().Be("SrcCat");
        options.TestMode.Should().BeTrue();
        options.AutoCleanup.Should().BeTrue();
        options.Verbose.Should().BeFalse();
        options.JsonOutput.Should().BeFalse();
    }

    [Fact]
    public void BuildDiscordOptions_WithDiscordArchitectAutoCleanupEnv_ForcesTestModeAndAutoCleanup()
    {
        Environment.SetEnvironmentVariable("DISCORD_ARCHITECT_AUTO_CLEANUP", "true");
        try
        {
            var config = Config(new Dictionary<string, string?>
            {
                ["Discord:Token"] = "fake-discord-token-for-testing-only-123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890",
                ["Discord:ServerId"] = "123456789012345678",
            });

            var options = OptionsBuilder.BuildDiscordOptions(config, verbose: false);

            options.TestMode.Should().BeTrue("DISCORD_ARCHITECT_AUTO_CLEANUP=true forces test behaviour");
            options.AutoCleanup.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("DISCORD_ARCHITECT_AUTO_CLEANUP", null);
        }
    }
}
