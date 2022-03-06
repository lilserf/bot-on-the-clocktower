using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.DSharp.DiscordWrappers
{
    public class DSharpChannelCategory : DiscordWrapper<DiscordChannel>, IDiscordChannel
	{
		public DSharpChannelCategory(DiscordChannel wrapped)
			: base(wrapped)
		{}

		public ulong Id => Wrapped.Id;

		public IReadOnlyCollection<IChannel> Channels => Wrapped.Children.Select(x => new DSharpChannel(x)).ToList();

		public string Name => Wrapped.Name;

		IReadOnlyCollection<IMember> IChannel.Users => throw new NotImplementedException();
		int IChannel.Position => throw new NotImplementedException();
		bool IChannel.IsVoice => throw new NotImplementedException();
		Task IChannel.SendMessageAsync(string msg) => throw new NotImplementedException();
		Task IChannel.AddPermissionsAsync(IMember member) => throw new NotImplementedException();
		Task IChannel.RemovePermissionsAsync(IMember member) => throw new NotImplementedException();
	}
}
