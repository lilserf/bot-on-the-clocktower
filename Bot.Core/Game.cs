using Bot.Api;
using System.Collections.Generic;
using System.Linq;

namespace Bot.Core
{
    public class Game : IGame
	{
		public TownKey TownKey { get; }
		public IReadOnlyCollection<IMember> Storytellers => m_storytellers;

		private readonly List<IMember> m_storytellers = new();

		public IReadOnlyCollection<IMember> Villagers => m_villagers;

		private readonly List<IMember> m_villagers = new();

		public IReadOnlyCollection<IMember> AllPlayers => Storytellers.Concat(Villagers).ToList();

		public Game(TownKey townKey)
		{
			TownKey = townKey;
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
