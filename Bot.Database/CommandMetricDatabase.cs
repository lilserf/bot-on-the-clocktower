using Bot.Api.Database;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Database
{
    internal class CommandMetricDatabase : ICommandMetricDatabase
    {
        public const string CollectionName = "CommandMetrics";

        private readonly IMongoCollection<MongoCommandMetricRecord> m_collection;

        public CommandMetricDatabase(IMongoDatabase db)
        {
            m_collection = db.GetCollection<MongoCommandMetricRecord>(CollectionName);
            if (m_collection == null) throw new MissingCommandMetricDatabaseException();
        }

        private FilterDefinition<MongoCommandMetricRecord>? FilterFromTime(DateTime timestamp)
        {
            var builder = Builders<MongoCommandMetricRecord>.Filter;

            return (builder.Lte(x => x.Day, timestamp.Date) & builder.Gt(x => x.Day, timestamp.Date.Subtract(TimeSpan.FromDays(1))));
        }

        private async Task<MongoCommandMetricRecord?> GetExisting(DateTime timestamp)
        {
            return await m_collection.Find(FilterFromTime(timestamp)).FirstOrDefaultAsync();
        }

        private async Task<MongoCommandMetricRecord> GetExistingOrNew(DateTime timestamp)
        {
            var rec = await GetExisting(timestamp);

            if (rec != null)
            {
                return rec;
            }

            return new MongoCommandMetricRecord()
            {
                Day = timestamp.Date,
            };
        }

        public async Task RecordCommand(string command, DateTime timestamp)
        {
            var rec = await GetExistingOrNew(timestamp);

            if(!rec.Commands.ContainsKey(command))
            {
                rec.Commands.Add(command, 0);
            }

            rec.Commands[command]++;
            var filter = FilterFromTime(timestamp);
            await m_collection.ReplaceOneAsync(filter, rec, new ReplaceOptions() { IsUpsert = true });
        }
    }

    class MissingCommandMetricDatabaseException : Exception { }
}
