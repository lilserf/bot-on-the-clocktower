using Bot.Api;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.DSharp.DiscordWrappers
{
    public class DSharpInteractionContext : DiscordWrapper<InteractionContext>, IBotInteractionContext
    {
        private readonly DSharpGuild m_guild;
        private readonly DSharpChannel m_channel;
        private readonly DSharpMember m_member;

        public DSharpInteractionContext(InteractionContext wrapped)
            :base(wrapped)
        {
            m_guild = new DSharpGuild(Wrapped.Guild);
            m_channel = new DSharpChannel(Wrapped.Channel);
            m_member = new DSharpMember(Wrapped.Member);
        }

        public IServiceProvider Services => Wrapped.Services;
        public IGuild Guild => m_guild;
        public IChannel Channel => m_channel;
        public IMember Member => m_member;

        // Non-component interactions have a null ID here
        public string? ComponentCustomId => null;

        public IEnumerable<string> ComponentValues => Enumerable.Empty<string>();

        public Task DeferInteractionResponse() => Wrapped.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        public Task EditResponseAsync(IBotWebhookBuilder webhookBuilder)
        {
            if (webhookBuilder is DSharpWebhookBuilder irb)
                return ExceptionWrap.WrapExceptionsAsync(() => Wrapped.EditResponseAsync(irb.Wrapped));

            throw new InvalidOperationException("Passed an incorrect builder!");
        }

        public Task UpdateOriginalMessageAsync(IInteractionResponseBuilder builder)
		{
            if (builder is DSharpInteractionResponseBuilder d)
                return ExceptionWrap.WrapExceptionsAsync(() => Wrapped.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, d.Wrapped));

            throw new InvalidOperationException("Passed an incorrect builder!");
		}
    }
}
