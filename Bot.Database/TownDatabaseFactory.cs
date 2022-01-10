using Bot.Api;
using Bot.Api.Database;
using MongoDB.Driver;

namespace Bot.Database
{
    public interface ITownDatabaseFactory
    {
        ITownDatabase CreateTownLookup(IMongoDatabase mongoDb);
    }

    public class TownDatabaseFactory : ITownDatabaseFactory
    {
        public ITownDatabase CreateTownLookup(IMongoDatabase mongoDb) => new TownDatabase(mongoDb);
    }
}
