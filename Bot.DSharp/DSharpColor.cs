using Bot.Api;
using DSharpPlus.Entities;

namespace Bot.DSharp
{
    public class DSharpColor : DiscordWrapper<DiscordColor>, IColor
    {
        public DSharpColor(DiscordColor wrapped)
            : base(wrapped)
        {}
    }
}
