using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		public async Task PlaceInAsync(IChannel c)
		{
			// Have to upcast to a DSharpChannel
			if (c is DSharpChannel chan)
			{
				await Wrapped.PlaceInAsync(chan.Wrapped);
				return;
			}

			throw new InvalidOperationException("Passed an incorrect IChannel type");
		}
	}
}
