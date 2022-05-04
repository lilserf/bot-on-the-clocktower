using Bot.DSharp;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Database
{
    public class TestServices : TestBase
    {
        [Theory]
        [InlineData(typeof(IDiscordClientFactory), typeof(DiscordClientFactory))]
        public void RegisterServices_CreatesAllRequiredServices(Type serviceInterface, Type serviceImpl)
        {
            var newSp = ServiceFactory.RegisterServices(GetServiceProvider());
            var service = newSp.GetService(serviceInterface);

            Assert.NotNull(service);
            Assert.IsType(serviceImpl, service);
        }
    }
}
