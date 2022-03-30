using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IGuild
	{
		ulong Id { get; }

		IReadOnlyDictionary<ulong, IRole> Roles { get; }
		IRole? BotRole { get; }
		IRole EveryoneRole { get; }

		IReadOnlyDictionary<ulong, IMember> Members { get; }

		Task<IChannel?> CreateVoiceChannelAsync(string name, IChannelCategory? parent = null);
		Task<IChannel?> CreateTextChannelAsync(string name, IChannelCategory? parent = null);
		Task<IChannelCategory?> CreateCategoryAsync(string name);

		Task<IRole?> CreateRoleAsync(string name, Color color);

        IChannel? GetChannel(ulong id);

		IChannelCategory? GetChannelCategory(ulong id);

		IReadOnlyCollection<IChannel> Channels { get; }
		IReadOnlyCollection<IChannelCategory> ChannelCategories { get; }
    }
}
