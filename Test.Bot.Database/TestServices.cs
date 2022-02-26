using Bot.Database;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Database
{
    public class TestServices : TestBase
    {
        [Fact]
        public void RegisterServices_CreatesMongoFactory()
        {
            var newSp = ServiceFactory.RegisterServices(GetServiceProvider());
            var factory = newSp.GetService<IMongoClientFactory>();

            Assert.IsType<MongoClientFactory>(factory);
        }

        [Fact]
        public void RegisterServices_CreatesTownLookupFactory()
        {
            var newSp = ServiceFactory.RegisterServices(GetServiceProvider());
            var factory = newSp.GetService<ITownDatabaseFactory>();

            Assert.IsType<TownDatabaseFactory>(factory);
        }

        [Fact]
        public void RegisterServices_CreatesGameActivityDbFactory()
        {
            var newSp = ServiceFactory.RegisterServices(GetServiceProvider());
            var factory = newSp.GetService<IGameActivityDatabaseFactory>();

            Assert.IsType<GameActivityDatabaseFactory>(factory);
        }
    }
}
