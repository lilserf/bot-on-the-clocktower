using Bot.Api;
using Bot.Core;
using Bot.Core.Callbacks;
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
        private readonly Mock<IBotSystem> m_systemMock = new();
        private readonly Mock<IBotClient> m_clientMock = new();
        private readonly Mock<IComponentService> m_componentService = new();

        public TestRunner()
        {
            RegisterMock(new Mock<ICallbackSchedulerFactory>());
            RegisterMock(m_componentService);

            m_systemMock.Setup(s => s.CreateClient(It.IsAny<IServiceProvider>())).Returns(m_clientMock.Object);
        }

        [Fact]
        public void ConstructRunner_NoExceptions()
        {
            _ = new BotSystemRunner(GetServiceProvider(), m_systemMock.Object);
        }

        [Fact]
        public void GiveRunnerSystem_Run_CreatesClient()
        {
            BotSystemRunner runner = new(GetServiceProvider(), m_systemMock.Object);

            m_systemMock.Verify(s => s.CreateClient(It.IsAny<IServiceProvider>()), Times.Once);
        }

        [Fact]
        public void GiveRunnerSystem_Run_RunsClient()
        {
            BotSystemRunner runner = new(GetServiceProvider(), m_systemMock.Object);

            m_clientMock.Verify(c => c.ConnectAsync(It.IsAny<IServiceProvider>()), Times.Never);

            var t = runner.RunAsync(CancellationToken.None);
            t.Wait(5);

            m_clientMock.Verify(c => c.ConnectAsync(It.IsAny<IServiceProvider>()), Times.Once);
        }

        [Fact]
        public void GiveRunnerSystem_RunsForever_NotComplete()
        {
            TaskCompletionSource<bool> tcs = new();
            m_clientMock.Setup(c => c.ConnectAsync(It.IsAny<IServiceProvider>())).Returns(tcs.Task);

            BotSystemRunner runner = new(GetServiceProvider(), m_systemMock.Object);

            var t = runner.RunAsync(CancellationToken.None);
            t.Wait(5);

            Assert.False(t.IsCompleted);
        }

        [Fact]
        public void GiveRunnerSystem_Cancelled_Complete()
        {
            TaskCompletionSource<bool> tcs = new();
            m_clientMock.Setup(c => c.ConnectAsync(It.IsAny<IServiceProvider>())).Returns(tcs.Task);

            BotSystemRunner runner = new(GetServiceProvider(), m_systemMock.Object);

            using CancellationTokenSource cts = new();
            var t = runner.RunAsync(cts.Token);
            t.Wait(5);

            cts.Cancel();
            t.Wait(5);

            Assert.True(t.IsCompleted);
        }
    }
}
