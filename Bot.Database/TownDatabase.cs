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

		private static MongoTownRecord RecordFromTownAndAuthorInfo(ITown town, ulong authorId, string? authorName)
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
				AuthorName = authorName,
				Author = authorId,
				Timestamp = DateTime.Now,
			};
		}

		private static MongoTownRecord RecordFromTown(ITown town, IMember author) => RecordFromTownAndAuthorInfo(town, author.Id, author.DisplayName);

        public async Task<bool> AddTownAsync(ITown town, IMember author)
        {
			var newRec = RecordFromTown(town, author);

			// TODO: error check this record?
			await UpdateRecordAsync(newRec, false);
			return true;
        }

		public async Task<bool> UpdateTownAsync(ITown town)
        {
			if (town.Guild == null || town.ControlChannel == null)
				return false;

            if (await GetTownRecordAsync(town.Guild.Id, town.ControlChannel.Id) is not MongoTownRecord oldRec)
                return false;

            var newRec = RecordFromTownAndAuthorInfo(town, oldRec.Author, oldRec.AuthorName);

			await UpdateRecordAsync(newRec, true);
			return true;
		}

		private Task UpdateRecordAsync(MongoTownRecord record, bool upsert)
        {
			return m_collection.ReplaceOneAsync(GetTownMatchFilter(record.GuildId, record.ControlChannelId), record, new ReplaceOptions() { IsUpsert = upsert });
        }

		public async Task<ITownRecord?> GetTownRecordAsync(ulong guildId, ulong channelId)
        {
            FilterDefinition<MongoTownRecord> filter = GetTownMatchFilter(guildId, channelId);

            // Get the first match
            var document = await m_collection.Find(filter).FirstOrDefaultAsync();
            return document;
        }

        private static FilterDefinition<MongoTownRecord> GetTownMatchFilter(ulong guildId, ulong channelId)
        {
            // Build a filter for the specific document we want
            var builder = Builders<MongoTownRecord>.Filter;
            var filter = builder.Eq(x => x.GuildId, guildId) & builder.Eq(x => x.ControlChannelId, channelId);
            return filter;
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
