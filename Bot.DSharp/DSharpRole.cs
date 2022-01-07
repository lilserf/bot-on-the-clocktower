using Bot.Api;
using DSharpPlus.Entities;

namespace Bot.DSharp
{
    class DSharpRole : DiscordWrapper<DiscordRole>, IRole
	{
        public string Name => Wrapped.Name;
        public string Mention => Wrapped.Mention;

        public DSharpRole(DiscordRole wrapped)
			: base(wrapped)
		{}
	}
}
