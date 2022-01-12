using Bot.Api;
using Bot.Api.Database;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public async Task<IEnumerable<IGameActivityRecord>> GetAllActivityRecords()
        {
            return await m_collection.Find(new BsonDocument()).ToListAsync();
        }

        public async Task ClearActivity(TownKey townKey)
        {
            var filter = FilterFromKey(townKey);

            await m_collection.DeleteManyAsync(filter);
        }


        public Task RecordActivity(TownKey townKey)
        {
            var filter = FilterFromKey(townKey);

            MongoGameActivityRecord rec = new()
            {
                GuildId = townKey.GuildId,
                ChannelId = townKey.ControlChannelId,
                LastActivity = DateTime.Now,
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
