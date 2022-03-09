using Bot.Api;
using DSharpPlus.Entities;
using System;
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

	}
}
