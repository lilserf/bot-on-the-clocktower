using Bot.Api;
using System.Collections.Generic;
using System.Linq;

namespace Bot.Core
{
    class Game : IGame
	{
		public ITown Town { get; }

		public IReadOnlyCollection<IMember> StoryTellers => m_storyTellers;

		private readonly List<IMember> m_storyTellers = new();

		public IReadOnlyCollection<IMember> Villagers => m_villagers;

		private readonly List<IMember> m_villagers = new();

		public IReadOnlyCollection<IMember> AllPlayers => StoryTellers.Concat(Villagers).ToList();

		public Game(ITown town)
		{
			Town = town;
		}

		public void AddVillager(IMember villager) => m_villagers.Add(villager);
		public void RemoveVillager(IMember villager) => m_villagers.Remove(villager);
		public void AddStoryTeller(IMember storyTeller) => m_storyTellers.Add(storyTeller);
		public void RemoveStoryTeller(IMember storyTeller) => m_storyTellers.Remove(storyTeller);

	}
}
