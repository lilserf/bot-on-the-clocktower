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
            Mock<ITownRecord> townRecordMock = new();
            Mock<IGame> gameMock = new();

            townMock.SetupGet(t => t.TownRecord).Returns(townRecordMock.Object);
            townRecordMock.SetupGet(tr => tr.GuildId).Returns(guildId);
            townRecordMock.SetupGet(tr => tr.ControlChannelId).Returns(channelId);

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
            Mock<ITownRecord> townRecord1Mock = new();
            Mock<ITownRecord> townRecord2Mock = new();
            Mock<IGame> game1Mock = new();
            Mock<IGame> game2Mock = new();

            town1Mock.SetupGet(t => t.TownRecord).Returns(townRecord1Mock.Object);
            townRecord1Mock.SetupGet(tr => tr.GuildId).Returns(guildId1);
            townRecord1Mock.SetupGet(tr => tr.ControlChannelId).Returns(channelId1);

            town2Mock.SetupGet(t => t.TownRecord).Returns(townRecord2Mock.Object);
            townRecord2Mock.SetupGet(tr => tr.GuildId).Returns(guildId2);
            townRecord2Mock.SetupGet(tr => tr.ControlChannelId).Returns(channelId2);

            bool ret1 = ags.RegisterGame(town1Mock.Object, game1Mock.Object);
            bool ret2 = ags.RegisterGame(town2Mock.Object, game2Mock.Object);

            Assert.True(ret1);
            Assert.True(ret2);
        }
    }
}
