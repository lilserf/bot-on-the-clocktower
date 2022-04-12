using Bot.Api;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Bot.Core
{
    public class ActiveGameService : IActiveGameService
    {
        private readonly ConcurrentDictionary<TownKey, IGame> m_games = new();

        public bool RegisterGame(ITown town, IGame game)
        {
            var key = TownKey.FromTown(town);
            return m_games.TryAdd(key, game);
        }

        public bool EndGame(ITown town)
        {
            var key = TownKey.FromTown(town);
            return m_games.TryRemove(key, out _);
        }

        public bool TryGetGame(TownKey townKey, [MaybeNullWhen(false)] out IGame game)
        {
            return m_games.TryGetValue(townKey, out game);
        }
    }
}
