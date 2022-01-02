using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.DSharp
{
	class DSharpChannel : IChannel
	{
		public DiscordChannel Wrapped { get; }

		public DSharpChannel(DiscordChannel wrapped)
		{
			Wrapped = wrapped;
		}

		public ulong Id => Wrapped.Id;

		public IReadOnlyCollection<IMember> Users => Wrapped.Users.Select(x => new DSharpMember(x)).ToList();

		public IReadOnlyCollection<IChannel> Channels => Wrapped.Children.Select(x => new DSharpChannel(x)).ToList();

		public int Position => Wrapped.Position;

		public bool IsVoice => Wrapped.Type == DSharpPlus.ChannelType.Voice;

		public override bool Equals(object? other)
		{
			if (other is DSharpChannel d)
			{
				return Wrapped.Equals(d?.Wrapped);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Wrapped.GetHashCode();
		}
	}
}
