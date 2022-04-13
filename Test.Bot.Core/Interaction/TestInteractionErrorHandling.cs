using Bot.Api;
using Bot.Core.Interaction;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Interaction
{
    public class TestInteractionErrorHandling : TestBase
    {
        private readonly Mock<IShutdownPreventionService> m_mockShutdownPrevention = new(MockBehavior.Loose);
        private readonly Mock<IBotSystem> m_mockSystem = new(MockBehavior.Loose);

        private readonly Mock<IBotInteractionContext> m_mockContext = new(MockBehavior.Strict);
        private readonly Mock<IMember> m_mockMember = new(MockBehavior.Strict);
        private const int ContextKey = 777;

        public TestInteractionErrorHandling()
        {
            RegisterMock(m_mockShutdownPrevention);
            RegisterMock(m_mockSystem);

            var mockWebhookBuilder = new Mock<IBotWebhookBuilder>(MockBehavior.Strict);
            mockWebhookBuilder.Setup(wb => wb.WithContent(It.IsAny<string>())).Returns(mockWebhookBuilder.Object);
            mockWebhookBuilder.Setup(wb => wb.AddEmbeds(It.IsAny<IEnumerable<IEmbed>>())).Returns(mockWebhookBuilder.Object);

            m_mockSystem.Setup(s => s.CreateWebhookBuilder()).Returns(mockWebhookBuilder.Object);

            m_mockContext.SetupGet(c => c.Member).Returns(m_mockMember.Object);
            m_mockContext.Setup(c => c.DeferInteractionResponse()).Returns(Task.CompletedTask);
            m_mockContext.Setup(c => c.EditResponseAsync(It.IsAny<IBotWebhookBuilder>())).Returns(Task.CompletedTask);

            m_mockMember.Setup(m => m.SendMessageAsync(It.IsAny<string>())).ReturnsAsync(new Mock<IMessage>(MockBehavior.Loose).Object);
        }

        [Fact]
        public void QueueRequest_ExceptionThrownDuringSetup_NotifiesAuthor()
        {
            string errorMessage = "threw an error!";
            m_mockContext.Setup(c => c.DeferInteractionResponse()).Throws(() => new TestException(errorMessage));

            var queue = new TestInteractionQueue(GetServiceProvider());
            AssertCompletedTask(() => queue.QueueInteractionAsync("initial message", m_mockContext.Object, () => Task.FromResult(InteractionResult.FromMessage("success"))));

            m_mockMember.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(nameof(TestException)) && s.Contains(errorMessage) && s.Contains(ContextKey.ToString()))), Times.Once);
        }

        [Fact]
        public async Task QueueRequest_ExceptionThrownDuringProcess_NotifiesAuthor()
        {
            string errorMessage = "threw an error!";
            m_mockContext.SetupSequence(c => c.EditResponseAsync(It.IsAny<IBotWebhookBuilder>()))
                .Returns(Task.CompletedTask)
                .Returns(Task.CompletedTask)
                .Returns(Task.CompletedTask)
                .Throws(() => new TestException(errorMessage));

            var tcs = new TaskCompletionSource();

            var queue = new TestInteractionQueue(GetServiceProvider());
            AssertCompletedTask(() => queue.QueueInteractionAsync("initial message 1", m_mockContext.Object, async () =>
            {
                await tcs.Task;
                return InteractionResult.FromMessage("success");
            }));
            AssertCompletedTask(() => queue.QueueInteractionAsync("initial message 2", m_mockContext.Object, () => Task.FromResult(InteractionResult.FromMessage("success"))));

            m_mockMember.Verify(m => m.SendMessageAsync(It.IsAny<string>()), Times.Never);

            tcs.SetResult();           

            m_mockMember.Verify(m => m.SendMessageAsync(It.Is<string>(s => s.Contains(nameof(TestException)) && s.Contains(errorMessage) && s.Contains(ContextKey.ToString()))), Times.Once);
        }

        private class TestException : Exception
        {
            public TestException(string message)
                : base(message)
            {}
        }

        private class TestInteractionQueue : BaseInteractionQueue<int>
        {
            public TestInteractionQueue(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {}

            protected override string GetFriendlyStringForKey(int key) => key.ToString();
            protected override int KeyFromContext(IBotInteractionContext context) => ContextKey;
        }
    }
}
