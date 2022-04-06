using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IChannel : IBaseChannel
	{
		public ulong Id { get; }

		public IReadOnlyCollection<IMember> Users { get; }

		public int Position { get; }
		
		public bool IsVoice { get; }
		public bool IsText { get; }

		public string Name { get; }

		public Task<IMessage> SendMessageAsync(string msg);		
		public Task<IMessage> SendMessageAsync(IEmbed embed);
		public Task<IMessage> SendMessageAsync(IMessageBuilder builder);

		public Task DeleteAsync(string? reason = null);
	}
}
