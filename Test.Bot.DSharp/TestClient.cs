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
        public void ClientConnect_NoDiscordToken_ThrowsException()
        {
            RegisterMock(new Mock<IEnvironment>());
            Assert.Throws<DSharpClient.InvalidDiscordTokenException>(() => new DSharpClient(GetServiceProvider()));
        }
    }
}
