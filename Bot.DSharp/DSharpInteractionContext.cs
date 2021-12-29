using Bot.Api;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public class DSharpInteractionContext : IBotInteractionContext
    {
        private readonly InteractionContext mWrapped;

        public DSharpInteractionContext(InteractionContext wrapped)
        {
            mWrapped = wrapped;
        }

        public Task CreateDeferredResponseMessage() => mWrapped.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        public Task CreateDeferredResponseMessage(IBotInteractionResponseBuilder response)
        {
            if (response is DSharpInteractionResponseBuilder irb)
                return mWrapped.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, irb.Wrapped);
            throw new InvalidOperationException("Passed an incorrect response type");
        }
    }
}
