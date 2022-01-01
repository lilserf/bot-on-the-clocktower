using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.DSharp
{
	class DSharpRole : IRole
	{
		public DiscordRole Wrapped { get; }

		public DSharpRole(DiscordRole wrapped)
		{
			Wrapped = wrapped;
		}
	}
}
