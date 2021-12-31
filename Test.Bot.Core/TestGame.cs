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
            var sp = ServiceFactory.RegisterServices(null);

            var gs = sp.GetService<IBotGameService>();
            Assert.IsType<BotGameService>(gs);
        }

        [Fact]
        public void RunGame_SendsMessageToContext()
        {
            Mock<IBotSystem> systemMock = RegisterMock<IBotSystem>();
            Mock<IBotInteractionContext> contextMock = GetStandardContext();
            Mock<IBotWebhookBuilder> builderMock = new();
            systemMock.Setup(c => c.CreateWebhookBuilder()).Returns(builderMock.Object);
            BotGameService gs = new();

            var t = gs.RunGameAsync(contextMock.Object);

            systemMock.Verify(c => c.CreateWebhookBuilder(), Times.Once);
            builderMock.Verify(r => r.WithContent(It.IsAny<string>()), Times.Once);
            contextMock.Verify(c => c.EditResponseAsync(It.Is<IBotWebhookBuilder>(b => b == builderMock.Object)), Times.Once);

            VerifyContext(contextMock);
        }

        [Fact]
        public void PhaseNight_LooksUpTown()
		{
            Mock<IBotInteractionContext> contextMock = GetStandardContext();
            Mock<ITownLookup> townLookupMock = RegisterMock<ITownLookup>();
            Mock<ITown> townMock = new();

            townLookupMock.Setup(x => x.GetTown(It.Is<ulong>(a => a == MockGuildId), It.Is<ulong>(b => b == MockChannelId))).ReturnsAsync(townMock.Object);

            BotGameService gs = new();
            var t = gs.PhaseNightAsync(contextMock.Object);

            townLookupMock.Verify(x => x.GetTown(It.Is<ulong>(a => a == MockGuildId), It.Is<ulong>(b => b == MockChannelId)), Times.Once);
            VerifyContext(contextMock);
        }
    }
}
