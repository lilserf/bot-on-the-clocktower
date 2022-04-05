using Bot.Api.Database;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Bot.Database
{
    [BsonIgnoreExtraElements]
    public class MongoLookupRoleRecord : ILookupRoleRecord
    {
        [BsonElement("server")]
        public ulong GuildId { get; set; }

        [BsonElement("urls")]
        public List<string> Urls { get; set; } = new();
    }
}
