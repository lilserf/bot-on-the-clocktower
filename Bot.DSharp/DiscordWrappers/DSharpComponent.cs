using Bot.Api;
using DSharpPlus.Entities;

namespace Bot.DSharp.DiscordWrappers
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
