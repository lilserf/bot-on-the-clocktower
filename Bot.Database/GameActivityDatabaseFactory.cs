using Bot.Api.Database;
using MongoDB.Driver;

namespace Bot.Database
{
    public interface IGameActivityDatabaseFactory
    {
        IGameActivityDatabase CreateGameActivityDatabase(IMongoDatabase db);
    }

    public class GameActivityDatabaseFactory : IGameActivityDatabaseFactory
    {
        public IGameActivityDatabase CreateGameActivityDatabase(IMongoDatabase db)
        {
            return new GameActivityDatabase(db);
        }
    }
}
