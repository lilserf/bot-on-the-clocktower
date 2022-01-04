using Bot.Api;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.DSharp
{
	class DSharpComponentContext : DiscordWrapper<DiscordInteraction>, IBotComponentContext
	{
		public DSharpComponentContext(DiscordInteraction wrapped)
			: base(wrapped)
		{

		}
		public string CustomId => Wrapped.Data.CustomId;

		public Task DeferInteractionResponse() => Wrapped.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

		public Task EditResponseAsync(IBotWebhookBuilder webhookBuilder)
		{
			if (webhookBuilder is DSharpWebhookBuilder irb)
				return ExceptionWrap.WrapExceptionsAsync(() => Wrapped.EditOriginalResponseAsync(irb.Wrapped));

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
