using Bot.Api;
using Bot.Base;
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
            var childSp = new ServiceProvider(m_serviceProvider);
            childSp.AddService<IDatabaseFactory>(this);

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

            MongoClient client = new MongoClient(connectionString);

            m_townLookup.Connect(client);
            childSp.AddService<ITownLookup>(m_townLookup);

            return childSp;
		}

        public class InvalidMongoConnectStringException : Exception { }
        public class InvalidMongoDbException : Exception { }
	}
}
