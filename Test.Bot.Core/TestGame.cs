using Bot.Api;
using Bot.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestGame : GameTestBase
    {
        [Fact]
        public void ConstructGame_NoExceptions()
        {
            var _ = new BotGameplay();
        }

        [Fact]
        public void ServiceProvider_GameServiceProvided()
        {
            var sp = ServiceFactory.RegisterServices(null);

            var gs = sp.GetService<IBotGameplay>();
            Assert.IsType<BotGameplay>(gs);
        }

        [Fact]
        public void RunGame_SendsMessageToContext()
        {
            BotGameplay gs = new();

            var t = gs.RunGameAsync(InteractionContextMock.Object);
            t.Wait(50);
            Assert.True(t.IsCompleted);

            BotSystemMock.Verify(c => c.CreateWebhookBuilder(), Times.Once);
            WebhookBuilderMock.Verify(r => r.WithContent(It.IsAny<string>()), Times.Once);
            InteractionContextMock.Verify(c => c.EditResponseAsync(It.Is<IBotWebhookBuilder>(b => b == WebhookBuilderMock.Object)), Times.Once);

            VerifyContext();
        }

        [Fact]
        public void Night_UnhandledException_NotifiesAuthorOfException() => TestUnhandledExceptionForCommand((bg, context) => bg.PhaseNightAsync(context));

        [Fact]
        public void Day_UnhandledException_NotifiesAuthorOfException() =>   TestUnhandledExceptionForCommand((bg, context) => bg.PhaseDayAsync(context));

        [Fact]
        public void Vote_UnhandledException_NotifiesAuthorOfException() =>  TestUnhandledExceptionForCommand((bg, context) => bg.PhaseVoteAsync(context));

        [Fact]
        public void Game_UnhandledException_NotifiesAuthorOfException() =>  TestUnhandledExceptionForCommand((bg, context) => bg.RunGameAsync(context));

        private void TestUnhandledExceptionForCommand(Func<BotGameplay, IBotInteractionContext, Task> gameCommandTestFunc)
        {
            var thrownException = new ApplicationException();

            // Could add other exceptions here for other types of commands, if needed
            Villager1Mock.Setup(m => m.MoveToChannelAsync(It.IsAny<IChannel>(), It.IsAny<IProcessLogger>())).ThrowsAsync(thrownException);
            BotSystemMock.Setup(s => s.CreateWebhookBuilder()).Throws(thrownException);

            BotGameplay gs = new();
            var t = gameCommandTestFunc(gs, InteractionContextMock.Object);
            t.Wait(5);
            Assert.True(t.IsCompleted);

            InteractionAuthorMock.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(thrownException.GetType().Name))), Times.Once);
        }
    }
}
