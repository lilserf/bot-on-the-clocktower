using Bot.Api;
using Bot.Core;
using Moq;
using Xunit;

namespace Test.Bot.Core
{
    public class TestActiveGames
    {
        [Fact]
        public static void RegisterSameGameTwice_SecondAddFails()
        {
            ActiveGameService ags = new();

            ulong guildId = 123;
            ulong channelId = 456;

            Mock<ITown> townMock = new();
            Mock<IGuild> guildMock = new();
            Mock<IChannel> channelMock = new();
            Mock<IGame> gameMock = new();

            townMock.SetupGet(t => t.Guild).Returns(guildMock.Object);
            townMock.SetupGet(t => t.ControlChannel).Returns(channelMock.Object);

            guildMock.SetupGet(g => g.Id).Returns(guildId);
            channelMock.SetupGet(c => c.Id).Returns(channelId);

            bool ret1 = ags.RegisterGame(townMock.Object, gameMock.Object);
            bool ret2 = ags.RegisterGame(townMock.Object, gameMock.Object);

            Assert.True(ret1);
            Assert.False(ret2);
        }
    }
}
