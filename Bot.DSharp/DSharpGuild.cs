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
		public ulong Id => Wrapped.Id;

		public IReadOnlyDictionary<ulong, IRole> Roles => m_roles;
		private Dictionary<ulong, IRole> m_roles;

		public DSharpGuild(DiscordGuild wrapped)
		{
			Wrapped = wrapped;
			m_roles = new();
			foreach(var (k,v) in wrapped.Roles)
			{
				m_roles[k] = new DSharpRole(v);
			}
		}
		public override bool Equals(object? other)
		{
			if (other is DSharpGuild d)
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
