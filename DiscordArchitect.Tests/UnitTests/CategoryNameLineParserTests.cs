using DiscordArchitect.Services;
using FluentAssertions;
using Xunit;

namespace DiscordArchitect.Tests.UnitTests;

public class CategoryNameLineParserTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void TryParse_EmptyOrWhitespace_ReturnsEmpty(string line)
    {
        var outcome = CategoryNameLineParser.TryParse(line, out var trimmed);

        outcome.Should().Be(CategoryNameLineParser.ParseOutcome.Empty);
        trimmed.Should().BeEmpty();
    }

    [Fact]
    public void TryParse_TrimsAndAcceptsMiddleText()
    {
        var outcome = CategoryNameLineParser.TryParse("  My Category  ", out var trimmed);

        outcome.Should().Be(CategoryNameLineParser.ParseOutcome.Ok);
        trimmed.Should().Be("My Category");
    }

    [Fact]
    public void TryParse_ExactlyMaxLength_IsOk()
    {
        var line = new string('a', CategoryNameLineParser.MaxLength);

        var outcome = CategoryNameLineParser.TryParse(line, out var trimmed);

        outcome.Should().Be(CategoryNameLineParser.ParseOutcome.Ok);
        trimmed.Should().HaveLength(CategoryNameLineParser.MaxLength);
    }

    [Fact]
    public void TryParse_OneOverMaxLength_ReturnsTooLong()
    {
        var line = new string('b', CategoryNameLineParser.MaxLength + 1);

        var outcome = CategoryNameLineParser.TryParse(line, out var trimmed);

        outcome.Should().Be(CategoryNameLineParser.ParseOutcome.TooLong);
        trimmed.Should().HaveLength(CategoryNameLineParser.MaxLength + 1);
    }
}
