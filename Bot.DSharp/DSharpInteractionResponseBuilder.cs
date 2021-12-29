using Bot.Api;
using DSharpPlus.Entities;
using System;

namespace Bot.DSharp
{
    public class DSharpInteractionResponseBuilder : IBotInteractionResponseBuilder
    {
        public DiscordInteractionResponseBuilder Wrapped => m_wrapped;
        private readonly DiscordInteractionResponseBuilder m_wrapped;

        public DSharpInteractionResponseBuilder(DiscordInteractionResponseBuilder wrapped)
        {
            m_wrapped = wrapped;
        }

        public IBotInteractionResponseBuilder WithContent(string content)
        {
            var w2 = m_wrapped.WithContent(content);
            if (w2 != m_wrapped) throw new ApplicationException("Unexpected chained call did not return itself");
            return this;
        }
    }
}
