using Bot.Api;
using Bot.Api.Database;
using Bot.Core;
using Bot.DSharp;
using DSharpPlus;
using Moq;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.DSharp
{
    public class TestChannelRetrieval : TestBase
    {
        private readonly Mock<IDiscordClient> m_mockDiscordClient = new(MockBehavior.Strict);

        private readonly Mock<IChannel> m_mockControlChannel = new(MockBehavior.Strict);
        private readonly Mock<IChannel> m_mockTownSquareChannel = new(MockBehavior.Strict);
        private readonly Mock<IChannel> m_mockChatChannel = new(MockBehavior.Strict);
        private readonly Mock<IChannelCategory> m_mockDayChannelCategory = new(MockBehavior.Strict);
        private readonly Mock<IChannelCategory> m_mockNightChannelCategory = new(MockBehavior.Strict);
        private readonly Mock<ITownRecord> m_mockTownRecord = new(MockBehavior.Loose);
        private readonly Mock<ITownDatabase> m_mockTownDb = new(MockBehavior.Strict);

        private const string MismatchedName = "mismatched name";
        private const string ControlName = "control chan";
        private const string TownSquareName = "TS chan";
        private const string ChatName = "chat chan";
        private const string DayCategoryName = "day cat";
        private const string NightCategoryName = "night cat";

        private const ulong MismatchedId = 0;
        private const ulong ControlId = 1;
        private const ulong TownSquareId = 2;
        private const ulong ChatId = 3;
        private const ulong DayCategoryId = 4;
        private const ulong NightCategoryId = 5;

        public TestChannelRetrieval()
        {
            var env = RegisterMock(new Mock<IEnvironment>());
            env.Setup(e => e.GetEnvironmentVariable(It.IsAny<string>())).Returns("env var");

            SetupChannelMock(m_mockDiscordClient, m_mockControlChannel, ControlId, ControlName, false);
            SetupChannelMock(m_mockDiscordClient, m_mockTownSquareChannel, TownSquareId, TownSquareName, true);
            SetupChannelMock(m_mockDiscordClient, m_mockChatChannel, ChatId, ChatName, false);
            SetupChannelCategoryMock(m_mockDiscordClient, m_mockDayChannelCategory, DayCategoryId, DayCategoryName);
            SetupChannelCategoryMock(m_mockDiscordClient, m_mockNightChannelCategory, NightCategoryId, NightCategoryName);

            m_mockTownRecord.SetupGet(tr => tr.ControlChannel).Returns(ControlName);
            m_mockTownRecord.SetupGet(tr => tr.ControlChannelId).Returns(ControlId);
            m_mockTownRecord.SetupGet(tr => tr.TownSquare).Returns(TownSquareName);
            m_mockTownRecord.SetupGet(tr => tr.TownSquareId).Returns(TownSquareId);
            m_mockTownRecord.SetupGet(tr => tr.ChatChannel).Returns(ChatName);
            m_mockTownRecord.SetupGet(tr => tr.ChatChannelId).Returns(ChatId);
            m_mockTownRecord.SetupGet(tr => tr.DayCategory).Returns(DayCategoryName);
            m_mockTownRecord.SetupGet(tr => tr.DayCategoryId).Returns(DayCategoryId);
            m_mockTownRecord.SetupGet(tr => tr.NightCategory).Returns(NightCategoryName);
            m_mockTownRecord.SetupGet(tr => tr.NightCategoryId).Returns(NightCategoryId);

            static void SetupChannelMock(Mock<IDiscordClient> clientMock, Mock<IChannel> channelMock, ulong channelId, string channelName, bool expectedVoice)
            {
                channelMock.SetupGet(c => c.Id).Returns(channelId);
                channelMock.SetupGet(c => c.Name).Returns(channelName);
                clientMock.Setup(c => c.GetChannelAsync(It.Is<ulong>(id => id == channelId))).ReturnsAsync(channelMock.Object);
            }

            static void SetupChannelCategoryMock(Mock<IDiscordClient> clientMock, Mock<IChannelCategory> channelMock, ulong channelId, string channelName)
            {
                channelMock.SetupGet(c => c.Id).Returns(channelId);
                channelMock.SetupGet(c => c.Name).Returns(channelName);
                clientMock.Setup(c => c.GetChannelCategoryAsync(It.Is<ulong>(id => id == channelId))).ReturnsAsync(channelMock.Object);
            }

            var mockFactory = RegisterMock(new Mock<IDiscordClientFactory>());
            mockFactory.Setup(f => f.CreateClient(It.IsAny<DiscordConfiguration>())).Returns(m_mockDiscordClient.Object);

            m_mockTownDb.Setup(db => db.UpdateTownAsync(It.IsAny<ITown>())).ReturnsAsync(true);
        }

        [Fact]
        public void TownResolveChatNameOff_RequestsUpdate()
        {
            var tr = new TownResolver(GetServiceProvider());
            var resolveTask = tr.ResolveTownAsync(m_mockTownRecord.Object);
            resolveTask.Wait(50);
            Assert.True(resolveTask.IsCompleted);

            m_mockTownDb.Verify(db => db.UpdateTownAsync(It.IsAny<ITown>()), Times.Once);
        }
    }
}
