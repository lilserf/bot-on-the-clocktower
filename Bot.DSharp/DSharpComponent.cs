using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.DSharp
{
	class DSharpComponent : DiscordWrapper<DiscordComponent>, IComponent
	{
		public DSharpComponent(DiscordComponent wrapped)
			: base(wrapped)
		{
		}
	}
}
