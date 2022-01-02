using Bot.Api;
using DSharpPlus.Entities;

namespace Bot.DSharp
{
    public class DSharpMessage : IMessage
    {
        private readonly DiscordMessage m_wrapped;
        public DSharpMessage(DiscordMessage wrapped)
        {
            m_wrapped = wrapped;
        }
    }
}