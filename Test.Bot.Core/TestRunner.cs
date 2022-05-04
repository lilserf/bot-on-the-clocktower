using Bot.Api;
using Bot.Core;
using Bot.Core.Callbacks;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core
{
    public class TestRunner : TestBase
    {
        private readonly Mock<IBotSystem> m_mockSystem = new(MockBehavior.Strict);
        private readonly Mock<IBotClient> m_mockClient = new(MockBehavior.Strict);

        private readonly Mock<IFinalShutdownService> m_mockFinalShutdown = new(MockBehavior.Strict);
        private readonly TaskCompletionSource m_finalShutdownTcs = new();

        public TestRunner()
        {
            RegisterMock(new Mock<IShutdownPreventionService>());
            RegisterMock(new Mock<ICallbackSchedulerFactory>());
            RegisterMock(new Mock<IComponentService>());
            RegisterMock(new Mock<IColorBuilder>());
            RegisterMock(m_mockFinalShutdown);

            m_mockFinalShutdown.SetupGet(fs => fs.ReadyToShutdown).Returns(m_finalShutdownTcs.Task);

            m_mockClient.Setup(c => c.ConnectAsync(It.IsAny<IServiceProvider>())).Returns(Task.CompletedTask);
            m_mockClient.Setup(c => c.DisconnectAsync()).Returns(Task.CompletedTask);

            m_mockSystem.Setup(s => s.CreateClient(It.IsAny<IServiceProvider>())).Returns(m_mockClient.Object);
            m_mockSystem.Setup(s => s.CreateButton(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IBotSystem.ButtonType>(), It.IsAny<bool>(), It.IsAny<string>())).Returns(new Mock<IBotComponent>().Object);
            m_mockSystem.Setup(s => s.CreateSelectMenu(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<IBotSystem.SelectMenuOption>>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>())).Returns(new Mock<IBotComponent>().Object);
            m_mockSystem.Setup(s => s.CreateEmbedBuilder()).Returns(new Mock<IEmbedBuilder>().Object);
            m_mockSystem.Setup(s => s.CreateMessageBuilder()).Returns(new Mock<IMessageBuilder>().Object);
        }

        [Fact]
        public void ConstructRunner_NoExceptions()
        {
            _ = new BotSystemRunner(GetServiceProvider(), m_mockSystem.Object);
        }

        [Fact]
        public void GiveRunnerSystem_Run_CreatesClient()
        {
            BotSystemRunner runner = new(GetServiceProvider(), m_mockSystem.Object);

            m_mockSystem.Verify(s => s.CreateClient(It.IsAny<IServiceProvider>()), Times.Once);
        }

        [Fact]
        public void GiveRunnerSystem_Run_RunsClient()
        {
            BotSystemRunner runner = new(GetServiceProvider(), m_mockSystem.Object);

            m_mockClient.Verify(c => c.ConnectAsync(It.IsAny<IServiceProvider>()), Times.Never);

            var t = runner.RunAsync();
            t.Wait(5);

            m_mockClient.Verify(c => c.ConnectAsync(It.IsAny<IServiceProvider>()), Times.Once);
        }

        [Fact]
        public void GiveRunnerSystem_RunsForever_NotComplete()
        {
            TaskCompletionSource<bool> tcs = new();
            m_mockClient.Setup(c => c.ConnectAsync(It.IsAny<IServiceProvider>())).Returns(tcs.Task);

            BotSystemRunner runner = new(GetServiceProvider(), m_mockSystem.Object);


            var t = runner.RunAsync();
            t.Wait(5);

            Assert.False(t.IsCompleted);
        }

        [Fact]
        public void BotRunner_ShutdownReady_Exits()
        {
            BotSystemRunner runner = new(GetServiceProvider(), m_mockSystem.Object);

            var t = runner.RunAsync();
            t.Wait(5);

            m_mockClient.Verify(c => c.ConnectAsync(It.IsAny<IServiceProvider>()), Times.Once);
            m_mockClient.Verify(c => c.DisconnectAsync(), Times.Never);
            Assert.False(t.IsCompleted);

            m_finalShutdownTcs.SetResult();

            t.Wait(5);

            m_mockClient.Verify(c => c.DisconnectAsync(), Times.Once);
            Assert.True(t.IsCompleted);
        }
    }
}
