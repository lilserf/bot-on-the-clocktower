using MongoDB.Bson.Serialization.Attributes;
using System;
using Bot.Api.Database;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Database
{
    [BsonIgnoreExtraElements]
    class MongoAnnouncementRecord : IAnnouncementRecord
    {
        [BsonElement("guild")]
        public ulong GuildId { get; set; }

        [BsonElement("version")]
        public Version Version { get; set; } = new Version();
    }
}
