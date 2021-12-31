using Bot.Api;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace Bot.Database
{
    public class TownLookup : ITownLookup
	{
		public const string GuildInfoDbName = "GuildInfo";

		private readonly IMongoCollection<MongoTown> m_guildInfo;

		public TownLookup(IMongoDatabase db)
		{
			m_guildInfo = db.GetCollection<MongoTown>(GuildInfoDbName);
			if (m_guildInfo == null) throw new MissingGuildInfoDatabaseException();
		}

		public async Task<ITown> GetTown(ulong guildId, ulong channelId)
		{
			// Build a filter for the specific document we want
			var builder = Builders<MongoTown>.Filter;
			var filter = builder.Eq(x => x.GuildId, guildId) & builder.Eq(x => x.ControlChannelId, channelId);

			// Get the first match
			var document = await m_guildInfo.Find(filter).FirstOrDefaultAsync();
			return document;
		}

		public class MissingGuildInfoDatabaseException : Exception { }
	}
}
