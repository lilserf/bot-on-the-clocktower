using Bot.Api;
using Bot.Core;
using Moq;
using System;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestRunner : TestBase
    {
        [Fact]
        public void ConstructRunner_NoExceptions()
        {
            _ = new BotSystemRunner(GetServiceProvider(), new Mock<IBotSystem>().Object);
        }

        [Fact]
        public void GiveRunnerSystem_Run_CreatesClient()
        {
            Mock<IBotSystem> sysMock = new();
            BotSystemRunner runner = new(GetServiceProvider(), sysMock.Object);

            sysMock.Verify(s => s.CreateClient(It.IsAny<IServiceProvider>()), Times.Never);

            var t = runner.RunAsync();
            t.Wait(100);

            sysMock.Verify(s => s.CreateClient(It.IsAny<IServiceProvider>()), Times.Once);
            Assert.True(t.IsCompleted);
        }

        [Fact]
        public void GiveRunnerSystem_Run_RunsClient()
        {
            Mock<IBotSystem> sysMock = new();
            Mock<IBotClient> clientMock = new();
            sysMock.Setup(s => s.CreateClient(It.IsAny<IServiceProvider>())).Returns(clientMock.Object);

            BotSystemRunner runner = new(GetServiceProvider(), sysMock.Object);

            clientMock.Verify(c => c.ConnectAsync(), Times.Never);

            var t = runner.RunAsync();
            t.Wait(100);

            clientMock.Verify(c => c.ConnectAsync(), Times.Once);
            Assert.True(t.IsCompleted);
        }
    }
}
