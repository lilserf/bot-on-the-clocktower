using Bot.Api;
using DSharpPlus.Entities;
using System;

namespace Bot.DSharp
{
    public class DSharpInteractionResponseBuilder : IBotInteractionResponseBuilder
    {
        public DiscordInteractionResponseBuilder Wrapped => mWrapped;
        private readonly DiscordInteractionResponseBuilder mWrapped;

        public DSharpInteractionResponseBuilder(DiscordInteractionResponseBuilder wrapped)
        {
            mWrapped = wrapped;
        }

        public IBotInteractionResponseBuilder WithContent(string content)
        {
            var w2 = mWrapped.WithContent(content);
            if (w2 != mWrapped) throw new ApplicationException("Unexpected chained call did not return itself");
            return this;
        }
    }
}
