using Bot.Base;
using Bot.Core;
using Xunit;

namespace Test.Bot.Core
{
    public static class TestServices
    {
        [Fact]
        public static void CreateServices_ConstructsType()
        {
            var sp = ServiceFactory.RegisterServices(null);
            Assert.IsType<ServiceProvider>(sp);
        }
    }
}
