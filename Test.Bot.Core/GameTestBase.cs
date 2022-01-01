using Bot.Api;
using Moq;
using Test.Bot.Base;

namespace Test.Bot.Core
{
    public class GameTestBase : TestBase
    {
        protected const ulong MockGuildId = 1337;
        protected const ulong MockChannelId = 42;

        protected readonly Mock<IBotSystem> BotSystemMock = new();
        protected readonly Mock<IBotWebhookBuilder> WebhookBuilderMock = new();

        protected readonly Mock<IGuild> GuildMock = new();
        protected readonly Mock<ITownLookup> TownLookupMock = new();
        protected readonly Mock<ITown> TownMock = new();
        protected readonly Mock<ITownRecord> TownRecordMock = new();
        protected readonly Mock<IBotClient> ClientMock = new();

        protected readonly Mock<IChannel> ControlChannelMock = new();
        protected readonly Mock<IChannel> TownSquareMock = new();
        protected readonly Mock<IChannel> DayCategoryMock = new();
        protected readonly Mock<IChannel> NightCategoryMock = new();
        protected readonly Mock<IChannel> ChatChannelMock = new();

        protected readonly Mock<IChannel> DarkAlleyMock = new();
        protected readonly Mock<IChannel> Cottage1Mock = new();
        protected readonly Mock<IChannel> Cottage2Mock = new();
        protected readonly Mock<IChannel> Cottage3Mock = new();

        protected readonly Mock<IBotInteractionContext> InteractionContextMock = new();


        public GameTestBase()
        {
            Mock<IBotWebhookBuilder> builderMock = new();
            BotSystemMock.Setup(c => c.CreateWebhookBuilder()).Returns(WebhookBuilderMock.Object);

            RegisterMock(BotSystemMock);
            RegisterMock(ClientMock);
            RegisterMock(TownLookupMock);

            TownLookupMock.Setup(tl => tl.GetTownRecord(It.Is<ulong>(a => a == MockGuildId), It.Is<ulong>(b => b == MockChannelId))).ReturnsAsync(TownRecordMock.Object);

            ClientMock.Setup(c => c.ResolveTownAsync(It.Is<ITownRecord>(tr => tr == TownRecordMock.Object))).ReturnsAsync(TownMock.Object);

            GuildMock.SetupGet(x => x.Id).Returns(MockGuildId);
            ControlChannelMock.SetupGet(x => x.Id).Returns(MockChannelId);

            InteractionContextMock.SetupGet(c => c.Services).Returns(GetServiceProvider());
            InteractionContextMock.SetupGet(x => x.Guild).Returns(GuildMock.Object);
            InteractionContextMock.SetupGet(x => x.Channel).Returns(ControlChannelMock.Object);

            TownMock.SetupGet(t => t.Guild).Returns(GuildMock.Object);
            TownMock.SetupGet(t => t.ControlChannel).Returns(ControlChannelMock.Object);
            TownMock.SetupGet(t => t.TownSquare).Returns(TownSquareMock.Object);
            TownMock.SetupGet(t => t.DayCategory).Returns(DayCategoryMock.Object);
            TownMock.SetupGet(t => t.NightCategory).Returns(NightCategoryMock.Object);
            TownMock.SetupGet(t => t.ChatChannel).Returns(ChatChannelMock.Object);

            SetupChannelMock(ControlChannelMock);
            SetupChannelMock(ChatChannelMock);

            SetupChannelMock(TownSquareMock);

            DayCategoryMock.SetupGet(c => c.Channels).Returns(new[] { ControlChannelMock.Object, ChatChannelMock.Object, TownSquareMock.Object });
            NightCategoryMock.SetupGet(c => c.Channels).Returns(new[] { Cottage1Mock.Object, Cottage2Mock.Object, Cottage3Mock.Object });
        }

        private static void SetupChannelMock(Mock<IChannel> channel)
        {
            channel.SetupGet(c => c.Users).Returns(new IMember[] { });
        }

        // Common assumptions we want to make for most of our Interactions
        protected void VerifyContext()
        {
            // Most interactions should Defer to allow for latency
            InteractionContextMock.Verify(c => c.CreateDeferredResponseMessageAsync(), Times.Once);
        }
    }
}
