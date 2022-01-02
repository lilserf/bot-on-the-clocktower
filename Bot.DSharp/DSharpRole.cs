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

        public string Name => Wrapped.Name;

        public DSharpRole(DiscordRole wrapped)
		{
			Wrapped = wrapped;
		}

		public override bool Equals(object? other)
		{
			if (other is DSharpRole d)
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
