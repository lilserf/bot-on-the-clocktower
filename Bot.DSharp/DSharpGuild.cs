using Bot.Api;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public class DSharpGuild : DiscordWrapper<DiscordGuild>, IGuild
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

        public async Task<IChannel?> CreateVoiceChannelAsync(string name, IChannelCategory? parent = null)
        {
            DiscordChannel? dc;
            if (parent is DSharpChannel dp)
                dc = await Wrapped.CreateChannelAsync(name, DSharpPlus.ChannelType.Voice, dp.Wrapped);
            else
                dc = await Wrapped.CreateChannelAsync(name, DSharpPlus.ChannelType.Voice);

            return dc != null ? new DSharpChannel(dc) : null;
        }

        public async Task<IChannel?> CreateTextChannelAsync(string name, IChannelCategory? parent = null)
        {
            DiscordChannel? dc;
            if (parent is DSharpChannel dp)
                dc = await Wrapped.CreateChannelAsync(name, DSharpPlus.ChannelType.Text, dp.Wrapped);
            else
                dc = await Wrapped.CreateChannelAsync(name, DSharpPlus.ChannelType.Text);

            return dc != null ? new DSharpChannel(dc) : null;
        }

        public async Task<IChannelCategory?> CreateCategoryAsync(string name)
        {
            var c = await Wrapped.CreateChannelCategoryAsync(name);
            return c != null ? new DSharpChannelCategory(c) : null;
        }

        public async Task<IRole?> CreateRoleAsync(string name, Color color)
        {
            var r = await Wrapped.CreateRoleAsync(name, null, new DiscordColor(color.R, color.G, color.B));
            return r != null ? new DSharpRole(r) : null;
        }

        public IChannel? GetChannel(ulong id)
        {
            var channel = Wrapped.GetChannel(id);
            if (channel != null && (channel.Type == DSharpPlus.ChannelType.Text || channel.Type == DSharpPlus.ChannelType.Voice))
                return new DSharpChannel(channel);
            return null;
        }

        public IChannelCategory? GetChannelCategory(ulong id)
        {
            var channel = Wrapped.GetChannel(id);
            if (channel != null && (channel.Type == DSharpPlus.ChannelType.Category))
                return new DSharpChannelCategory(channel);
            return null;
        }
    }
}
