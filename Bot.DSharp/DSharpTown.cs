using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.DSharp
{
	class DSharpTown : ITown
	{
		public IGuild Guild { get; set; }
		public IChannel ControlChannel { get; set; }
		public IChannel TownSquare { get; set; }
		public IChannel DayCategory { get; set; }
		public IChannel NightCategory { get; set; }
		public IChannel ChatChannel { get; set; }
		public IRole StoryTellerRole { get; set; }
		public IRole VillagerRole { get; set; }
	}
}
