using Bot.Api.Database;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Database
{
    [BsonIgnoreExtraElements]
    internal class MongoGameMetricRecord : IGameMetricRecord
    {
        public int TownHash { get; set; } = 0;

        public DateTime FirstActivity { get; set; }

        public DateTime LastActivity { get; set; }

        public bool Complete { get; set; } = false;

        public int Days { get; set; } = 0;

        public int Nights { get; set; } = 0;

        public int Votes { get; set; } = 0;
    }
}
