using Bot.Api;
using Bot.DSharp;
using Moq;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.DSharp
{
    public class TestClient : TestBase
    {
        [Fact]
        public async Task ClientConnect_NoDiscordToken_ThrowsException()
        {
            var mockEnv = RegisterMock(new Mock<IEnvironment>());
            DSharpClient client = new(GetServiceProvider());

            await Assert.ThrowsAsync<DSharpClient.InvalidDiscordTokenException>(() => client.ConnectAsync());
        }
    }
}
