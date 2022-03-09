using Bot.DSharp;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Database
{
    public class TestServices : TestBase
    {
        [Fact]
        public void RegisterServices_CreatesDiscordClientFactory()
        {
            var newSp = ServiceFactory.RegisterServices(GetServiceProvider());
            var service = newSp.GetService<IDiscordClientFactory>();

            Assert.NotNull(service);
            Assert.IsType<DiscordClientFactory>(service);
        }
    }
}
