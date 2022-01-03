using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IMember
	{
		Task<bool> MoveToChannelAsync(IChannel c, IProcessLogger logger);

		Task<bool> GrantRoleAsync(IRole role, IProcessLogger logger);

		Task<bool> RevokeRoleAsync(IRole role, IProcessLogger logger);

		// TODO: this should probably also take an IProcessLogger
		Task<IMessage> SendMessageAsync(string content);

		public string DisplayName { get; }
		public bool IsBot { get; }
	}
}
