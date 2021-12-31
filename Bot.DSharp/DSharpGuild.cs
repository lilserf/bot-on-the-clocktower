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
		private DiscordGuild m_wrapped;
		public DSharpGuild(DiscordGuild wrapped)
		{
			m_wrapped = wrapped;
		}

		public ulong Id => m_wrapped.Id;
	}
}
