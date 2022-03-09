using Bot.Api;
using Bot.Api.Database;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.Database
{
    class GameActivityDatabase : IGameActivityDatabase
    {
        public const string CollectionName = "ActiveGames";

        private readonly IMongoCollection<MongoGameActivityRecord> m_collection;

        public GameActivityDatabase(IMongoDatabase db)
        {
            m_collection = db.GetCollection<MongoGameActivityRecord>(CollectionName);
            if (m_collection == null) throw new MissingGameActivityDatabaseException();
        }

        private FilterDefinition<MongoGameActivityRecord>? FilterFromKey(TownKey townKey)
        {
            var builder = Builders<MongoGameActivityRecord>.Filter;
            return (builder.Eq(x => x.GuildId, townKey.GuildId) & builder.Eq(x => x.ChannelId, townKey.ControlChannelId));
        }

        public async Task<IGameActivityRecord> GetActivityRecord(TownKey townKey)
        {
            var filter = FilterFromKey(townKey);

            // Get the first match
            return await m_collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<IGameActivityRecord>> GetAllActivityRecords() => await m_collection.Find(new BsonDocument()).ToListAsync();

        public Task ClearActivityAsync(TownKey townKey) => m_collection.DeleteManyAsync(FilterFromKey(townKey));

        public Task RecordActivityAsync(TownKey townKey, DateTime activityTime)
        {
            var filter = FilterFromKey(townKey);

            MongoGameActivityRecord rec = new()
            {
                GuildId = townKey.GuildId,
                ChannelId = townKey.ControlChannelId,
                LastActivity = activityTime,
            };

            ReplaceOptions options = new()
            {
                IsUpsert = true
            };

            return m_collection.ReplaceOneAsync(filter, rec, options);
        }
    }

    public class MissingGameActivityDatabaseException : Exception { }

}
