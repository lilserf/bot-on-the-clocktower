using Bot.Api.Database;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Database
{
    class MongoLookupRoleRecord : ILookupRoleRecord
    {
        public MongoLookupRoleRecord()
        {
            Urls = new List<string>();
        }
        [BsonElement("server")]
        public ulong GuildId { get; set; }

        [BsonElement("urls")]
        public List<string> Urls { get; set; }
    }
}
