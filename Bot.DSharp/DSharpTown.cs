using Bot.Api;

namespace Bot.DSharp
{
    public class DSharpTown : ITown
	{
		public ITownRecord TownRecord { get; }
		public IGuild? Guild { get; set; }
		public IChannel? ControlChannel { get; set; }
		public IChannel? TownSquare { get; set; }
		public IChannel? DayCategory { get; set; }
		public IChannel? NightCategory { get; set; }
		public IChannel? ChatChannel { get; set; }
		public IRole? StoryTellerRole { get; set; }
		public IRole? VillagerRole { get; set; }

        public DSharpTown(ITownRecord townRecord)
        {
			TownRecord = townRecord;
        }
	}
}
