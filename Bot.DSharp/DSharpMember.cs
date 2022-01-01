using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    class DSharpMember : IMember
	{
		public DiscordMember Wrapped { get; }

		public DSharpMember(DiscordMember wrapped)
		{
			Wrapped = wrapped;
		}

		public Task PlaceInAsync(IChannel c)
		{
			if (c is DSharpChannel chan)
				return Wrapped.PlaceInAsync(chan.Wrapped);

			throw new InvalidOperationException("Passed an incorrect IChannel type");
		}
	}
}
