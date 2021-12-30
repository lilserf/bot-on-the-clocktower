using MongoDB.Driver;

namespace Bot.Database
{
    public interface IMongoClientFactory
    {
        IMongoClient CreateClient(string connectionString);
    }

    public class MongoClientFactory : IMongoClientFactory
    {
        public IMongoClient CreateClient(string connectionString) => new MongoClient(connectionString);
    }
}
