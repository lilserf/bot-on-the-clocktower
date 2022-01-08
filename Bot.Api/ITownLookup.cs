using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Api
{
    public interface ITownLookup
	{
		// Get all Towns present on this guild
		public Task<IEnumerable<ITownRecord>> GetTownRecords(ulong guildId);
	}
}
