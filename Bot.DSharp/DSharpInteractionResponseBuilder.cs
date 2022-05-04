using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot.DSharp
{
    class DSharpInteractionResponseBuilder : DiscordWrapper<DiscordInteractionResponseBuilder>, IInteractionResponseBuilder
	{
		public DSharpInteractionResponseBuilder(DiscordInteractionResponseBuilder wrapped)
			: base(wrapped)
		{
		}

		public IInteractionResponseBuilder WithContent(string content)
		{
			var w2 = Wrapped.WithContent(content);
			if (w2 != Wrapped) throw new ApplicationException("Unexpected chained call did not return itself");
			return this;
		}

		public IInteractionResponseBuilder AddComponents(params IBotComponent[] components)
		{
			if (!components.All(x => x is DSharpComponent)) throw new InvalidOperationException("Unexpected type of IComponent!");
			var realComponents = components.Cast<DSharpComponent>().Select(x => x.Wrapped);
			var w2 = Wrapped.AddComponents(realComponents);
			if (w2 != Wrapped) throw new ApplicationException("Unexpected chained call did not return itself");
			return this;
		}

        public IInteractionResponseBuilder WithTitle(string title)
        {
			var w2 = Wrapped.WithTitle(title);
			if (w2 != Wrapped) throw new ApplicationException("Unexpected chained call did not return itself");
			return this;
		}

        public IInteractionResponseBuilder WithCustomId(string customId)
        {
			var w2 = Wrapped.WithCustomId(customId);
			if (w2 != Wrapped) throw new ApplicationException("Unexpected chained call did not return itself");
			return this;
		}

        public IInteractionResponseBuilder AddEmbeds(IEnumerable<IEmbed> embeds)
		{
			var typed = embeds.Select(e => e as DSharpEmbed);
			if (!typed.All(e => e != null)) throw new InvalidOperationException("Expected to be passed only embeds of DSharp types");

			var w2 = Wrapped.AddEmbeds(typed.Select(t => t!.Wrapped));
			if (w2 != Wrapped) throw new ApplicationException("Unexpected chained call did not return itself");
			return this;
		}
    }
}
