using Bot.Api;
using Bot.Base;
using Bot.Core;
using System;
using Xunit;

namespace Test.Bot.Core
{
    public static class TestServices
    {
        [Fact]
        public static void CreateServices_ConstructsType()
        {
            var sp = ServiceProviderFactory.CreateServiceProvider();
            Assert.IsType<ServiceProvider>(sp);
        }

        [Fact]
        public static void CreateServices_HasEnvironment()
        {
            var sp = ServiceProviderFactory.CreateServiceProvider();
            var env = sp.GetService<IEnvironment>();

            Assert.IsType<BotEnvironment>(env);
        }
    }
}
