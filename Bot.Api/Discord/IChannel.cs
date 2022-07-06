using System.Collections.Generic;
using System.Linq;
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
		Task RestrictOverwriteToMembersAsync(IReadOnlyCollection<IMember> memberPool, Permissions permission, IEnumerable<IMember> allowedMembers);

		Task DeleteAsync(string? reason = null);
    }

	public static class IChannelExtensions
    {
		public static Task RestrictOverwriteToMembersAsync(this IChannel @this, IReadOnlyCollection<IMember> memberPool, IBaseChannel.Permissions permission, IMember allowedMember)
        {
			return @this.RestrictOverwriteToMembersAsync(memberPool, permission, new[] { allowedMember });
		}

		public static Task RemoveOverwriteFromMembersAsync(this IChannel @this, IReadOnlyCollection<IMember> memberPool, IBaseChannel.Permissions permission)
		{
			return @this.RestrictOverwriteToMembersAsync(memberPool, permission, Enumerable.Empty<IMember>());
		}
	}
}
