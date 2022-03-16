using Bot.Api;
using Bot.Api.Database;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Database
{
    public class TownDatabase : ITownDatabase
	{
		public const string CollectionName = "GuildInfo";

		private readonly IMongoCollection<MongoTownRecord> m_collection;

		public TownDatabase(IMongoDatabase db)
		{
			m_collection = db.GetCollection<MongoTownRecord>(CollectionName);
			if (m_collection == null) throw new MissingGuildInfoDatabaseException();
		}

		private static MongoTownRecord RecordFromTown(ITown town, IMember author)
        {
			return new MongoTownRecord()
			{
				GuildId = town.Guild?.Id ?? 0,
				ControlChannel = town.ControlChannel?.Name,
				ControlChannelId = town.ControlChannel?.Id ?? 0,
				ChatChannel = town.ChatChannel?.Name,
				ChatChannelId = town.ChatChannel?.Id ?? 0,
				TownSquare = town.TownSquare?.Name,
				TownSquareId = town.TownSquare?.Id ?? 0,
				DayCategory = town.DayCategory?.Name,
				DayCategoryId = town.DayCategory?.Id ?? 0,
				NightCategory = town.NightCategory?.Name,
				NightCategoryId = town.NightCategory?.Id ?? 0,
				StorytellerRole = town.StorytellerRole?.Name,
				StorytellerRoleId = town.StorytellerRole?.Id ?? 0,
				VillagerRole = town.VillagerRole?.Name,
				VillagerRoleId = town.VillagerRole?.Id ?? 0,
				AuthorName = author.DisplayName,
				Author = author.Id,
				Timestamp = DateTime.Now,
			};
		}

        public async Task<bool> AddTownAsync(ITown town, IMember author)
        {
			var newRec = RecordFromTown(town, author);

			// TODO: error check this record?
			await m_collection.InsertOneAsync(newRec);

			return true;
        }

        public async Task<ITownRecord?> GetTownRecordAsync(ulong guildId, ulong channelId)
		{
			// Build a filter for the specific document we want
			var builder = Builders<MongoTownRecord>.Filter;
			var filter = builder.Eq(x => x.GuildId, guildId) & builder.Eq(x => x.ControlChannelId, channelId);

			// Get the first match
			var document = await m_collection.Find(filter).FirstOrDefaultAsync();
			return document;
		}

		public async Task<IEnumerable<ITownRecord>> GetTownRecordsAsync(ulong guildId)
        {
			// Build a filter for the specific document we want
			var builder = Builders<MongoTownRecord>.Filter;
			var filter = builder.Eq(x => x.GuildId, guildId);

			return await m_collection.Find(filter).ToListAsync();
		}

		public class MissingGuildInfoDatabaseException : Exception { }
	}
}
