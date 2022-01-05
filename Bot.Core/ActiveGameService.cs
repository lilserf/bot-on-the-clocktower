using Bot.Api;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Bot.Core
{
    public class ActiveGameService : IActiveGameService
    {
        struct TownKey
        {
#pragma warning disable IDE0052 // Remove unread private members
            private readonly ulong GuildId;
            private readonly ulong ChannelId;
#pragma warning restore IDE0052 // Remove unread private members
            public TownKey(ulong guildId, ulong channelId)
            {
                GuildId = guildId;
                ChannelId = channelId;
            }
        }

        private static TownKey KeyFromTown(ITown town)
        {
            return new TownKey(town.TownRecord.GuildId, town.TownRecord.ControlChannelId);
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

        public bool EndGame(ITown town)
        {
            var key = KeyFromTown(town);
            if(m_games.ContainsKey(key))
            {
                m_games.Remove(key);
                return true;
            }
            return false;
        }

        public bool TryGetGame(IBotInteractionContext context, [MaybeNullWhen(false)] out IGame game)
        {
            return m_games.TryGetValue(new TownKey(context.Guild.Id, context.Channel.Id), out game);
        }
    }
}
