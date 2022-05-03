using Bot.Api;
using System.Collections.Generic;
using System.Linq;

namespace Bot.Core
{
    public class Game : IGame
	{
		public TownKey TownKey { get; }
		public IReadOnlyCollection<IMember> Storytellers => m_storytellers;

		private readonly HashSet<IMember> m_storytellers;

		public IReadOnlyCollection<IMember> Villagers => m_villagers;

		private readonly HashSet<IMember> m_villagers;

		public IReadOnlyCollection<IMember> AllPlayers => Storytellers.Concat(Villagers).ToList();

		public Game(TownKey townKey, IEnumerable<IMember> storytellers, IEnumerable<IMember> villagers)
		{
			TownKey = townKey;
			m_storytellers = new(storytellers);
			m_villagers = new(villagers);
		}

		public void AddVillager(IMember villager) => m_villagers.Add(villager);
		public void RemoveVillager(IMember villager) => m_villagers.Remove(villager);
		public void AddStoryteller(IMember storyteller) => m_storytellers.Add(storyteller);
		public void RemoveStoryteller(IMember storyteller) => m_storytellers.Remove(storyteller);

        public override string ToString()
        {
			return $"Game in town {TownKey} with Storytellers {string.Join(",", Storytellers)}";
        }

    }
}
