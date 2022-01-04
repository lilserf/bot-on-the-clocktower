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

        [Fact]
        public static void RegisterTwoGames_BothAddsSucceed()
        {
            ActiveGameService ags = new();

            ulong guildId1 = 123;
            ulong guildId2 = 321;
            ulong channelId1 = 456;
            ulong channelId2 = 654;

            Mock<ITown> town1Mock = new();
            Mock<ITown> town2Mock = new();
            Mock<IGuild> guild1Mock = new();
            Mock<IGuild> guild2Mock = new();
            Mock<IChannel> channel1Mock = new();
            Mock<IChannel> channel2Mock = new();
            Mock<IGame> game1Mock = new();
            Mock<IGame> game2Mock = new();

            town1Mock.SetupGet(t => t.Guild).Returns(guild1Mock.Object);
            town2Mock.SetupGet(t => t.Guild).Returns(guild2Mock.Object);
            town1Mock.SetupGet(t => t.ControlChannel).Returns(channel1Mock.Object);
            town2Mock.SetupGet(t => t.ControlChannel).Returns(channel2Mock.Object);

            guild1Mock.SetupGet(g => g.Id).Returns(guildId1);
            guild2Mock.SetupGet(g => g.Id).Returns(guildId2);
            channel1Mock.SetupGet(c => c.Id).Returns(channelId1);
            channel2Mock.SetupGet(c => c.Id).Returns(channelId2);

            bool ret1 = ags.RegisterGame(town1Mock.Object, game1Mock.Object);
            bool ret2 = ags.RegisterGame(town2Mock.Object, game2Mock.Object);

            Assert.True(ret1);
            Assert.True(ret2);
        }
    }
}
