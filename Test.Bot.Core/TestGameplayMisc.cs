using Bot.Api;
using Bot.Core;
using Bot.Core.Interaction;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Test.Bot.Core
{
    public class TestGameplayMisc : GameTestBase
    {
        public TestGameplayMisc()
        {
            RegisterService<ITownInteractionErrorHandler>(new TownInteractionErrorHandler(GetServiceProvider()));
        }

        [Fact]
        public void ConstructGame_NoExceptions()
        {
            var _ = new BotGameplay(GetServiceProvider());
        }

        [Fact]
        public void CreateBotServices_ProperServicesAvailable_RegistersServices()
        {
            var sp = ServiceFactory.RegisterBotServices(GetServiceProvider());

            Assert.NotNull(sp.GetService<IBotGameplayInteractionHandler>());
        }

        [Fact]
        public void RunGame_SendsMessageToContext()
        {
            var gs = CreateGameplayInteractionHandler();

            var t = gs.CommandGameAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            BotSystemMock.Verify(c => c.CreateWebhookBuilder(), Times.AtLeastOnce);
            WebhookBuilderMock.Verify(r => r.WithContent(It.IsAny<string>()), Times.AtLeastOnce);
            InteractionContextMock.Verify(c => c.EditResponseAsync(It.Is<IBotWebhookBuilder>(b => b == WebhookBuilderMock.Object)), Times.AtLeastOnce);

            VerifyContext();
        }

        [Fact]
        public void Night_UnhandledException_NotifiesAuthorOfException() => TestUnhandledExceptionForCommand((bg, context) => bg.CommandNightAsync(context));

        [Fact]
        public void Day_UnhandledException_NotifiesAuthorOfException()
        {
            TownSquareMock.SetupGet(t => t.Users).Returns(Enumerable.Empty<IMember>().ToList());
            Cottage1Mock.SetupGet(t => t.Users).Returns(new[] { InteractionAuthorMock.Object });
            Cottage2Mock.SetupGet(t => t.Users).Returns(new[] { Villager1Mock.Object });
            Cottage3Mock.SetupGet(t => t.Users).Returns(new[] { Villager2Mock.Object });
            Cottage4Mock.SetupGet(t => t.Users).Returns(new[] { Villager3Mock.Object });

            TestUnhandledExceptionForCommand((bg, context) => bg.CommandDayAsync(context));
        }

        [Fact]
        public void Vote_UnhandledException_NotifiesAuthorOfException()
        {
            TownSquareMock.SetupGet(t => t.Users).Returns(Enumerable.Empty<IMember>().ToList());
            DarkAlleyMock.SetupGet(t => t.Users).Returns(new[] { InteractionAuthorMock.Object, Villager1Mock.Object, Villager2Mock.Object, Villager3Mock.Object });

            TestUnhandledExceptionForCommand((bg, context) => bg.CommandVoteAsync(context));
        }

        [Fact]
        public void Game_UnhandledException_NotifiesAuthorOfException() => TestUnhandledExceptionForCommand((bg, context) => bg.CommandGameAsync(context));

        private void TestUnhandledExceptionForCommand(Func<BotGameplayInteractionHandler, IBotInteractionContext, Task> gameCommandTestFunc)
        {
            var thrownException = new ApplicationException();

            // Could add other exceptions here for other types of commands, if needed
            Villager1Mock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).ThrowsAsync(thrownException);
            TownLookupMock.Setup(tl => tl.GetTownRecordsAsync(It.IsAny<ulong>())).Throws(thrownException);
            var gs = CreateGameplayInteractionHandler();
            var t = gameCommandTestFunc(gs, InteractionContextMock.Object);
            t.Wait(5);
            Assert.True(t.IsCompleted);

            InteractionAuthorMock.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(thrownException.GetType().Name))), Times.Once);
        }

        [Fact]
        public void GameConstructed_RegistersButtons()
        {
            var gs = CreateGameplayInteractionHandler();
            ComponentServiceMock.Verify(cs => cs.RegisterComponent(It.IsAny<IBotComponent>(), It.IsAny<Func<IBotInteractionContext, Task>>()), Times.AtLeastOnce);
        }

        [Fact]
        public void GameEnd_Completes()
        {
            var gs = CreateGameplayInteractionHandler();
            var t = gs.CommandEndGameAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);
        }

        [Fact]
        public void GameEnd_RolesAndTagsRemoved()
        {
            MockGameInProgress();

            var gs = CreateGameplayInteractionHandler();
            var t = gs.CommandEndGameAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            InteractionAuthorMock.Verify(m => m.RevokeRoleAsync(StorytellerRoleMock.Object), Times.Once);
            InteractionAuthorMock.Verify(m => m.SetDisplayName(StorytellerDisplayName), Times.Once);
            Villager1Mock.Verify(m => m.RevokeRoleAsync(VillagerRoleMock.Object), Times.Once);
            Villager2Mock.Verify(m => m.RevokeRoleAsync(VillagerRoleMock.Object), Times.Once);
        }

        [Fact]
        public void Gameplay_SetStorytellers_OneExisting()
        {
            MockGameInProgress();

            var sts = new[] { InteractionAuthorMock.Object, Villager1Mock.Object };

            BotGameplay gs = new(GetServiceProvider());
            var t = gs.SetStorytellersUnsafe(MockTownKey, InteractionAuthorMock.Object, sts, ProcessLoggerMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            // Verify the original storyteller wasn't revoked OR added again unnecessarily
            InteractionAuthorMock.Verify(x => x.GrantRoleAsync(StorytellerRoleMock.Object), Times.Never);
            InteractionAuthorMock.Verify(m => m.RevokeRoleAsync(StorytellerRoleMock.Object), Times.Never);
            // Verify the new storyteller had the role granted 
            Villager1Mock.Verify(x => x.GrantRoleAsync(StorytellerRoleMock.Object), Times.Once);
        }

        [Fact]
        public void Gameplay_SetStorytellers_NoneExisting()
        {
            MockGameInProgress();

            var sts = new[] { Villager1Mock.Object, Villager2Mock.Object };

            BotGameplay gs = new(GetServiceProvider());
            var t = gs.SetStorytellersUnsafe(MockTownKey, InteractionAuthorMock.Object, sts, ProcessLoggerMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            // Verify the original storyteller had the role revoked
            InteractionAuthorMock.Verify(x => x.GrantRoleAsync(StorytellerRoleMock.Object), Times.Never);
            InteractionAuthorMock.Verify(m => m.RevokeRoleAsync(StorytellerRoleMock.Object), Times.Once);
            // Verify the new storytellers had the role granted
            Villager1Mock.Verify(x => x.GrantRoleAsync(StorytellerRoleMock.Object), Times.Once);
            Villager2Mock.Verify(x => x.GrantRoleAsync(StorytellerRoleMock.Object), Times.Once);
        }
    }
}