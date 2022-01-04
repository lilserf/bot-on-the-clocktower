using Bot.Api;
using Bot.Base;
using Bot.Core;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestServices : TestBase
    {
        [Fact]
        public static void CreateCoreServices_ConstructsType()
        {
            var sp = ServiceFactory.RegisterCoreServices(null);
            Assert.IsType<ServiceProvider>(sp);
        }

        [Fact]
        public static void CreateBotServices_NothingRegistered_Throws()
        {
            Assert.Throws<ServiceNotFoundException>(() => ServiceFactory.RegisterBotServices(new ServiceProvider()));
        }
    }
}
