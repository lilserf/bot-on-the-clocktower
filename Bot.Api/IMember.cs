using System;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IMember
	{
		public Task PlaceInAsync(IChannel c);

		public Task GrantRoleAsync(IRole role, string? reason=null);

		public Task RevokeRoleAsync(IRole role, string? reason=null);

		public string DisplayName { get; }
		public bool IsBot { get; }
	}
}
