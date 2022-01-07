using Bot.Api;
using Bot.Core;
using Bot.Core.Callbacks;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Test.Bot.Base;

namespace Test.Bot.Core
{
    public class GameTestBase : TestBase
    {
        protected const ulong MockGuildId = 1337;
        protected const ulong MockChannelId = 42;
        protected const string StorytellerDisplayName = "Peter Storyteller";

        protected readonly Mock<ICallbackScheduler<ITownRecord>> TownRecordCallbackSchedulerMock = new(MockBehavior.Strict);
        protected readonly Mock<ICallbackSchedulerFactory> CallbackSchedulerFactoryMock = new(MockBehavior.Strict);

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
        protected readonly Mock<IActiveGameService> ActiveGameServiceMock = new();
        protected readonly Mock<IProcessLogger> ProcessLoggerMock = new();
        protected readonly Mock<IComponentService> ComponentServiceMock = new();
        protected readonly Mock<IShuffleService> ShuffleServiceMock = new();

        public GameTestBase()
        {
            RegisterMock(CallbackSchedulerFactoryMock);
            CallbackSchedulerFactoryMock.Setup(csf => csf.CreateScheduler(It.IsAny<Func<ITownRecord, Task>>(), It.IsAny<TimeSpan>())).Returns(TownRecordCallbackSchedulerMock.Object);

            Mock<IBotWebhookBuilder> builderMock = new();
            BotSystemMock.Setup(c => c.CreateWebhookBuilder()).Returns(WebhookBuilderMock.Object);

            // WithContent returns the mock again so you can chain calls
            WebhookBuilderMock.Setup(c => c.WithContent(It.IsAny<string>())).Returns(WebhookBuilderMock.Object);
            WebhookBuilderMock.Setup(c => c.AddComponents(It.IsAny<IBotComponent[]>())).Returns(WebhookBuilderMock.Object);

            RegisterMock(BotSystemMock);
            RegisterMock(ClientMock);
            RegisterMock(TownLookupMock);
            RegisterMock(ActiveGameServiceMock);
            RegisterMock(ComponentServiceMock);
            RegisterMock(ShuffleServiceMock);

            ShuffleServiceMock.Setup(ss => ss.Shuffle(It.IsAny<IEnumerable<Tuple<IChannel, IMember>>>()))
                .Returns((IEnumerable<Tuple<IChannel, IMember>> input) => input.Reverse());
            ShuffleServiceMock.Setup(ss => ss.Shuffle(It.IsAny<IEnumerable<IMember>>()))
                .Returns((IEnumerable<IMember> input) => input.Reverse());

            // TownLookup expects MockGuildId and MockChannelId and returns the TownRecord
            TownLookupMock.Setup(tl => tl.GetTownRecord(It.Is<ulong>(a => a == MockGuildId), It.Is<ulong>(b => b == MockChannelId))).ReturnsAsync(TownRecordMock.Object);

            // ResolveTown expects the TownRecord and returns the Town
            ClientMock.Setup(c => c.ResolveTownAsync(It.Is<ITownRecord>(tr => tr == TownRecordMock.Object))).ReturnsAsync(TownMock.Object);

            // By default, the ActiveGameService won't find a game for this context
            IGame? defaultGame = null;
            ActiveGameServiceMock.Setup(a => a.TryGetGame(It.IsAny<IBotInteractionContext>(), out defaultGame)).Returns(false);

            ProcessLoggerMock.Setup(c => c.LogException(It.IsAny<Exception>(), It.IsAny<string>()));

            GuildMock.SetupGet(x => x.Id).Returns(MockGuildId);
            ControlChannelMock.SetupGet(x => x.Id).Returns(MockChannelId);

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
            TownMock.SetupGet(t => t.TownRecord).Returns(TownRecordMock.Object);

            VillagerRoleMock.SetupGet(r => r.Name).Returns("BotC Villager Mock");
            VillagerRoleMock.SetupGet(r => r.Mention).Returns(@"BotC Villager Mock");

            SetupChannelMock(ControlChannelMock, "botc_mover_mock", false);
            SetupChannelMock(ChatChannelMock, "chat_mock", false);

            SetupChannelMock(TownSquareMock, "Town Square Mock");
            TownSquareMock.SetupGet(t => t.Users).Returns(new[] { InteractionAuthorMock.Object, Villager1Mock.Object, Villager2Mock.Object });

            DayCategoryMock.SetupGet(c => c.Channels).Returns(new[] { ControlChannelMock.Object, ChatChannelMock.Object, TownSquareMock.Object });
            
            SetupChannelMock(Cottage1Mock, "Cottage 1 Mock");
            SetupChannelMock(Cottage2Mock, "Cottage 2 Mock");
            SetupChannelMock(Cottage3Mock, "Cottage 3 Mock");
            Cottage1Mock.SetupGet(x => x.Position).Returns(1);
            Cottage2Mock.SetupGet(x => x.Position).Returns(2);
            Cottage3Mock.SetupGet(x => x.Position).Returns(3);
            // Purposely don't order the collection of cottages in their display order
            NightCategoryMock.SetupGet(c => c.Channels).Returns(new[] { Cottage1Mock.Object, Cottage3Mock.Object, Cottage2Mock.Object});

            SetupUserMock(InteractionAuthorMock, StorytellerDisplayName);
            SetupUserMock(Villager1Mock, "Bob");
            SetupUserMock(Villager2Mock, "Alice");
        }

        protected static void SetupChannelMock(Mock<IChannel> channel, string name, bool isVoice=true)
        {
            channel.SetupGet(c => c.Users).Returns(Array.Empty<IMember>());
            channel.SetupGet(c => c.Name).Returns(name);
            channel.SetupGet(c => c.IsVoice).Returns(isVoice);
        }

        protected static void SetupUserMock(Mock<IMember> member, string name)
		{
            member.SetupGet(x => x.DisplayName).Returns(name);
            member.SetupGet(x => x.Roles).Returns(Array.Empty<IRole>());
        }

        protected Mock<IGame> MockGameInProgress()
        {
            var gameMock = new Mock<IGame>();
            gameMock.SetupGet(g => g.Town).Returns(TownMock.Object);
            gameMock.SetupGet(g => g.AllPlayers).Returns(new[] { Villager1Mock.Object, Villager2Mock.Object, InteractionAuthorMock.Object });
            gameMock.SetupGet(g => g.StoryTellers).Returns(() => new[] { InteractionAuthorMock.Object });
            gameMock.SetupGet(g => g.Villagers).Returns(new[] { Villager1Mock.Object, Villager2Mock.Object });
            var gameObject = gameMock.Object;
            ActiveGameServiceMock.Setup(ags => ags.TryGetGame(It.IsAny<IBotInteractionContext>(), out gameObject)).Returns(true);
            InteractionAuthorMock.SetupGet(m => m.DisplayName).Returns(MemberHelper.StorytellerTag + StorytellerDisplayName);
            InteractionAuthorMock.SetupGet(m => m.Roles).Returns(new[] { StoryTellerRoleMock.Object });
            Villager1Mock.SetupGet(m => m.Roles).Returns(new[] { VillagerRoleMock.Object });
            Villager2Mock.SetupGet(m => m.Roles).Returns(new[] { VillagerRoleMock.Object });
            return gameMock;
        }


        // Common assumptions we want to make for most of our Interactions
        protected void VerifyContext()
        {
            // Most interactions should Defer to allow for latency
            InteractionContextMock.Verify(c => c.DeferInteractionResponse(), Times.Once);
        }
    }
}
