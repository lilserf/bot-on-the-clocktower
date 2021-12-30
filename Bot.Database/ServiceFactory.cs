using Bot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Database
{
    public static class ServiceFactory
    {
        public static IServiceProvider RegisterServices(IServiceProvider parentServices)
        {
            var childSp = new ServiceProvider(parentServices);

            childSp.AddService<ITownLookupFactory>(new TownLookupFactory());
            childSp.AddService<IMongoClientFactory>(new MongoClientFactory());

            return childSp;
        }
    }
}
