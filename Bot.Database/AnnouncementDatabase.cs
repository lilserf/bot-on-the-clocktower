using Bot.Api.Database;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Database
{
    internal class AnnouncementDatabase : IAnnouncementDatabase
    {
        public const string CollectionName = "GuildVersionAnnouncementsCSharp";

        private readonly IMongoCollection<MongoAnnouncementRecord> m_collection;

        public AnnouncementDatabase(IMongoDatabase db)
        {
            m_collection = db.GetCollection<MongoAnnouncementRecord>(CollectionName);
            if (m_collection == null) throw new MissingAnnouncementDatabaseException();
        }

        private static FilterDefinition<MongoAnnouncementRecord> GetFilter(ulong guildId)
        {
            var builder = Builders<MongoAnnouncementRecord>.Filter;
            return builder.Eq(x => x.GuildId, guildId);
        }

        private Task<MongoAnnouncementRecord> GetRecord(ulong guildId)
        {
            var filter = GetFilter(guildId);

            return m_collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> HasSeenVersion(ulong guildId, Version version)
        {
            var record = await GetRecord(guildId);

            if (record == null) 
                return false;

            return record.Version >= version;
        }

        public async Task RecordGuildHasSeenVersion(ulong guildId, Version version, bool force = false)
        {
            var record = await GetRecord(guildId);

            if (record == null || version > record.Version || force)
            {
                MongoAnnouncementRecord rec = new()
                {
                    GuildId = guildId,
                    Version = version,
                };

                ReplaceOptions options = new()
                {
                    IsUpsert = true
                };

                await m_collection.ReplaceOneAsync(GetFilter(guildId), rec, options);
            }
        }
    }

    class MissingAnnouncementDatabaseException : Exception { }
}
