using Bot.Api.Database;
using MongoDB.Bson.Serialization.Attributes;
using System;

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

        [BsonElement("_id")]
        private ulong RecordId { get; }
    }
}
