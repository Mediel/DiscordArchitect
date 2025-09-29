using System.Collections.Generic;
using System.Linq;
using Discord;

namespace DiscordArchitect.Services.Pure
{
    public static class ChannelOrdering
    {
        public record Chan(int Position, string Kind, IGuildChannel Channel);

        public static IReadOnlyList<Chan> Order(IReadOnlyList<Chan> channels)
            => channels.OrderBy(c => c.Position).ToList();
    }
}
