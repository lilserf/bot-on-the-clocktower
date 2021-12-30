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
            var factory = newSp.GetService<ITownLookupFactory>();

            Assert.IsType<TownLookupFactory>(factory);
        }
    }
}
