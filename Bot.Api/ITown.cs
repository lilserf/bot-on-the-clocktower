using Bot.Api.Database;

namespace Bot.Api
{
	public interface ITown
	{
		public ITownRecord? TownRecord { get;}
		public IGuild? Guild { get; }
		public IChannel? ControlChannel { get; }
		public IChannel? TownSquare { get; }
		public IChannelCategory? DayCategory { get; }
		public IChannelCategory? NightCategory { get; set; }
		public IChannel? ChatChannel { get; set; }
		public IRole? StorytellerRole { get; set; }
		public IRole? VillagerRole { get; set; }
	}
}
