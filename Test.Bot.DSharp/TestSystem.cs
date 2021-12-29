using Bot.Api;
using Bot.DSharp;
using Moq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.DSharp
{
    public class TestSystem : TestBase
    {
        [Fact]
        public void ConstructSystem_NoExceptions()
        {
            _ = new DSharpSystem();
        }

        [Fact]
        public static void System_ImplementsSystemInteraface()
        {
            Assert.True(typeof(IBotSystem).IsAssignableFrom(typeof(DSharpSystem)));
        }

        [Fact]
        public void System_CreateCalled_CreatesDSharpClient()
        {
            var mockEnv = RegisterMock(new Mock<IEnvironment>());
            DSharpSystem system = new();

            var result = system.CreateClient(GetServiceProvider());

            Assert.IsType<DSharpClient>(result);
        }
    }
}
