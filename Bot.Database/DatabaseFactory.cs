using Bot.Api;
using Bot.Base;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;

namespace Bot.Database
{
	public class DatabaseFactory : IDatabaseFactory
	{
        private readonly IServiceProvider m_serviceProvider;
        private readonly IEnvironment m_environment;

        ITownLookup IDatabaseFactory.TownLookup => m_townLookup;
        private TownLookup m_townLookup;

        public DatabaseFactory(IServiceProvider serviceProvider)
        {
            m_serviceProvider = serviceProvider;
            m_environment = serviceProvider.GetService<IEnvironment>();
            m_townLookup = new();
        }

        public IServiceProvider Connect()
		{
            // Add ourself to the Services
            var childSp = new ServiceProvider(m_serviceProvider);
            childSp.AddService<IDatabaseFactory>(this);

            // Connect to Mongo
			var connectionString = m_environment.GetEnvironmentVariable("MONGO_CONNECT");
            var db = m_environment.GetEnvironmentVariable("MONGO_DB");

            if(string.IsNullOrWhiteSpace(connectionString))
			{
                throw new InvalidMongoConnectStringException();
			}

            if(string.IsNullOrWhiteSpace(db))
			{
                throw new InvalidMongoDbException();
			}

            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(db);

            // TODO: throw exceptions for null/invalid client or database?

            // Initialize the child DB services we provide and add them to the service registry
            m_townLookup.Connect(database);
            childSp.AddService<ITownLookup>(m_townLookup);

            // Return our new service registry full of child services
            return childSp;
		}

        public class InvalidMongoConnectStringException : Exception { }
        public class InvalidMongoDbException : Exception { }
	}
}
