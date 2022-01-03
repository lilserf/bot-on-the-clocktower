using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IMember
	{
		Task MoveToChannelAsync(IChannel c);

		Task GrantRoleAsync(IRole role);

		Task RevokeRoleAsync(IRole role);

		Task<IMessage> SendMessageAsync(string content);

		public string DisplayName { get; }
		public bool IsBot { get; }
	}
}
