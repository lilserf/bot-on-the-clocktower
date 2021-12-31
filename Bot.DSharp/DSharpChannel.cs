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
		private DiscordChannel m_wrapped;

		public DSharpChannel(DiscordChannel wrapped)
		{
			m_wrapped = wrapped;
		}

		public ulong Id => m_wrapped.Id;
	}
}
