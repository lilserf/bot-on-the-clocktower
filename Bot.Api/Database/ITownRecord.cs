using System;

namespace Bot.Api.Database
{
    public interface ITownRecord
	{
		public ulong GuildId { get; }
		public string? ControlChannel { get; }
		public ulong ControlChannelId { get; }
		public string? DayCategory { get; }
		public ulong DayCategoryId { get; }
		public string? NightCategory { get; }
		public ulong NightCategoryId { get; }
		public string? ChatChannel { get; }
		public ulong? ChatChannelId { get; }
		public string? TownSquare { get; }
		public ulong TownSquareId { get; }
		public string? StorytellerRole { get; }
		public ulong StorytellerRoleId { get; }
		public string? VillagerRole { get; }
		public ulong VillagerRoleId { get; }
		public string? AuthorName { get; }
		public DateTime Timestamp { get; }
	}
}
