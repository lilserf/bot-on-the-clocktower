using System.Collections.Generic;

namespace Bot.Api
{
    public interface IChannelCategory : IBaseChannel
	{
		public ulong Id { get; }

		public IReadOnlyCollection<IChannel> Channels { get; }

		public string Name { get; }
	}
}
