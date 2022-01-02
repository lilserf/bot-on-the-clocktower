using Bot.Api;
using Bot.Core;
using Moq;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestGame : GameTestBase
    {
        [Fact]
        public void ConstructGame_NoExceptions()
        {
            var _ = new BotGameService();
        }

        [Fact]
        public void ServiceProvider_GameServiceProvided()
        {
            var sp = ServiceFactory.RegisterServices(null);

            var gs = sp.GetService<IBotGameplay>();
            Assert.IsType<BotGameService>(gs);
        }

        [Fact]
        public void RunGame_SendsMessageToContext()
        {
            BotGameService gs = new();

            var t = gs.RunGameAsync(InteractionContextMock.Object);

            BotSystemMock.Verify(c => c.CreateWebhookBuilder(), Times.Once);
            WebhookBuilderMock.Verify(r => r.WithContent(It.IsAny<string>()), Times.Once);
            InteractionContextMock.Verify(c => c.EditResponseAsync(It.Is<IBotWebhookBuilder>(b => b == WebhookBuilderMock.Object)), Times.Once);

            VerifyContext();
        }
    }
}
