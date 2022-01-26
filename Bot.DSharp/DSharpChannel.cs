using Bot.Api;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public class DSharpChannel : DiscordWrapper<DiscordChannel>, IChannel
	{
		public DSharpChannel(DiscordChannel wrapped)
			: base(wrapped)
		{}

		public ulong Id => Wrapped.Id;

		public IReadOnlyCollection<IMember> Users => Wrapped.Users.Select(x => new DSharpMember(x)).ToList();

		public IReadOnlyCollection<IChannel> Channels => Wrapped.Children.Select(x => new DSharpChannel(x)).ToList();

		public int Position => Wrapped.Position;

		public bool IsVoice => Wrapped.Type == DSharpPlus.ChannelType.Voice;

		public string Name => Wrapped.Name;

        public async Task AddPermissionsAsync(IMember m)
        {
			if (m is DSharpMember member)
			{
				await Wrapped.AddOverwriteAsync(member.Wrapped, DSharpPlus.Permissions.AccessChannels);
			}
        }

		public async Task RemovePermissionsAsync(IMember m)
        {
			if (m is DSharpMember member)
            {
				await Wrapped.DeleteOverwriteAsync(member.Wrapped);
            }
        }

        public async Task SendMessageAsync(string msg) => await Wrapped.SendMessageAsync(msg);
	}
}
