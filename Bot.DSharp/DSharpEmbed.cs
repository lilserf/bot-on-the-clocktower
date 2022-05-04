using Bot.Api;
using DSharpPlus.Entities;

namespace Bot.DSharp
{
    public class DSharpEmbed : DiscordWrapper<DiscordEmbed>, IEmbed
    {
        public DSharpEmbed(DiscordEmbed wrapped)
            : base(wrapped) { }
    }
}
