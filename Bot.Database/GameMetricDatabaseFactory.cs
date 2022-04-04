using Bot.Api.Database;
using MongoDB.Driver;

namespace Bot.Database
{
    public interface IGameMetricDatabaseFactory
    {
        IGameMetricDatabase CreateGameMetricDatabase(IMongoDatabase db);
    }

    public class GameMetricDatabaseFactory : IGameMetricDatabaseFactory
    {
        public IGameMetricDatabase CreateGameMetricDatabase(IMongoDatabase db)
        {
            return new MongoGameMetricDatabase(db);
        }
    }
}
