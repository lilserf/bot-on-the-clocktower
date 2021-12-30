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

        public IServiceProvider Connect()
        {
            var childSp = new ServiceProvider(m_serviceProvider);

            // Connect to Mongo
			var connectionString = m_environment.GetEnvironmentVariable(MongoConnectEnvironmentVar);
            if(string.IsNullOrWhiteSpace(connectionString)) throw new InvalidMongoConnectStringException();

            var db = m_environment.GetEnvironmentVariable(MongoDbEnvironmentVar);
            if(string.IsNullOrWhiteSpace(db)) throw new InvalidMongoDbException();

            IMongoClient client = m_mongoClientFactory.CreateClient(connectionString);
            if (client == null) throw new MongoClientNotCreatedException();

            IMongoDatabase database = client.GetDatabase(db);
            if (database == null) throw new MongoDbNotFoundException();

            // Initialize the child DB services we provide and add them to the service registry
            childSp.AddService(m_townLookupFactory.CreateTownLookup(database));

            // Return our new service registry full of child services
            return childSp;
		}

        public class InvalidMongoConnectStringException : Exception { }
        public class InvalidMongoDbException : Exception { }
        public class MongoClientNotCreatedException : Exception { }
        public class MongoDbNotFoundException : Exception { }
	}
}
