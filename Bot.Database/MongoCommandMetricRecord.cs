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
    internal class MongoCommandMetricRecord : ICommandMetricRecord
    {
        public DateTime Day { get; set; }

        public Dictionary<string, int> Commands { get; set; } = new Dictionary<string, int>();
    }
}
