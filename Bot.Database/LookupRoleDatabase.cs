using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bot.Api.Database;
using MongoDB.Driver;

namespace Bot.Database
{
    class LookupRoleDatabase : ILookupRoleDatabase
    {
        private const string CollectionName = "ServerRoleUrls";

        private readonly IMongoCollection<MongoLookupRoleRecord> m_collection;

        public LookupRoleDatabase(IMongoDatabase db)
        {
            m_collection = db.GetCollection<MongoLookupRoleRecord>(CollectionName);
            if (m_collection == null) throw new MissingLookupRoleDatabaseException();
        }

        private async Task<MongoLookupRoleRecord> GetRecordInternal(ulong guildId)
        {
            // Build a filter for the specific document we want
            var builder = Builders<MongoLookupRoleRecord>.Filter;
            var filter = builder.Eq(x => x.GuildId, guildId);
            var document = await m_collection.Find(filter).FirstOrDefaultAsync();
            return document;
        }
        
        private async Task UpdateRecordInternal(MongoLookupRoleRecord rec)
        {
            // Build a filter for the specific document we want
            var builder = Builders<MongoLookupRoleRecord>.Filter;
            var filter = builder.Eq(x => x.GuildId, rec.GuildId);
            ReplaceOptions options = new()
            {
                IsUpsert = true
            };

            await m_collection.ReplaceOneAsync(filter, rec, options);
        }
        public async Task AddScriptUrlAsync(ulong guildId, string url)
        {
            var doc = await GetRecordInternal(guildId);

            if(doc == null)
            {
                doc = new MongoLookupRoleRecord();
                doc.GuildId = guildId;
                doc.Urls = new List<string>();
            }

            doc.Urls.Add(url);
            await UpdateRecordInternal(doc);
        }

        public async Task<IReadOnlyCollection<string>> GetScriptUrlsAsync(ulong guildId)
        {
            var doc = await GetRecordInternal(guildId);
            return doc.Urls;
        }

        public async Task RemoveScriptUrlAsync(ulong guildId, string url)
        {
            var doc = await GetRecordInternal(guildId);
            doc.Urls.Remove(url);
            await UpdateRecordInternal(doc);
        }
        public class MissingLookupRoleDatabaseException : Exception { }

    }
}
