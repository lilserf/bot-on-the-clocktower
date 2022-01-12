using Bot.Api;
using DSharpPlus.Entities;
using System.Collections.Generic;

namespace Bot.DSharp
{
    class DSharpGuild : DiscordWrapper<DiscordGuild>, IGuild
	{
		public ulong Id => Wrapped.Id;

		public IReadOnlyDictionary<ulong, IRole> Roles => m_roles;

        private readonly Dictionary<ulong, IRole> m_roles;

		public IReadOnlyDictionary<ulong, IMember> Members => m_members;
		private readonly Dictionary<ulong, IMember> m_members;

		public DSharpGuild(DiscordGuild wrapped)
			: base(wrapped)
		{
			m_roles = new();
			foreach(var (k,v) in wrapped.Roles)
			{
				m_roles[k] = new DSharpRole(v);
			}

			m_members = new();
			foreach(var (k,v) in wrapped.Members)
            {
				m_members[k] = new DSharpMember(v);
            }
		}
	}
}
