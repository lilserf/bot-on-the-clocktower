using Bot.Api;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public class DSharpInteractionContext : IBotInteractionContext
    {
        private readonly InteractionContext m_wrapped;

        public DSharpInteractionContext(InteractionContext wrapped)
        {
            m_wrapped = wrapped;
        }

        public IServiceProvider Services => m_wrapped.Services;

        public Task CreateDeferredResponseMessage(IBotInteractionResponseBuilder response)
        {
            if (response is DSharpInteractionResponseBuilder irb)
                return m_wrapped.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, irb.Wrapped);
            throw new InvalidOperationException("Passed an incorrect response type");
        }
    }
}
