using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
	public interface ITown
	{
		public long GuildId { get; set; }
		public long ControlChannelId { get; set; }
		public long DayCategoryId { get; set; }
		public long NightCategoryId { get; set; }
		public long ChatChannelId { get; set; }
		public long TownSquareId { get; set; }
		public long StoryTellerRoleId { get; set; }
		public long VillagerRoleId { get; set; }
		public string? AuthorName { get; set; }
		public DateTime Timestamp { get; set; }
	}
}
