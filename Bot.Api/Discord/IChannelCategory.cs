using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IChannelCategory : IBaseChannel
	{
		public ulong Id { get; }

		public IReadOnlyCollection<IChannel> Channels { get; }


		public string Name { get; }
		public IChannel? GetChannelByName(string name);
		public Task DeleteAsync(string? reason = null);

	}
}
