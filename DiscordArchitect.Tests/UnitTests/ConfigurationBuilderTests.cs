using DiscordArchitect.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using AppCfg = DiscordArchitect.Configuration.ConfigurationBuilder;
using Xunit;

namespace DiscordArchitect.Tests.UnitTests;

public class ConfigurationBuilderTests
{
    private static IConfiguration Config(Dictionary<string, string?> data)
    {
        return new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(data!)
            .Build();
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("", false)]
    public void ParseBoolean_SingleKey_ReflectsTryParseAndTrueOnlyWhenTrue(string value, bool expected)
    {
        var config = Config(new Dictionary<string, string?> { ["X"] = value });

        var result = AppCfg.ParseBoolean(config, "X");

        result.Should().Be(expected);
    }

    [Fact]
    public void ParseBoolean_ReturnsTrueWhenEarlierFalseIsFollowedByTrue()
    {
        var config = Config(new Dictionary<string, string?>
        {
            ["A"] = "false",
            ["B"] = "true",
        });

        AppCfg.ParseBoolean(config, "A", "B").Should().BeTrue();
    }

    [Fact]
    public void ParseBoolean_ReturnsFalseWhenNoKeyMatchesTrue()
    {
        var config = Config(new Dictionary<string, string?>
        {
            ["A"] = "false",
            ["B"] = "false",
        });

        AppCfg.ParseBoolean(config, "A", "B").Should().BeFalse();
    }

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("", "default")]
    public void ParseString_ReturnsFirstNonEmptyOrDefault(string stored, string expected)
    {
        var config = Config(new Dictionary<string, string?> { ["K"] = stored });

        var result = AppCfg.ParseString(config, "default", "K");

        result.Should().Be(expected);
    }

    [Fact]
    public void ParseString_FirstKeyWithValueWins()
    {
        var config = Config(new Dictionary<string, string?>
        {
            ["First"] = "",
            ["Second"] = "pick",
        });

        AppCfg.ParseString(config, "none", "First", "Second").Should().Be("pick");
    }

    [Fact]
    public void ParseUlong_ReturnsParsedOrNull()
    {
        var ok = Config(new Dictionary<string, string?> { ["G"] = "12345" });
        AppCfg.ParseUlong(ok, "G").Should().Be(12345UL);

        var bad = Config(new Dictionary<string, string?> { ["G"] = "nope" });
        AppCfg.ParseUlong(bad, "G").Should().BeNull();
    }

    [Fact]
    public void ParseBooleanWithEnv_WhenEnvTrue_ReturnsTrueWithoutConfig()
    {
        var env = "UNIT_TEST_PARSE_BOOL_ENV_" + Guid.NewGuid().ToString("N");
        var previous = Environment.GetEnvironmentVariable(env);
        try
        {
            Environment.SetEnvironmentVariable(env, "true");
            var empty = Config([]);

            AppCfg.ParseBooleanWithEnv(empty, env, "Missing").Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable(env, previous);
        }
    }

    [Fact]
    public void ParseCommandLineArgs_ExposesFlagSyntax()
    {
        var config = AppCfg.ParseCommandLineArgs(
            ["--TestMode", "true"]);

        AppCfg.ParseBoolean(config, "TestMode").Should().BeTrue();
    }
}
