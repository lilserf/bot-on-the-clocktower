using Bot.Api;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Bot.Api.IBaseChannel;

namespace Bot.DSharp
{
    public class DSharpChannelCategory : DiscordWrapper<DiscordChannel>, IChannelCategory
	{
		public DSharpChannelCategory(DiscordChannel wrapped)
			: base(wrapped)
		{}

		public ulong Id => Wrapped.Id;

		public IReadOnlyCollection<IChannel> Channels => Wrapped.Children.Select(x => new DSharpChannel(x)).ToList();

		public string Name => Wrapped.Name;

		public Task ClearOverwrites()
		{
			var deletes = Wrapped.PermissionOverwrites.Select(DeletePermissionOverwriteAsync);
			return Task.WhenAll(deletes);
		}

		private async Task DeletePermissionOverwriteAsync(DiscordOverwrite overwrite)
		{
			try
			{
				await ExceptionWrap.WrapExceptionsAsync(() => overwrite.DeleteAsync());
			}
			catch (Bot.Api.UnauthorizedException)
			{ }
		}

		public async Task AddOverwriteAsync(IMember m, Permissions allow, Permissions deny = Permissions.None)
		{
			if (m is DSharpMember member)
			{
				await Wrapped.AddOverwriteAsync(member.Wrapped, (DSharpPlus.Permissions)allow, (DSharpPlus.Permissions)deny);
			}
		}

		public async Task AddOverwriteAsync(IRole r, IChannel.Permissions allow, Permissions deny = Permissions.None)
		{
			if (r is DSharpRole role)
			{
				await Wrapped.AddOverwriteAsync(role.Wrapped, (DSharpPlus.Permissions)allow, (DSharpPlus.Permissions)deny);
			}
		}

        public async Task DeleteAsync(string? reason = null)
        {
			await Wrapped.DeleteAsync(reason);
        }

        public IChannel? GetChannelByName(string name)
        {
            var chan = Channels.FirstOrDefault(x => x.Name == name);
			if(chan != null)
            {
				return chan;
            }
			return null;
        }

        public async Task RemoveOverwriteAsync(IMember m)
		{
			if (m is DSharpMember member)
			{
				await Wrapped.DeleteOverwriteAsync(member.Wrapped);
			}
		}

		public async Task RemoveOverwriteAsync(IRole r)
		{
			if (r is DSharpRole role)
			{
				await Wrapped.DeleteOverwriteAsync(role.Wrapped);
			}
		}
	}
}
