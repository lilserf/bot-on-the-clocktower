using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Api.Database
{
    public interface ITownDatabase
	{
		public Task<ITownRecord?> GetTownRecord(ulong guildId, ulong controlChannelId);

		// Get all Towns present on this guild
		public Task<IEnumerable<ITownRecord>> GetTownRecords(ulong guildId);

		public Task<bool> AddTown(ITown town, IMember author);
	}

	public static class ITownLookupExtensions
    {
		public static Task<ITownRecord?> GetTownRecord(this ITownDatabase @this, TownKey townKey) => @this.GetTownRecord(townKey.GuildId, townKey.ControlChannelId);
    }
}
