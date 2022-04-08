using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bot.Api;
using Bot.Api.Database;
using MongoDB.Driver;

namespace Bot.Database
{
    internal class GameMetricDatabase : IGameMetricDatabase
    {
        public const string CollectionName = "GameMetrics";

        private readonly IMongoCollection<MongoGameMetricRecord> m_collection;

        public GameMetricDatabase(IMongoDatabase db)
        {
            m_collection = db.GetCollection<MongoGameMetricRecord>(CollectionName);
            if (m_collection == null) throw new MissingGameMetricDatabaseException();
        }

        static int TownHash(TownKey townKey)
        {
            return HashCode.Combine(townKey.GuildId, townKey.ControlChannelId);
        }

        private FilterDefinition<MongoGameMetricRecord>? FilterFromKey(TownKey townKey)
        {
            var builder = Builders<MongoGameMetricRecord>.Filter;

            return (builder.Eq(x => x.TownHash, TownHash(townKey)));
        }

        private async Task<MongoGameMetricRecord?> GetExisting(TownKey townKey)
        {
            var builder = Builders<MongoGameMetricRecord>.Filter;

            var filter = builder.Eq(x => x.TownHash, TownHash(townKey))
                & builder.Eq(x => x.Complete, false);

            return await m_collection.Find(filter).FirstOrDefaultAsync();
        }

        private async Task<MongoGameMetricRecord> GetExistingOrNew(TownKey townKey, DateTime timestamp)
        {
            var rec = await GetExisting(townKey);

            if (rec != null)
            {
                rec.LastActivity = timestamp;
                return rec;
            }

            return new MongoGameMetricRecord()
            {
                TownHash = TownHash(townKey),
                FirstActivity = timestamp,
                LastActivity = timestamp,
            };
        }

        public async Task RecordGame(TownKey townKey, DateTime timestamp)
        {
            var existing = await GetExisting(townKey);
            if(existing != null)
            {
                // Close any existing game
                existing.Complete = true;
                var filter = FilterFromKey(townKey);
                await m_collection.ReplaceOneAsync(filter, existing, new ReplaceOptions() { IsUpsert = true });
            }

            var newRec = new MongoGameMetricRecord()
            {
                TownHash = TownHash(townKey),
                FirstActivity = timestamp,
                LastActivity = timestamp,
            };
            await m_collection.InsertOneAsync(newRec);
        }

        public async Task RecordDay(TownKey townKey, DateTime timestamp)
        {
            var record = await GetExistingOrNew(townKey, timestamp);
            record.Days++;

            var filter = FilterFromKey(townKey);
            await m_collection.ReplaceOneAsync(filter, record, new ReplaceOptions() { IsUpsert = true });
        }

        public async Task RecordNight(TownKey townKey, DateTime timestamp)
        {
            var record = await GetExistingOrNew(townKey, timestamp);
            record.Nights++;

            var filter = FilterFromKey(townKey);
            await m_collection.ReplaceOneAsync(filter, record, new ReplaceOptions() { IsUpsert = true });
        }

        public async Task RecordVote(TownKey townKey, DateTime timestamp)
        {
            var record = await GetExistingOrNew(townKey, timestamp);
            record.Votes++;

            var filter = FilterFromKey(townKey);
            await m_collection.ReplaceOneAsync(filter, record, new ReplaceOptions() { IsUpsert = true });
        }

        public async Task RecordEndGame(TownKey townKey, DateTime timestamp)
        {
            var record = await GetExistingOrNew(townKey, timestamp);
            record.Complete = true;

            var filter = FilterFromKey(townKey);
            await m_collection.ReplaceOneAsync(filter, record, new ReplaceOptions() { IsUpsert = true });
        }

        public async Task<DateTime?> GetMostRecentGame(TownKey townKey)
        {
            var filterBuilder = Builders<MongoGameMetricRecord>.Filter;

            var filter = filterBuilder.Eq(x => x.TownHash, TownHash(townKey));

            var sortBuilder = Builders<MongoGameMetricRecord>.Sort;

            var sort = sortBuilder.Descending(x => x.FirstActivity);

            var mostRecent = await m_collection.Find(filter).Sort(sort).FirstOrDefaultAsync();

            return mostRecent?.FirstActivity ?? null;
        }
    }

    class MissingGameMetricDatabaseException : Exception { }
}
