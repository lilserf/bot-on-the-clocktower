using Bot.Api.Database;
using MongoDB.Driver;

namespace Bot.Database
{
    public interface ICommandMetricDatabaseFactory
    {
        ICommandMetricDatabase CreateCommandMetricDatabase(IMongoDatabase db);
    }

    public class CommandMetricDatabaseFactory : ICommandMetricDatabaseFactory
    {
        public ICommandMetricDatabase CreateCommandMetricDatabase(IMongoDatabase db)
        {
            return new CommandMetricDatabase(db);
        }
    }
}
