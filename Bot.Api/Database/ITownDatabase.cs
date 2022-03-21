using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Api.Database
{
    public interface ITownDatabase
	{
		public Task<ITownRecord?> GetTownRecordAsync(ulong guildId, ulong controlChannelId);

		// Get all Towns present on this guild
		public Task<IEnumerable<ITownRecord>> GetTownRecordsAsync(ulong guildId);

		public Task<bool> AddTownAsync(ITown town, IMember author);
		public Task<bool> UpdateTownAsync(ITown town);
	}

	public static class ITownLookupExtensions
    {
		public static Task<ITownRecord?> GetTownRecordAsync(this ITownDatabase @this, TownKey townKey) => @this.GetTownRecordAsync(townKey.GuildId, townKey.ControlChannelId);
    }
}
