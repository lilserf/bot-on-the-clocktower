using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
	class ActiveGameService : IActiveGameService
	{
		struct TownKey
		{
			ulong GuildId;
			ulong ChannelId;
			public TownKey(ulong guildId, ulong channelId)
			{
				GuildId = guildId;
				ChannelId = channelId;
			}
		}
		private TownKey KeyFromTown(ITown town)
		{
			return new TownKey(town.Guild.Id, town.ControlChannel.Id);
		}

		private Dictionary<TownKey, IGame> m_games;

		public ActiveGameService()
		{
			m_games = new();
		}
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
