using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IChannel : IBaseChannel
	{
		ulong Id { get; }

		IReadOnlyCollection<IMember> Users { get; }

		int Position { get; }
		
		bool IsVoice { get; }
		bool IsText { get; }

		string Name { get; }

		Task<IMessage> SendMessageAsync(string msg);		
		Task<IMessage> SendMessageAsync(IEmbed embed);
		Task<IMessage> SendMessageAsync(IMessageBuilder builder);
		Task RestrictOverwriteToMembersAsync(IReadOnlyCollection<IMember> memberPool, Permissions permission, params IMember[] allowedMembers);

		Task DeleteAsync(string? reason = null);
    }
}
