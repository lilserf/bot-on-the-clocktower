using System.Collections.Generic;

namespace Bot.Api
{
    public interface IGame
	{
		ITown Town { get; }

		IReadOnlyCollection<IMember> Storytellers { get; }

		IReadOnlyCollection<IMember> Villagers { get; }
		
		IReadOnlyCollection<IMember> AllPlayers { get; }

		void AddVillager(IMember villager);
		void RemoveVillager(IMember villager);
		void AddStoryteller(IMember storyteller);
		void RemoveStoryteller(IMember storyteller);
	}
}
