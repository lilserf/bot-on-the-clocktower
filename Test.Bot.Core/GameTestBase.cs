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

        protected readonly Mock<IRole> StoryTellerRoleMock = new();
        protected readonly Mock<IRole> VillagerRoleMock = new();

        protected readonly Mock<IChannel> DarkAlleyMock = new();
        protected readonly Mock<IChannel> Cottage1Mock = new();
        protected readonly Mock<IChannel> Cottage2Mock = new();
        protected readonly Mock<IChannel> Cottage3Mock = new();

        protected readonly Mock<IMember> InteractionAuthorMock = new();
        protected readonly Mock<IMember> Villager1Mock = new();
        protected readonly Mock<IMember> Villager2Mock = new();

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
            InteractionContextMock.SetupGet(x => x.Member).Returns(InteractionAuthorMock.Object);

            TownMock.SetupGet(t => t.Guild).Returns(GuildMock.Object);
            TownMock.SetupGet(t => t.ControlChannel).Returns(ControlChannelMock.Object);
            TownMock.SetupGet(t => t.TownSquare).Returns(TownSquareMock.Object);
            TownMock.SetupGet(t => t.DayCategory).Returns(DayCategoryMock.Object);
            TownMock.SetupGet(t => t.NightCategory).Returns(NightCategoryMock.Object);
            TownMock.SetupGet(t => t.ChatChannel).Returns(ChatChannelMock.Object);
            TownMock.SetupGet(t => t.StoryTellerRole).Returns(StoryTellerRoleMock.Object);
            TownMock.SetupGet(t => t.VillagerRole).Returns(VillagerRoleMock.Object);

            SetupChannelMock(ControlChannelMock);
            SetupChannelMock(ChatChannelMock);

            SetupChannelMock(TownSquareMock);
            TownSquareMock.SetupGet(t => t.Users).Returns(new[] { InteractionAuthorMock.Object, Villager1Mock.Object, Villager2Mock.Object });

            DayCategoryMock.SetupGet(c => c.Channels).Returns(new[] { ControlChannelMock.Object, ChatChannelMock.Object, TownSquareMock.Object });
            Cottage1Mock.SetupGet(x => x.Position).Returns(1);
            Cottage2Mock.SetupGet(x => x.Position).Returns(2);
            Cottage3Mock.SetupGet(x => x.Position).Returns(3);
            // Purposely don't order the collection of cottages in their display order
            NightCategoryMock.SetupGet(c => c.Channels).Returns(new[] { Cottage1Mock.Object, Cottage3Mock.Object, Cottage2Mock.Object});

            InteractionAuthorMock.SetupGet(x => x.DisplayName).Returns("Storyteller");
            Villager1Mock.SetupGet(x => x.DisplayName).Returns("Bob");
            Villager2Mock.SetupGet(x => x.DisplayName).Returns("Alice");
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
