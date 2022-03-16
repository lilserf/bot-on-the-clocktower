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
        private readonly Mock<ITownRecord> m_mockTownRecord = new(MockBehavior.Strict);

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

            SetupChannelMock(m_mockDiscordClient, m_mockControlChannel, ControlId, ControlName, BotChannelType.Text);
            SetupChannelMock(m_mockDiscordClient, m_mockTownSquareChannel, TownSquareId, TownSquareName, BotChannelType.Voice);
            SetupChannelMock(m_mockDiscordClient, m_mockChatChannel, ChatId, ChatName, BotChannelType.Text);
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

            static void SetupChannelMock(Mock<IDiscordClient> clientMock, Mock<IChannel> channelMock, ulong channelId, string channelName, BotChannelType channelType)
            {
                channelMock.SetupGet(c => c.Id).Returns(channelId);
                channelMock.SetupGet(c => c.Name).Returns(channelName);
                clientMock.Setup(c => c.GetChannelAsync(It.Is<ulong>(id => id == channelId), It.Is<string>(s => s == channelName), It.Is<BotChannelType>(ct => ct == channelType))).ReturnsAsync(new GetChannelResult(channelMock.Object, ChannelUpdateRequired.None));
                clientMock.Setup(c => c.GetChannelAsync(It.Is<ulong>(id => id == MismatchedId), It.Is<string>(s => s == channelName), It.Is<BotChannelType>(ct => ct == channelType))).ReturnsAsync(new GetChannelResult(channelMock.Object, ChannelUpdateRequired.Id));
                clientMock.Setup(c => c.GetChannelAsync(It.Is<ulong>(id => id == channelId), It.Is<string>(s => s == MismatchedName), It.Is<BotChannelType>(ct => ct == channelType))).ReturnsAsync(new GetChannelResult(channelMock.Object, ChannelUpdateRequired.Name));

                var otherType = channelType == BotChannelType.Text ? BotChannelType.Voice : BotChannelType.Text;
                clientMock.Setup(c => c.GetChannelAsync(It.Is<ulong>(id => id == channelId), It.Is<string>(s => s == channelName), It.Is<BotChannelType>(ct => ct == otherType))).ReturnsAsync(new GetChannelResult(null, ChannelUpdateRequired.None));
            }

            static void SetupChannelCategoryMock(Mock<IDiscordClient> clientMock, Mock<IChannelCategory> channelMock, ulong channelId, string channelName)
            {
                channelMock.SetupGet(c => c.Id).Returns(channelId);
                channelMock.SetupGet(c => c.Name).Returns(channelName);
                clientMock.Setup(c => c.GetChannelCategoryAsync(It.Is<ulong>(id => id == channelId), It.Is<string>(s => s == channelName))).ReturnsAsync(new GetChannelCategoryResult(channelMock.Object, ChannelUpdateRequired.None));
                clientMock.Setup(c => c.GetChannelCategoryAsync(It.Is<ulong>(id => id == MismatchedId), It.Is<string>(s => s == channelName))).ReturnsAsync(new GetChannelCategoryResult(channelMock.Object, ChannelUpdateRequired.Id));
                clientMock.Setup(c => c.GetChannelCategoryAsync(It.Is<ulong>(id => id == channelId), It.Is<string>(s => s == MismatchedName))).ReturnsAsync(new GetChannelCategoryResult(channelMock.Object, ChannelUpdateRequired.Name));
            }

            var mockFactory = RegisterMock(new Mock<IDiscordClientFactory>());
            mockFactory.Setup(f => f.CreateClient(It.IsAny<DiscordConfiguration>())).Returns(m_mockDiscordClient.Object);
        }

        [Fact]
        public void ControlChannelTypeMismatch_NullReturnChannel() => TestChannelTypeMismatch(m_mockControlChannel.Object, BotChannelType.Text);

        [Fact]
        public void ChatChannelTypeMismatch_NullReturnChannel() => TestChannelTypeMismatch(m_mockChatChannel.Object, BotChannelType.Text);

        [Fact]
        public void TownSquareChannelTypeMismatch_NullReturnChannel() => TestChannelTypeMismatch(m_mockTownSquareChannel.Object, BotChannelType.Voice);

        private void TestChannelTypeMismatch(IChannel channel, BotChannelType channelType)
        {
            var client = new DSharpClient(GetServiceProvider());
            var testType = channelType == BotChannelType.Text ? BotChannelType.Voice : BotChannelType.Text;
            var channelTask = client.GetChannelAsync(channel.Id, channel.Name, testType);
            channelTask.Wait(50);
            Assert.True(channelTask.IsCompleted);
            Assert.Null(channelTask.Result.Channel);
        }

        [Fact]
        public void ControlChannelNameMismatch_RequestsUpdate() => TestChannelNameMismatch(m_mockControlChannel.Object, BotChannelType.Text);

        [Fact]
        public void ChatChannelNameMismatch_RequestsUpdate() => TestChannelNameMismatch(m_mockChatChannel.Object, BotChannelType.Text);

        [Fact]
        public void TownSquareChannelNameMismatch_RequestsUpdate() => TestChannelNameMismatch(m_mockTownSquareChannel.Object, BotChannelType.Voice);

        private void TestChannelNameMismatch(IChannel channel, BotChannelType channelType)
        {
            var client = new DSharpClient(GetServiceProvider());
            var channelTask = client.GetChannelAsync(channel.Id, MismatchedName, channelType);
            channelTask.Wait(50);
            Assert.True(channelTask.IsCompleted);
            Assert.Equal(channel, channelTask.Result.Channel);
            Assert.Equal(ChannelUpdateRequired.Name, channelTask.Result.UpdateRequired);
        }

        [Fact]
        public void DayCatNameMismatch_RequestsUpdate() => TestChannelCategoryNameMismatch(m_mockDayChannelCategory.Object);

        [Fact]
        public void NightCatNameMismatch_RequestsUpdate() => TestChannelCategoryNameMismatch(m_mockNightChannelCategory.Object);

        private void TestChannelCategoryNameMismatch(IChannelCategory channelCategory)
        {
            var client = new DSharpClient(GetServiceProvider());
            var channelCatTask = client.GetChannelCategoryAsync(channelCategory.Id, MismatchedName);
            channelCatTask.Wait(50);
            Assert.True(channelCatTask.IsCompleted);
            Assert.Equal(channelCategory, channelCatTask.Result.Channel);
            Assert.Equal(ChannelUpdateRequired.Name, channelCatTask.Result.UpdateRequired);
        }

        [Fact]
        public void ControlChannelIdMismatch_RequestsUpdate() => TestChannelIdMismatch(m_mockControlChannel.Object, BotChannelType.Text);

        [Fact]
        public void ChatChannelIdMismatch_RequestsUpdate() => TestChannelIdMismatch(m_mockChatChannel.Object, BotChannelType.Text);

        [Fact]
        public void TownSquareChannelIdMismatch_RequestsUpdate() => TestChannelIdMismatch(m_mockTownSquareChannel.Object, BotChannelType.Voice);

        private void TestChannelIdMismatch(IChannel channel, BotChannelType channelType)
        {
            var client = new DSharpClient(GetServiceProvider());
            var channelTask = client.GetChannelAsync(MismatchedId, channel.Name, channelType);
            channelTask.Wait(50);
            Assert.True(channelTask.IsCompleted);
            Assert.Equal(channel, channelTask.Result.Channel);
            Assert.Equal(ChannelUpdateRequired.Id, channelTask.Result.UpdateRequired);
        }

        [Fact]
        public void DayCatIdMismatch_RequestsUpdate() => TestChannelCategoryIdMismatch(m_mockDayChannelCategory.Object);

        [Fact]
        public void NightCatIdMismatch_RequestsUpdate() => TestChannelCategoryIdMismatch(m_mockNightChannelCategory.Object);

        private void TestChannelCategoryIdMismatch(IChannelCategory channelCategory)
        {
            var client = new DSharpClient(GetServiceProvider());
            var channelCatTask = client.GetChannelCategoryAsync(MismatchedId, channelCategory.Name);
            channelCatTask.Wait(50);
            Assert.True(channelCatTask.IsCompleted);
            Assert.Equal(channelCategory, channelCatTask.Result.Channel);
            Assert.Equal(ChannelUpdateRequired.Id, channelCatTask.Result.UpdateRequired);
        }

        /*
        [Fact]
        public void TownResolveChatNameOff_RequestsUpdate()
        {
            var tr = new TownResolver(GetServiceProvider());
            var resolveTask = tr.ResolveTownAsync(m_mockTownRecord.Object);
            resolveTask.Wait(50);
            Assert.True(resolveTask.IsCompleted);
            
        }
        */
    }
}
