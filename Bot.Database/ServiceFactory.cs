using Bot.Base;
using System;

namespace Bot.Database
{
    public static class ServiceFactory
    {
        public static IServiceProvider RegisterServices(IServiceProvider parentServices)
        {
            var childSp = new ServiceProvider(parentServices);

            childSp.AddService<ITownDatabaseFactory>(new TownDatabaseFactory());
            childSp.AddService<IMongoClientFactory>(new MongoClientFactory());

            return childSp;
        }
    }
}
