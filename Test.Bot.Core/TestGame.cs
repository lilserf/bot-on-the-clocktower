using Bot.Api;
using Bot.Core;
using Moq;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestGame : TestBase
    {
        [Fact]
        public void ConstructGame_NoExceptions()
        {
            var _ = new BotGameService();
        }

        [Fact]
        public void ServiceProvider_GameServiceProvided()
        {
            var sp = ServiceProviderFactory.CreateServiceProvider();

            var gs = sp.GetService<IBotGameService>();
            Assert.IsType<BotGameService>(gs);
        }

        [Fact]
        public void RunGame_SendsMessageToContext()
        {
            Mock<IBotClient> clientMock = new();
            Mock<IBotInteractionContext> contextMock = new();
            Mock<IBotInteractionResponseBuilder> responseMock = new();
            clientMock.Setup(c => c.CreateInteractionResponseBuilder()).Returns(responseMock.Object);
            BotGameService gs = new();

            var t = gs.RunGameAsync(clientMock.Object, contextMock.Object);

            clientMock.Verify(c => c.CreateInteractionResponseBuilder(), Times.Once);
            responseMock.Verify(r => r.WithContent(It.IsAny<string>()), Times.Once);
            contextMock.Verify(c => c.CreateDeferredResponseMessage(It.Is<IBotInteractionResponseBuilder>(irb => irb == responseMock.Object)), Times.Once);
        }
    }
}
