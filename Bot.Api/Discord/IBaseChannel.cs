using System;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IBaseChannel
    {
		// This unfortunately is just a copy of the relevant portions of the DSharpPlus.Permissions enum :/
		[Flags]
		public enum Permissions : long
		{
			None = 0x00,
			All = 1099511627775,
			AccessChannels = 0x0000000000000400,
			MoveMembers = 0x0000000001000000,
			Stream = 0x0000000000000200,
		}

		public Task AddOverwriteAsync(IMember member, Permissions allow, Permissions deny = Permissions.None);
		public Task AddOverwriteAsync(IRole role, Permissions allow, Permissions deny = Permissions.None);
		public Task RemoveOverwriteAsync(IMember member);
		public Task RemoveOverwriteAsync(IRole role);
	}
}
