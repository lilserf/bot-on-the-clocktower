using Bot.Api;
using DSharpPlus.Entities;

namespace Bot.DSharp
{
    class DSharpComponent : DiscordWrapper<DiscordComponent>, IBotComponent
	{
		public DSharpComponent(DiscordComponent wrapped)
			: base(wrapped)
		{
		}

		public string CustomId => Wrapped.CustomId;
	}
}
