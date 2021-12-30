using Bot.Api;
using MongoDB.Driver;

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
