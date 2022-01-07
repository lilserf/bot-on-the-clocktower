namespace Bot.Api
{
	public interface ITown
	{
		public ITownRecord TownRecord { get; }
		public IGuild? Guild { get; }
		public IChannel? ControlChannel { get; }
		public IChannel? TownSquare { get; }
		public IChannel? DayCategory { get; }
		public IChannel? NightCategory { get; }
		public IChannel? ChatChannel { get; }
		public IRole? StorytellerRole {get; }
		public IRole? VillagerRole {get; }
	}
}
