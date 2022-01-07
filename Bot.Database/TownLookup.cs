using Bot.Api;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Database
{
    public class TownLookup : ITownLookup
	{
		public const string GuildInfoDbName = "GuildInfo";

		private readonly IMongoCollection<MongoTownRecord> m_guildInfo;

		public TownLookup(IMongoDatabase db)
		{
			m_guildInfo = db.GetCollection<MongoTownRecord>(GuildInfoDbName);
			if (m_guildInfo == null) throw new MissingGuildInfoDatabaseException();
		}

		public async Task<ITownRecord?> GetTownRecord(ulong guildId, ulong channelId)
		{
			// Build a filter for the specific document we want
			var builder = Builders<MongoTownRecord>.Filter;
			var filter = builder.Eq(x => x.GuildId, guildId) & builder.Eq(x => x.ControlChannelId, channelId);

			// Get the first match
			var document = await m_guildInfo.Find(filter).FirstOrDefaultAsync();
			return document;
		}

        public async Task<IEnumerable<ITownRecord>> GetTownRecords(ulong guildId)
        {
			// Build a filter for the specific document we want
			var builder = Builders<MongoTownRecord>.Filter;
			var filter = builder.Eq(x => x.GuildId, guildId);

			return await m_guildInfo.Find(filter).ToListAsync();
		}

		public class MissingGuildInfoDatabaseException : Exception { }
	}
}
