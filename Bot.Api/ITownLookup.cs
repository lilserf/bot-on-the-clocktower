using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface ITownLookup
	{
		public Task<ITownRecord?> GetTownRecord(ulong guildId, ulong controlChannelId);

		// Get all Towns present on this guild
		public Task<IEnumerable<ITownRecord>> GetTownRecords(ulong guildId);
	}

	public static class ITownLookupExtensions
    {
		public static Task<ITownRecord?> GetTownRecord(this ITownLookup @this, TownKey townKey) => @this.GetTownRecord(townKey.GuildId, townKey.ControlChannelId);
    }
}
