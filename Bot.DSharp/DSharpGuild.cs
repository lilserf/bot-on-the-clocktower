using Bot.Api;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
            if (parent is DSharpChannelCategory dp)
                dc = await Wrapped.CreateChannelAsync(name, DSharpPlus.ChannelType.Voice, dp.Wrapped);
            else
                dc = await Wrapped.CreateChannelAsync(name, DSharpPlus.ChannelType.Voice);

            return dc != null ? new DSharpChannel(dc) : null;
        }

        public async Task<IChannel?> CreateTextChannelAsync(string name, IChannelCategory? parent = null)
        {
            DiscordChannel? dc;
            if (parent is DSharpChannelCategory dp)
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
            if (channel != null && IsStandardChannel(channel))
                return new DSharpChannel(channel);
            return null;
        }

        public IChannelCategory? GetChannelCategory(ulong id)
        {
            var channel = Wrapped.GetChannel(id);
            if (channel != null && IsChannelCategory(channel))
                return new DSharpChannelCategory(channel);
            return null;
        }

        public IReadOnlyCollection<IChannel> Channels => Wrapped.Channels.Values.Where(IsStandardChannel).Select(c => new DSharpChannel(c)).ToArray();

        public IReadOnlyCollection<IChannelCategory> ChannelCategories => Wrapped.Channels.Values.Where(IsChannelCategory).Select(c => new DSharpChannelCategory(c)).ToArray();

        public IRole? BotRole
        {
            get
            {
                // TODO: how do we get the bot user name instead of magic-stringing this
                var role = Wrapped.Roles.Where(kvp => kvp.Value.IsManaged == true && kvp.Value.Name.Equals("Bot on the Clocktower")).Select(kvp => kvp.Value).FirstOrDefault();
                return role == null ? null : new DSharpRole(role);
            }
        }

        public IRole EveryoneRole => new DSharpRole(Wrapped.EveryoneRole);

        private static bool IsStandardChannel(DiscordChannel channel)
        {
            return channel.Type == DSharpPlus.ChannelType.Text || channel.Type == DSharpPlus.ChannelType.Voice;
        }

        private static bool IsChannelCategory(DiscordChannel channel)
        {
            return channel.Type == DSharpPlus.ChannelType.Category;
        }
    }
}
