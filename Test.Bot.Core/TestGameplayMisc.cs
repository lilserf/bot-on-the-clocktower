using Bot.Api;
using Bot.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Test.Bot.Core
{
    public class TestGameplayMisc : GameTestBase
    {
        [Fact]
        public void ConstructGame_NoExceptions()
        {
            var _ = new BotGameplay(GetServiceProvider());
        }

        [Fact]
        public void CreateBotServices_ProperServicesAvailable_RegistersServices()
        {
            var sp = ServiceFactory.RegisterBotServices(GetServiceProvider());

            Assert.NotNull(sp.GetService<IBotGameplay>());
        }

        [Fact]
        public void RunGame_SendsMessageToContext()
        {
            BotGameplay gs = new(GetServiceProvider());

            var t = gs.CommandGameAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            BotSystemMock.Verify(c => c.CreateWebhookBuilder(), Times.Once);
            WebhookBuilderMock.Verify(r => r.WithContent(It.IsAny<string>()), Times.Once);
            InteractionContextMock.Verify(c => c.EditResponseAsync(It.Is<IBotWebhookBuilder>(b => b == WebhookBuilderMock.Object)), Times.Once);

            VerifyContext();
        }

        [Fact]
        public void Night_UnhandledException_NotifiesAuthorOfException() => TestUnhandledExceptionForCommand((bg, context) => bg.CommandNightAsync(context));

        [Fact]
        public void Day_UnhandledException_NotifiesAuthorOfException() => TestUnhandledExceptionForCommand((bg, context) => bg.CommandDayAsync(context));

        [Fact]
        public void Vote_UnhandledException_NotifiesAuthorOfException() => TestUnhandledExceptionForCommand((bg, context) => bg.CommandVoteAsync(context));

        [Fact]
        public void Game_UnhandledException_NotifiesAuthorOfException() => TestUnhandledExceptionForCommand((bg, context) => bg.CommandGameAsync(context));

        private void TestUnhandledExceptionForCommand(Func<BotGameplay, IBotInteractionContext, Task> gameCommandTestFunc)
        {
            var thrownException = new ApplicationException();

            // Could add other exceptions here for other types of commands, if needed
            Villager1Mock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>())).ThrowsAsync(thrownException);
            BotSystemMock.Setup(s => s.CreateWebhookBuilder()).Throws(thrownException);

            BotGameplay gs = new(GetServiceProvider());
            var t = gameCommandTestFunc(gs, InteractionContextMock.Object);
            t.Wait(5);
            Assert.True(t.IsCompleted);

            InteractionAuthorMock.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(thrownException.GetType().Name))), Times.Once);
        }

        [Fact]
        public void GameConstructed_RegistersButtons()
        {
            BotGameplay gs = new(GetServiceProvider());
            ComponentServiceMock.Verify(cs => cs.RegisterComponent(It.IsAny<IBotComponent>(), It.IsAny<Func<IBotInteractionContext, Task>>()), Times.AtLeastOnce);
        }

        [Fact]
        public void GameEnd_Completes()
        {
            BotGameplay gs = new(GetServiceProvider());
            var t = gs.CommandEndGameAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GameEnd_RolesAndTagsRemoved(bool gameInProgress)
        {
            if (gameInProgress)
            {
                MockGameInProgress();
            }
            else
            {
                // Even if no game is supposedly in progress, let's give the storyteller the (ST) tag
                InteractionAuthorMock.SetupGet(m => m.DisplayName).Returns(MemberHelper.StorytellerTag + StorytellerDisplayName);
            }

            BotGameplay gs = new(GetServiceProvider());
            var t = gs.CommandEndGameAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            InteractionAuthorMock.Verify(m => m.RevokeRoleAsync(StorytellerRoleMock.Object), Times.Once);
            InteractionAuthorMock.Verify(m => m.SetDisplayName(StorytellerDisplayName), Times.Once);
            Villager1Mock.Verify(m => m.RevokeRoleAsync(VillagerRoleMock.Object), Times.Once);
            Villager2Mock.Verify(m => m.RevokeRoleAsync(VillagerRoleMock.Object), Times.Once);
            ActiveGameServiceMock.Verify(m => m.EndGame(TownMock.Object), Times.Once);
        }

        [Fact]
        public void Gameplay_SetStorytellers_OneExisting()
        {
            var gameMock = MockGameInProgress();

            var sts = new[] { "Peter", "Bob" };

            BotGameplay gs = new(GetServiceProvider());
            var t = gs.SetStorytellersUnsafe(InteractionContextMock.Object, sts, ProcessLoggerMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            gameMock.Verify(g => g.AddStoryteller(It.Is<IMember>(m => m == InteractionAuthorMock.Object)), Times.Never);
            gameMock.Verify(g => g.RemoveStoryteller(It.Is<IMember>(m => m == InteractionAuthorMock.Object)), Times.Never);
            gameMock.Verify(g => g.AddStoryteller(It.Is<IMember>(m => m == Villager1Mock.Object)), Times.Once);
            gameMock.Verify(g => g.RemoveVillager(It.Is<IMember>(m => m == Villager1Mock.Object)), Times.Once);
        }

        [Fact]
        public void Gameplay_SetStorytellers_NoneExisting()
        {
            var gameMock = MockGameInProgress();

            var sts = new[] { "Alice", "Bob" };

            BotGameplay gs = new(GetServiceProvider());
            var t = gs.SetStorytellersUnsafe(InteractionContextMock.Object, sts, ProcessLoggerMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            gameMock.Verify(g => g.AddStoryteller(It.Is<IMember>(m => m == InteractionAuthorMock.Object)), Times.Never);
            gameMock.Verify(g => g.RemoveStoryteller(It.Is<IMember>(m => m == InteractionAuthorMock.Object)), Times.Once);
            gameMock.Verify(g => g.AddStoryteller(It.Is<IMember>(m => m == Villager1Mock.Object)), Times.Once);
            gameMock.Verify(g => g.RemoveVillager(It.Is<IMember>(m => m == Villager1Mock.Object)), Times.Once);
            gameMock.Verify(g => g.AddStoryteller(It.Is<IMember>(m => m == Villager2Mock.Object)), Times.Once);
            gameMock.Verify(g => g.RemoveVillager(It.Is<IMember>(m => m == Villager2Mock.Object)), Times.Once);
        }
    }
}