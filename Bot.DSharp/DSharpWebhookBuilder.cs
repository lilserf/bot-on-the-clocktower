using Bot.Api;
using DSharpPlus.Entities;
using System;

namespace Bot.DSharp
{
    public class DSharpWebhookBuilder : IBotWebhookBuilder
    {
        public DiscordWebhookBuilder Wrapped => m_wrapped;
        private readonly DiscordWebhookBuilder m_wrapped;

        public DSharpWebhookBuilder(DiscordWebhookBuilder wrapped)
        {
            m_wrapped = wrapped;
        }

        public IBotWebhookBuilder WithContent(string content)
        {
            var w2 = m_wrapped.WithContent(content);
            if (w2 != m_wrapped) throw new ApplicationException("Unexpected chained call did not return itself");
            return this;
        }
    }
}
