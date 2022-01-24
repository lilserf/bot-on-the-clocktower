﻿using Bot.Api;
using Bot.Api.Database;

namespace Bot.Api
{
    public class Town : ITown
	{
		public ITownRecord TownRecord { get; }
		public IGuild? Guild { get; set; }
		public IChannel? ControlChannel { get; set; }
		public IChannel? TownSquare { get; set; }
		public IChannel? DayCategory { get; set; }
		public IChannel? NightCategory { get; set; }
		public IChannel? ChatChannel { get; set; }
		public IRole? StorytellerRole { get; set; }
		public IRole? VillagerRole { get; set; }

        public Town(ITownRecord townRecord)
        {
			TownRecord = townRecord;
        }
	}
}