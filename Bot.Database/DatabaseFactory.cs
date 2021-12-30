using Bot.Api;
using Bot.Base;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;

namespace Bot.Database
{
    public class DatabaseFactory
	{
        private readonly IServiceProvider m_serviceProvider;
        private readonly IEnvironment m_environment;
        private readonly IMongoClientFactory m_mongoClientFactory;
        private readonly ITownLookupFactory m_townLookupFactory;

        public const string MongoConnectEnvironmentVar = "MONGO_CONNECT";
        public const string MongoDbEnvironmentVar = "MONGO_DB";

        static DatabaseFactory()
        {
            BsonClassMap.RegisterClassMap<MongoGuildInfo>();
        }

        public DatabaseFactory(IServiceProvider serviceProvider)
        {
            m_serviceProvider = serviceProvider;
            m_environment = serviceProvider.GetService<IEnvironment>();
            m_mongoClientFactory = serviceProvider.GetService<IMongoClientFactory>();
            m_townLookupFactory = serviceProvider.GetService<ITownLookupFactory>();
        }

        // TODO: This connect call should probably call out to a smaller class or two that handle this, via an interface, so we can
        // test this Connect call itself (which is currently not tested). However, at the moment, all the calls from this Connect
        // call ARE tested, so we're pretty covered.
        public IServiceProvider Connect()
        {
            var client = ConnectToMongoClient();
            var database = ConnectToMongoDatabase(client);
            return CreateDatabaseServices(database);
        }

        public IMongoClient ConnectToMongoClient()
        {
            var connectionString = m_environment.GetEnvironmentVariable(MongoConnectEnvironmentVar);
            if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidMongoConnectStringException();

            IMongoClient client = m_mongoClientFactory.CreateClient(connectionString);
            if (client == null) throw new MongoClientNotCreatedException();

            return client;
        }

        public IMongoDatabase ConnectToMongoDatabase(IMongoClient client)
        {
            var db = m_environment.GetEnvironmentVariable(MongoDbEnvironmentVar);
            if (string.IsNullOrWhiteSpace(db)) throw new InvalidMongoDbException();

            IMongoDatabase database = client.GetDatabase(db);
            if (database == null) throw new MongoDbNotFoundException();
            return database;
        }

        public IServiceProvider CreateDatabaseServices(IMongoDatabase mongoDatabase)
        {
            var childSp = new ServiceProvider(m_serviceProvider);
            childSp.AddService(m_townLookupFactory.CreateTownLookup(mongoDatabase));
            return childSp;
        }

        public class InvalidMongoConnectStringException : Exception { }
        public class InvalidMongoDbException : Exception { }
        public class MongoClientNotCreatedException : Exception { }
        public class MongoDbNotFoundException : Exception { }
	}
}
