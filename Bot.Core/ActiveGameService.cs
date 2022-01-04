using Bot.Api;
using System.Collections.Generic;

namespace Bot.Core
{
    public class ActiveGameService : IActiveGameService
	{
		struct TownKey
		{
            readonly ulong GuildId;
            readonly ulong ChannelId;
			public TownKey(ulong guildId, ulong channelId)
			{
				GuildId = guildId;
				ChannelId = channelId;
			}
		}

		private static TownKey KeyFromTown(ITown town)
		{
			return new TownKey(town.Guild.Id, town.ControlChannel.Id);
		}

		private readonly Dictionary<TownKey, IGame> m_games = new();

		public bool RegisterGame(ITown town, IGame game)
		{
			var key = KeyFromTown(town);
			if (!m_games.ContainsKey(key))
			{
				m_games.Add(key, game);
				return true;
			}
			return false;
		}

		public bool TryGetGame(IBotInteractionContext context, out IGame? game)
		{
			return m_games.TryGetValue(new TownKey(context.Guild.Id, context.Channel.Id), out game);
		}
	}
}
