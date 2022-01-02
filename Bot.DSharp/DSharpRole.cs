using Bot.Api;
using DSharpPlus.Entities;

namespace Bot.DSharp
{
    class DSharpRole : DiscordWrapper<DiscordRole>, IRole
	{
        public string Name => Wrapped.Name;

        public DSharpRole(DiscordRole wrapped)
			: base(wrapped)
		{}
	}
}
