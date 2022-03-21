using Bot.Api;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Bot.Api.IBaseChannel;

namespace Bot.DSharp
{
    public class DSharpChannel : DiscordWrapper<DiscordChannel>, IChannel
	{
		public DSharpChannel(DiscordChannel wrapped)
			: base(wrapped)
		{}

		public ulong Id => Wrapped.Id;

		public IReadOnlyCollection<IMember> Users => Wrapped.Users.Select(x => new DSharpMember(x)).ToList();

		public int Position => Wrapped.Position;

		public bool IsVoice => Wrapped.Type == DSharpPlus.ChannelType.Voice;

		public string Name => Wrapped.Name;

        public async Task AddOverwriteAsync(IMember m, Permissions allow, Permissions deny = Permissions.None)
        {
			if (m is DSharpMember member)
			{
				await Wrapped.AddOverwriteAsync(member.Wrapped, (DSharpPlus.Permissions)allow, (DSharpPlus.Permissions)deny);
			}
        }

		public async Task AddOverwriteAsync(IRole m, IChannel.Permissions allow, Permissions deny = Permissions.None)
		{
			if (m is DSharpMember member)
			{
				await Wrapped.AddOverwriteAsync(member.Wrapped, (DSharpPlus.Permissions)allow, (DSharpPlus.Permissions)deny);
			}
		}


		public async Task RemoveOverwriteAsync(IMember m)
        {
			if (m is DSharpMember member)
            {
				await Wrapped.DeleteOverwriteAsync(member.Wrapped);
            }
        }

		public async Task RemoveOverwriteAsync(IRole m)
		{
			if (m is DSharpMember member)
			{
				await Wrapped.DeleteOverwriteAsync(member.Wrapped);
			}
		}

		public async Task SendMessageAsync(string msg) => await Wrapped.SendMessageAsync(msg);
	}
}
