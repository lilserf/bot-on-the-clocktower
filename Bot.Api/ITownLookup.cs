using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface ITownLookup
	{
		// Get a Town given its guild and channel IDs
		public Task<ITownRecord?> GetTownRecord(ulong guildId, ulong channelId);

		// Get all Towns present on this guild
		public Task<IEnumerable<ITownRecord>> GetTownRecords(ulong guildId);
	}
}
