using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IMember
	{
		Task PlaceInAsync(IChannel c);

		Task GrantRoleAsync(IRole role, string? reason=null);

		Task RevokeRoleAsync(IRole role, string? reason=null);

		Task<IMessage> SendMessageAsync(string content);

		public string DisplayName { get; }
		public bool IsBot { get; }
	}
}
