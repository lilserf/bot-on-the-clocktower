using Bot.Api;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Bot.DSharp.DiscordWrappers
{
    public class DSharpChannelCategory : DiscordWrapper<DiscordChannel>, IChannelCategory
	{
		private readonly IReadOnlyCollection<IChannel> mChildren;

		public DSharpChannelCategory(DiscordChannel wrapped, IEnumerable<DiscordChannel> childChannels)
			: base(wrapped)
		{
			mChildren = childChannels.Select(c => new DSharpChannel(c)).ToList();
		}

		public ulong Id => Wrapped.Id;

		public IReadOnlyCollection<IChannel> Channels => mChildren;

		public string Name => Wrapped.Name;
	}
}
