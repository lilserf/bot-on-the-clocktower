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


        public Task CreateDeferredResponseMessageAsync() => m_wrapped.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        public Task EditResponseAsync(IBotWebhookBuilder webhookBuilder)
        {
            if (webhookBuilder is DSharpWebhookBuilder irb)
                return m_wrapped.EditResponseAsync(irb.Wrapped);
            throw new InvalidOperationException("Passed an incorrect response type");
        }
    }
}
