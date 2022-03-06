using Bot.Api;
using Bot.DSharp;
using Bot.DSharp.DiscordWrappers;
using DSharpPlus;
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
            var mockClient = new Mock<IDiscordClient>();
            var mockFactory = RegisterMock(new Mock<IDiscordClientFactory>());
            mockFactory.Setup(f => f.CreateClient(It.IsAny<DiscordConfiguration>())).Returns(mockClient.Object);

            var env = RegisterMock(new Mock<IEnvironment>());
            env.Setup(e => e.GetEnvironmentVariable(It.IsAny<string>())).Returns("env var");

            DSharpSystem system = new();

            var result = system.CreateClient(GetServiceProvider());

            Assert.IsType<DSharpClient>(result);
        }
    }
}
