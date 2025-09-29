using System.Collections.Generic;
using Discord;
using DiscordArchitect.Services.Pure;
using FluentAssertions;
using Moq;
using Xunit;

namespace DiscordArchitect.Tests.UnitTests;

public class ChannelOrderingTests
{
    private static IGuildChannel MockChannel(int position, string name)
    {
        var ch = new Mock<IGuildChannel>(MockBehavior.Strict);
        ch.SetupGet(c => c.Position).Returns(position);
        ch.SetupGet(c => c.Name).Returns(name);
        return ch.Object;
    }

    [Fact]
    public void Order_SortsByPositionAscending()
    {
        var a = MockChannel(10, "a-text");
        var b = MockChannel(15, "b-news");
        var c = MockChannel(20, "c-voice");

        var input = new List<ChannelOrdering.Chan>
        {
            new(20, "voice", c),
            new(10, "text",  a),
            new(15, "news",  b),
        };

        var ordered = ChannelOrdering.Order(input);

        ordered.Should().HaveCount(3);
        ordered[0].Position.Should().Be(10);
        ordered[1].Position.Should().Be(15);
        ordered[2].Position.Should().Be(20);
    }
}
