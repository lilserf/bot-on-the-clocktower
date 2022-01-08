using Bot.Api;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    class DSharpComponentContext : DiscordWrapper<DiscordInteraction>, IBotInteractionContext
	{
		private readonly DSharpGuild m_guild;
		private readonly DSharpChannel m_channel;
		private readonly DSharpMember m_member;

		public DSharpComponentContext(DiscordInteraction wrapped)
			: base(wrapped)
		{
			if (wrapped.User is not DiscordMember dm)
				throw new InvalidOperationException("Passed unexpected User type");

			m_guild = new DSharpGuild(wrapped.Guild);
			m_channel = new DSharpChannel(wrapped.Channel);
			m_member = new DSharpMember(dm);
		}

		public IGuild Guild => m_guild;

		public IChannel Channel => m_channel;

		public IMember Member => m_member;

		public string? ComponentCustomId => Wrapped.Data.CustomId;

        public IEnumerable<string> ComponentValues => Wrapped.Data.Values.ToList();


        // Defer and say we're going to update the original message
        public Task DeferInteractionResponse() => Wrapped.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

		public Task EditResponseAsync(IBotWebhookBuilder webhookBuilder)
		{
			if (webhookBuilder is DSharpWebhookBuilder irb)
				return ExceptionWrap.WrapExceptionsAsync(() => Wrapped.EditOriginalResponseAsync(irb.Wrapped));

			throw new InvalidOperationException("Passed an incorrect builder!");
		}

		public Task UpdateOriginalMessageAsync(IInteractionResponseBuilder builder)
		{
			if (builder is DSharpInteractionResponseBuilder d)
				return ExceptionWrap.WrapExceptionsAsync(() => Wrapped.CreateResponseAsync(InteractionResponseType.UpdateMessage, d.Wrapped));

			throw new InvalidOperationException("Passed an incorrect builder!");
		}

	}
}
