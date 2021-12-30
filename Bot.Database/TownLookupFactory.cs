using Bot.Api;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Database
{
    public interface ITownLookupFactory
    {
        ITownLookup CreateTownLookup(IMongoDatabase mongoDb);
    }

    public class TownLookupFactory : ITownLookupFactory
    {
        public ITownLookup CreateTownLookup(IMongoDatabase mongoDb) => new TownLookup(mongoDb);
    }
}
