using Bot.Api;
using DSharpPlus.Entities;

namespace Bot.DSharp
{
    internal class DSharpEmbed : DiscordWrapper<DiscordEmbed>, IEmbed
    {
        public DSharpEmbed(DiscordEmbed wrapped)
            : base(wrapped) { }
    }
}
