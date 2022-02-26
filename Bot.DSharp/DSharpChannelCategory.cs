using Bot.Api;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Bot.DSharp
{
    public class DSharpChannelCategory : DiscordWrapper<DiscordChannel>, IChannelCategory
	{
		public DSharpChannelCategory(DiscordChannel wrapped)
			: base(wrapped)
		{}

		public ulong Id => Wrapped.Id;

		public IReadOnlyCollection<IChannel> Channels => Wrapped.Children.Select(x => new DSharpChannel(x)).ToList();

		public string Name => Wrapped.Name;
	}
}
