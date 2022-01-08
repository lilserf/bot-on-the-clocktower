using Bot.Api;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Bot.Core
{
    public class ActiveGameService : IActiveGameService
    {
        private readonly Dictionary<TownKey, IGame> m_games = new();

        public bool RegisterGame(ITown town, IGame game)
        {
            var key = TownKey.FromTown(town);
            if (!m_games.ContainsKey(key))
            {
                m_games.Add(key, game);
                return true;
            }
            return false;
        }

        public bool EndGame(ITown town)
        {
            var key = TownKey.FromTown(town);
            if(m_games.ContainsKey(key))
            {
                m_games.Remove(key);
                return true;
            }
            return false;
        }

        public bool TryGetGame(TownKey townKey, [MaybeNullWhen(false)] out IGame game)
        {
            return m_games.TryGetValue(townKey, out game);
        }
    }
}
