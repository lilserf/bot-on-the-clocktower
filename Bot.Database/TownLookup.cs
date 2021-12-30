using Bot.Api;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace Bot.Database
{
    public class TownLookup : ITownLookup
	{
		public const string GuildInfoDbName = "GuildInfo";

		private readonly IMongoCollection<MongoGuildInfo> m_guildInfo;

		public TownLookup(IMongoDatabase db)
		{
			m_guildInfo = db.GetCollection<MongoGuildInfo>(GuildInfoDbName);
			if (m_guildInfo == null) throw new MissingGuildInfoDatabaseException();
		}

		public async Task<Town> GetTown(long guildId, long channelId)
		{
			// Build a filter for the specific document we want
			var builder = Builders<MongoGuildInfo>.Filter;
			var filter = builder.Eq(x => x.guild, guildId) & builder.Eq(x => x.controlChannelId, channelId);

			// Get the first match
			var document = await m_guildInfo.Find(filter).FirstOrDefaultAsync();

			// Return a new Town with a subset of this data
			// It'd be great if we could just deserialize the collection directly to a Town, but we can't at the moment
			// This is because Town is in Bot.Api (rightly) and has no knowledge of Mongo, so we can't put Mongo-specific serialization attributes on it
			// Conversely we don't want Town to live in Bot.Database, do we? It feels more core than that
			Town town = new()
			{
				GuildId = document.guild,
				ControlChannelId = document.controlChannelId,
				DayCategoryId = document.dayCategoryId,
				NightCategoryId = document.nightCategoryId,
				ChatChannelId = document.chatChannelId,
				TownSquareId = document.townSquareId,
				StoryTellerRoleId = document.storyTellerRoleId,
				VillagerRoleId = document.villagerRoleId,
				AuthorName = document.authorName,
				Timestamp = document.timestamp
			};
			return town;
		}

		public class MissingGuildInfoDatabaseException : Exception { }
	}
}
