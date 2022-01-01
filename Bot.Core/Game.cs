using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
	class Game : IGame
	{
		public ITown Town => m_town;
		ITown m_town;

		public IList<IMember> StoryTellers => m_storyTellers;
		List<IMember> m_storyTellers;

		public IList<IMember> Villagers => m_villagers;
		List<IMember> m_villagers;

		public Game(ITown town)
		{
			m_town = town;
			m_storyTellers = new();
			m_villagers = new();
		}

	}
}
