using Bot.Api;
using Bot.Api.Database;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Database
{
    class MongoGameActivityRecord : IGameActivityRecord
    {
        [BsonElement("guild")]
        public ulong GuildId { get; set; }
        [BsonElement("channel")]
        public ulong ChannelId { get; set; }
        [BsonElement("lastActivity")]
        public DateTime LastActivity { get; set; }
    }
}
