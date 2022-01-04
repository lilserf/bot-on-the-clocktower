using Bot.Api;
using Bot.Core;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestRunner : TestBase
    {
        [Fact]
        public void ConstructRunner_NoExceptions()
        {
            RegisterMock(new Mock<IBotSystem>());
            _ = new BotSystemRunner(GetServiceProvider());
        }

        [Fact]
        public void GiveRunnerSystem_Run_CreatesClient()
        {
            Mock<IBotSystem> sysMock = new();
            RegisterMock(sysMock);
            BotSystemRunner runner = new(GetServiceProvider());
            sysMock.Setup(s => s.CreateClient(It.IsAny<IServiceProvider>())).Returns(new Mock<IBotClient>().Object);
            Mock<IBotGameplay> gameplayMock = new();
            gameplayMock.Setup(g => g.CreateComponents(It.IsAny<IServiceProvider>()));
            RegisterMock(gameplayMock);

            sysMock.Verify(s => s.CreateClient(It.IsAny<IServiceProvider>()), Times.Never);

            var t = runner.RunAsync(CancellationToken.None);
            t.Wait(5);

            sysMock.Verify(s => s.CreateClient(It.IsAny<IServiceProvider>()), Times.Once);
        }

        [Fact]
        public void GiveRunnerSystem_Run_RunsClient()
        {
            Mock<IBotSystem> sysMock = new();
            Mock<IBotClient> clientMock = new();
            sysMock.Setup(s => s.CreateClient(It.IsAny<IServiceProvider>())).Returns(clientMock.Object);
            RegisterMock(sysMock);
            Mock<IBotGameplay> gameplayMock = new();
            gameplayMock.Setup(g => g.CreateComponents(It.IsAny<IServiceProvider>()));
            RegisterMock(gameplayMock);

            BotSystemRunner runner = new(GetServiceProvider());

            clientMock.Verify(c => c.ConnectAsync(), Times.Never);

            var t = runner.RunAsync(CancellationToken.None);
            t.Wait(5);

            clientMock.Verify(c => c.ConnectAsync(), Times.Once);
        }

        [Fact]
        public void GiveRunnerSystem_RunsForever_NotComplete()
        {
            Mock<IBotSystem> sysMock = new();
            Mock<IBotClient> clientMock = new();
            sysMock.Setup(s => s.CreateClient(It.IsAny<IServiceProvider>())).Returns(clientMock.Object);
            RegisterMock(sysMock);
            Mock<IBotGameplay> gameplayMock = new();
            gameplayMock.Setup(g => g.CreateComponents(It.IsAny<IServiceProvider>()));
            RegisterMock(gameplayMock);

            TaskCompletionSource<bool> tcs = new();
            clientMock.Setup(c => c.ConnectAsync()).Returns(tcs.Task);

            BotSystemRunner runner = new(GetServiceProvider());

            var t = runner.RunAsync(CancellationToken.None);
            t.Wait(5);

            Assert.False(t.IsCompleted);
        }

        [Fact]
        public void GiveRunnerSystem_Cancelled_Complete()
        {
            Mock<IBotSystem> sysMock = new();
            Mock<IBotClient> clientMock = new();
            sysMock.Setup(s => s.CreateClient(It.IsAny<IServiceProvider>())).Returns(clientMock.Object);
            RegisterMock(sysMock);
            Mock<IBotGameplay> gameplayMock = new();
            gameplayMock.Setup(g => g.CreateComponents(It.IsAny<IServiceProvider>()));
            RegisterMock(gameplayMock);

            TaskCompletionSource<bool> tcs = new();
            clientMock.Setup(c => c.ConnectAsync()).Returns(tcs.Task);

            BotSystemRunner runner = new(GetServiceProvider());

            using CancellationTokenSource cts = new();
            var t = runner.RunAsync(cts.Token);
            t.Wait(5);

            cts.Cancel();
            t.Wait(5);

            Assert.True(t.IsCompleted);
        }
    }
}
