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
            _ = new DSharpSystem(GetServiceProvider());
        }

        [Fact]
        public static void System_ImplementsSystemInteraface()
        {
            Assert.True(typeof(IBotSystem).IsAssignableFrom(typeof(DSharpSystem)));
        }

        [Fact]
        public void SystemInitialize_NoDiscordToken_ThrowsException()
        {
            var mockEnv = RegisterMock(new Mock<IEnvironment>());
            DSharpSystem system = new(GetServiceProvider());

            Assert.ThrowsAsync<DSharpSystem.InvalidDiscordTokenException>(system.InitializeAsync)
                .Wait(100);
        }

        [Fact]
        public void SystemInitialize_DiscordToken_NoException()
        {
            var mockEnv = RegisterMock(new Mock<IEnvironment>());
            mockEnv.Setup(env => env.GetEnvironmentVariable(It.Is<string>(s => s == "DISCORD_TOKEN"))).Returns("abcdefg");

            DSharpSystem system = new(GetServiceProvider());

            var t = system.InitializeAsync();
            t.Wait(100);
            Assert.True(t.IsCompleted);
        }
    }
}
