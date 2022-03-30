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
    public class TestShutdown : TestBase
    {
        private readonly Mock<IBotSystem> m_mockBotSystem = new(MockBehavior.Strict);
        private readonly Mock<IShutdownPreventionService> m_mockShutdownPrevention = new(MockBehavior.Strict);
        private readonly Mock<IBotWebhookBuilder> m_mockWebhookBuilder = new(MockBehavior.Strict);
        private readonly Mock<IBotInteractionContext> m_mockInteractionContext = new(MockBehavior.Strict);


        private readonly List<Task> m_registeredPreventerTasks = new();

        public TestShutdown()
        {
            RegisterMock(m_mockBotSystem);
            RegisterMock(m_mockShutdownPrevention);

            m_mockShutdownPrevention.SetupAdd(sp => sp.ShutdownRequested += (sender, args) => { });
            m_mockShutdownPrevention.Setup(sp => sp.RegisterShutdownPreventer(It.IsAny<Task>())).Callback<Task>(m_registeredPreventerTasks.Add);

            m_mockBotSystem.Setup(c => c.CreateWebhookBuilder()).Returns(m_mockWebhookBuilder.Object);
            m_mockWebhookBuilder.Setup(wb => wb.WithContent(It.IsAny<string>())).Returns(m_mockWebhookBuilder.Object);

            var mockGuild = new Mock<IGuild>(MockBehavior.Strict);
            var mockChannel = new Mock<IChannel>(MockBehavior.Strict);
            mockGuild.SetupGet(g => g.Id).Returns(123ul);
            mockChannel.SetupGet(c => c.Id).Returns(456ul);

            m_mockInteractionContext.Setup(c => c.DeferInteractionResponse()).Returns(Task.CompletedTask);
            m_mockInteractionContext.Setup(c => c.EditResponseAsync(It.IsAny<IBotWebhookBuilder>())).Returns(Task.CompletedTask);
            m_mockInteractionContext.SetupGet(ic => ic.Guild).Returns(mockGuild.Object);
            m_mockInteractionContext.SetupGet(ic => ic.Channel).Returns(mockChannel.Object);
        }

        [Fact]
        public void TownCommandQueue_RegistersAsPreventer()
        {
            var tcq = new TownCommandQueue(GetServiceProvider());

            m_mockShutdownPrevention.Verify(sp => sp.RegisterShutdownPreventer(It.IsAny<Task>()), Times.Once());
            Assert.Collection(m_registeredPreventerTasks,
                t => Assert.False(t.IsCompleted));
        }

        [Fact]
        public void NothingQueued_LearnsOfShutdown_UnblocksShutdown()
        {
            var tcq = new TownCommandQueue(GetServiceProvider());

            Assert.Collection(m_registeredPreventerTasks,
                t => Assert.False(t.IsCompleted));

            m_mockShutdownPrevention.Raise(sp => sp.ShutdownRequested += null, EventArgs.Empty);

            Assert.Collection(m_registeredPreventerTasks,
                t => Assert.True(t.IsCompleted));
        }

        [Fact]
        public async Task ItemQueued_LearnsOfShutdown_WaitsForItem()
        {
            string initialMessage = "some initial message";
            string completionMessage = "completion message";

            var tcs = new TaskCompletionSource<QueuedCommandResult>();

            var tcq = new TownCommandQueue(GetServiceProvider());
            var t = tcq.QueueCommandAsync(initialMessage, m_mockInteractionContext.Object, () => tcs.Task);

            Assert.True(t.IsCompleted);
            m_mockInteractionContext.Verify(ic => ic.DeferInteractionResponse(), Times.Once);
            m_mockWebhookBuilder.Verify(wb => wb.WithContent(It.Is<string>(s => s == initialMessage)), Times.Once);
            m_mockWebhookBuilder.Verify(wb => wb.WithContent(It.Is<string>(s => s == completionMessage)), Times.Never);
            m_mockInteractionContext.Verify(ic => ic.EditResponseAsync(It.Is<IBotWebhookBuilder>(wb => wb == m_mockWebhookBuilder.Object)), Times.Once);

            m_mockShutdownPrevention.Raise(sp => sp.ShutdownRequested += null, EventArgs.Empty);

            Assert.Collection(m_registeredPreventerTasks,
                t => Assert.False(t.IsCompleted));

            tcs.SetResult(new QueuedCommandResult(completionMessage));

            m_mockWebhookBuilder.Verify(wb => wb.WithContent(It.Is<string>(s => s == completionMessage)), Times.Once);
            m_mockWebhookBuilder.Verify(wb => wb.WithContent(It.IsAny<string>()), Times.Exactly(2));
            m_mockInteractionContext.Verify(ic => ic.EditResponseAsync(It.Is<IBotWebhookBuilder>(wb => wb == m_mockWebhookBuilder.Object)), Times.Exactly(2));

            Assert.Collection(m_registeredPreventerTasks,
                t => Assert.True(t.IsCompleted));
        }

        [Fact(Skip ="NYI")]
        public void LearnsOfShutdown_ItemQueuedAfter_PoliteErrorAndQueuedItemNotCalled()
        {
            Assert.True(false);
        }
    }
}
