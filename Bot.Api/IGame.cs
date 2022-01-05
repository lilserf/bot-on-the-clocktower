using System.Collections.Generic;

namespace Bot.Api
{
    public interface IGame
	{
		ITown Town { get; }

		IReadOnlyCollection<IMember> StoryTellers { get; }

		IReadOnlyCollection<IMember> Villagers { get; }
		
		IReadOnlyCollection<IMember> AllPlayers { get; }

		void AddVillager(IMember villager);
		void RemoveVillager(IMember villager);
		void AddStoryTeller(IMember storyTeller);
		void RemoveStoryTeller(IMember storyTeller);
	}
}
