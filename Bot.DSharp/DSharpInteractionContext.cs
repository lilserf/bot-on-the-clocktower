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
        private readonly DSharpGuild m_guild;
        private readonly DSharpChannel m_channel;
        private readonly DSharpMember m_member;

        public DSharpInteractionContext(InteractionContext wrapped)
        {
            m_wrapped = wrapped;
            m_guild = new DSharpGuild(wrapped.Guild);
            m_channel = new DSharpChannel(wrapped.Channel);
            m_member = new DSharpMember(wrapped.Member);
        }

        public IServiceProvider Services => m_wrapped.Services;
        public IGuild Guild => m_guild;
        public IChannel Channel => m_channel;
        public IMember Member => m_member;

        public Task CreateDeferredResponseMessageAsync() => m_wrapped.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        public Task EditResponseAsync(IBotWebhookBuilder webhookBuilder)
        {
            if (webhookBuilder is DSharpWebhookBuilder irb)
                return ExceptionWrap.WrapExceptionsAsync(() => m_wrapped.EditResponseAsync(irb.Wrapped));

            throw new InvalidOperationException("Passed an incorrect response type");
        }
    }
}
