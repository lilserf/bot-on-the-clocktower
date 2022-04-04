using Bot.Api;
using Bot.Base;
using Bot.Core.Interaction;
using Moq;
using System;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Interactions
{
    public static class TestQueueHelper
    {
        public static void TestGuildQueueRequested(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<string, IBotInteractionContext>? verifyQueueParams = null)
        {
            Mock<IGuildInteractionQueue> queueMock = new(MockBehavior.Strict);

            ServiceProvider sp = new(serviceProvider);
            sp.AddService(queueMock.Object);

            queueMock.Setup(iq => iq.QueueInteractionAsync(It.IsAny<string>(), It.IsAny<IBotInteractionContext>(), It.IsAny<Func<Task<InteractionResult>>>()))
                .Callback<string, IBotInteractionContext, Func<Task<InteractionResult>>>((s, ic, f) =>
                {
                    verifyQueueParams?.Invoke(s, ic);
                })
                .Returns(Task.CompletedTask);

            TestTaskHelper.AssertCompletedTask(() => performTest(sp));

            queueMock.Verify(iq => iq.QueueInteractionAsync(It.IsAny<string>(), It.IsAny<IBotInteractionContext>(), It.IsAny<Func<Task<InteractionResult>>>()), Times.Once);
        }

        public static void TestGuildQueuedMethod(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<InteractionResult>? verifyResult = null)
        {
            Mock<IGuildInteractionQueue> queueMock = new(MockBehavior.Strict);

            ServiceProvider sp = new(serviceProvider);
            sp.AddService(queueMock.Object);

            InteractionResult? testResult = null;

            queueMock.Setup(iq => iq.QueueInteractionAsync(It.IsAny<string>(), It.IsAny<IBotInteractionContext>(), It.IsAny<Func<Task<InteractionResult>>>()))
                .Returns<string, IBotInteractionContext, Func<Task<InteractionResult>>>((_, _, f) =>
                {
                    var t = f();
                    t.Wait(50);
                    Assert.True(t.IsCompleted);
                    Assert.Null(testResult);
                    testResult = t.Result;
                    return t; //NOTE: The real queue returns nearly immediately. This test queue will return when the actual method does.
                });

            TestTaskHelper.AssertCompletedTask(() => performTest(sp));

            queueMock.Verify(iq => iq.QueueInteractionAsync(It.IsAny<string>(), It.IsAny<IBotInteractionContext>(), It.IsAny<Func<Task<InteractionResult>>>()), Times.Once);
            Assert.NotNull(testResult);
            verifyResult?.Invoke(testResult!);
        }
    }
}
