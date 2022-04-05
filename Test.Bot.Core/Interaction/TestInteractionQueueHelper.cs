using Bot.Api;
using Bot.Base;
using Bot.Core.Interaction;
using Moq;
using System;
using System.Threading.Tasks;
using Test.Bot.Base;
using Xunit;

namespace Test.Bot.Core.Interaction
{
    public static class TestInteractionQueueHelper
    {
        /// <summary>
        /// Test that a method is properly using the Guild Queue
        /// </summary>
        /// <param name="serviceProvider">Service Provider</param>
        /// <param name="performTest">Set up and perform the test. We expect it calls into the Guild Queue</param>
        /// <param name="verifyParams">Verification action to assert that params passed to the queue are correct</param>
        public static void TestGuildQueueRequested(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<string, IBotInteractionContext>? verifyParams = null)
        {
            TestQueueRequested<IGuildInteractionQueue>(serviceProvider, performTest, verifyParams);
        }

        /// <summary>
        /// Test that a method is properly using the Town Queue
        /// </summary>
        /// <param name="serviceProvider">Service Provider</param>
        /// <param name="performTest">Set up and perform the test. We expect it calls into the Town Queue</param>
        /// <param name="verifyParams">Verification action to assert that params passed to the queue are correct</param>
        public static void TestTownQueueRequested(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<string, IBotInteractionContext>? verifyParams = null)
        {
            TestQueueRequested<ITownInteractionQueue>(serviceProvider, performTest, verifyParams);
        }

        public static void TestQueueRequested<TQueue>(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<string, IBotInteractionContext>? verifyParams) where TQueue : class, IInteractionQueue
        {
            Mock<TQueue> queueMock = new(MockBehavior.Strict);

            ServiceProvider sp = new(serviceProvider);
            sp.AddService(queueMock.Object);

            queueMock.Setup(iq => iq.QueueInteractionAsync(It.IsAny<string>(), It.IsAny<IBotInteractionContext>(), It.IsAny<Func<Task<InteractionResult>>>()))
                .Callback<string, IBotInteractionContext, Func<Task<InteractionResult>>>((s, ic, f) =>
                {
                    verifyParams?.Invoke(s, ic);
                })
                .Returns(Task.CompletedTask);

            TestTaskHelper.AssertCompletedTask(() => performTest(sp));

            queueMock.Verify(iq => iq.QueueInteractionAsync(It.IsAny<string>(), It.IsAny<IBotInteractionContext>(), It.IsAny<Func<Task<InteractionResult>>>()), Times.Once);
        }

        /// <summary>
        /// Test that a method calling into the Guild queue behaves as expected
        /// </summary>
        /// <param name="serviceProvider">Service Provider</param>
        /// <param name="performTest">Set up and perform the test</param>
        /// <param name="verifyResult">Verification action to assert that the result of the test is correct</param>
        public static void TestGuildQueueMethod(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<InteractionResult>? verifyResult = null)
        {
            TestQueueMethod<IGuildInteractionQueue>(serviceProvider, performTest, verifyResult);
        }

        /// <summary>
        /// Test that a method calling into the Town queue behaves as expected
        /// </summary>
        /// <param name="serviceProvider">Service Provider</param>
        /// <param name="performTest">Set up and perform the test</param>
        /// <param name="verifyResult">Verification action to assert that the result of the test is correct</param>
        public static void TestTowndQueueMethod(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<InteractionResult>? verifyResult = null)
        {
            TestQueueMethod<IGuildInteractionQueue>(serviceProvider, performTest, verifyResult);
        }

        public static void TestQueueMethod<TQueue>(IServiceProvider serviceProvider, Func<IServiceProvider, Task> performTest, Action<InteractionResult>? verifyResult) where TQueue : class, IInteractionQueue
        { 
            Mock<TQueue> queueMock = new(MockBehavior.Strict);

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
