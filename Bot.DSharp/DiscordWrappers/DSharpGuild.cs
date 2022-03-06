using Bot.Api;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Bot.DSharp.DiscordWrappers
{
    public class DSharpGuild : DiscordWrapper<DiscordGuild>, IDiscordGuild
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

        public async Task<IChannel> CreateVoiceChannelAsync(string name, IChannel? parent = null)
        {
            DiscordChannel? dc = null;
            if (parent is DSharpChannel dp)
                dc = await Wrapped.CreateChannelAsync(name, DSharpPlus.ChannelType.Voice, dp.Wrapped);
            else
                dc = await Wrapped.CreateChannelAsync(name, DSharpPlus.ChannelType.Voice);

            return new DSharpChannel(dc);
        }

        public async Task<IChannel> CreateTextChannelAsync(string name, IChannel? parent = null)
        {
            DiscordChannel? dc = null;
            if (parent is DSharpChannel dp)
                dc = await Wrapped.CreateChannelAsync(name, DSharpPlus.ChannelType.Text, dp.Wrapped);
            else
                dc = await Wrapped.CreateChannelAsync(name, DSharpPlus.ChannelType.Text);

            return new DSharpChannel(dc);
        }

        public async Task<IChannel> CreateCategoryAsync(string name)
        {
            var c = await Wrapped.CreateChannelCategoryAsync(name);
            return new DSharpChannel(c);
        }

        public async Task<IRole> CreateRoleAsync(string name, Color color)
        {
            var r = await Wrapped.CreateRoleAsync(name, null, new DiscordColor(color.R, color.G, color.B));
            return new DSharpRole(r);
        }
    }
}
