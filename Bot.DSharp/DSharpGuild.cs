using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.DSharp
{
	class DSharpGuild : IGuild
	{
		public DiscordGuild Wrapped { get; }
		public DSharpGuild(DiscordGuild wrapped)
		{
			Wrapped = wrapped;
		}

		public ulong Id => Wrapped.Id;
	}
}
