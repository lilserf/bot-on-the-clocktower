using Bot.Api;
using DSharpPlus.Entities;

namespace Bot.DSharp.DiscordWrappers
{
    public class DSharpMessage : DiscordWrapper<DiscordMessage>, IMessage
    {
        public DSharpMessage(DiscordMessage wrapped)
            : base(wrapped)
        {
        }
    }
}