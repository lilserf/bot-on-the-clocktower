using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IChannel
	{
		public ulong Id { get; }

		public IReadOnlyCollection<IMember> Users { get; }

		public IReadOnlyCollection<IChannel> Channels { get; }

		public int Position { get; }
		
		public bool IsVoice { get; }

		public string Name { get; }

		public Task SendMessageAsync(string msg);

		public Task AddPermissionsAsync(IMember member);
		public Task RemovePermissionsAsync(IMember member);
	}
}
